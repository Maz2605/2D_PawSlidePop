using System;
using System.Collections.Generic;
using MazFramework.Foundation.Singleton;
using UnityEngine;
using UnityEngine.Pool;

namespace MazFramework.Foundation.Pooling
{
    public class PoolingManager : Singleton<PoolingManager>
    {
        // Entries also provide the legacy IObjectPool API without bypassing ownership checks.
        internal sealed class PoolEntry : IObjectPool<GameObject>
        {
            internal readonly PoolingManager Owner;
            internal readonly GameObject Prefab;
            internal readonly Stack<PooledItem> Idle;
            internal readonly int MaxSize;
            internal bool Retired;

            internal PoolEntry(PoolingManager owner, GameObject prefab, int capacity, int maxSize)
            {
                Owner = owner;
                Prefab = prefab;
                MaxSize = maxSize;
                Idle = new Stack<PooledItem>(capacity);
            }

            internal void Prune()
            {
                if (Idle.Count == 0) return;
                var items = Idle.ToArray();
                Idle.Clear();
                for (int i = items.Length - 1; i >= 0; i--)
                {
                    if (items[i] != null && items[i].State == PooledItem.ItemState.Idle) Idle.Push(items[i]);
                    else Owner.Forget(items[i]);
                }
            }

            public int CountInactive { get { Prune(); return Idle.Count; } }
            public GameObject Get() => Owner != null ? Owner.SpawnFromPool(this, Vector3.zero, Quaternion.identity, null) : null;
            public PooledObject<GameObject> Get(out GameObject value)
            {
                value = Get();
                return new PooledObject<GameObject>(value, this);
            }
            public void Release(GameObject value)
            {
                if (value == null) return;
                if (value.TryGetComponent(out PooledItem item) && ReferenceEquals(item.Entry, this)) item.Release();
                else Debug.LogWarning("[Pooling] Cannot release an item into a different pool.", value);
            }
            public void Clear() { if (Owner != null) Owner.Retire(this); }
        }

        private readonly Dictionary<int, PoolEntry> _pools = new Dictionary<int, PoolEntry>();
        private readonly HashSet<PooledItem> _owned = new HashSet<PooledItem>();
        [Header("Global Settings")]
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxSize = 50;
        private Transform _creationRoot;
        private bool _destroying;

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0 || _destroying) return;
            PoolEntry entry = GetPool(prefab);
            entry.Prune();
            int target = Mathf.Min(count, entry.MaxSize);
            while (entry.Idle.Count < target)
            {
                PooledItem item = CreateItem(entry);
                if (item == null) return;
                entry.Idle.Push(item);
            }
        }

        private PoolEntry GetPool(GameObject prefab)
        {
            int key = prefab.GetInstanceID();
            if (_pools.TryGetValue(key, out PoolEntry entry)) return entry;
            int limit = Mathf.Max(1, maxSize);
            entry = new PoolEntry(this, prefab, Mathf.Clamp(defaultCapacity, 0, limit), limit);
            _pools.Add(key, entry);
            return entry;
        }

        private PooledItem CreateItem(PoolEntry entry)
        {
            if (_destroying || entry.Retired || entry.Prefab == null) return null;
            if (_creationRoot == null)
            {
                var staging = new GameObject("Pool creation (inactive)");
                staging.SetActive(false);
                staging.transform.SetParent(transform, false);
                _creationRoot = staging.transform;
            }
            // An inactive parent prevents OnEnable on an active prefab before transform/ownership setup.
            GameObject instance = Instantiate(entry.Prefab, _creationRoot);
            instance.SetActive(false);
            PooledItem item = instance.GetComponent<PooledItem>();
            if (item == null) item = instance.AddComponent<PooledItem>();
            item.Bind(this, entry);
            _owned.Add(item);
            instance.transform.SetParent(transform, false);
            return item;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null || _destroying) return null;
            return SpawnFromPool(GetPool(prefab), position, rotation, parent);
        }

        internal GameObject SpawnFromPool(PoolEntry entry, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (_destroying || entry.Retired) return null;
            PooledItem item = null;
            while (entry.Idle.Count > 0 && item == null)
            {
                item = entry.Idle.Pop();
                if (item == null) _owned.Remove(item);
            }
            if (item == null) item = CreateItem(entry);
            if (item == null) return null;

            item.State = PooledItem.ItemState.Active;
            uint rental = ++item.RentalVersion;
            item.SpawnCallbackStarted = false;
            GameObject instance = item.gameObject;
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            // OnEnable may have returned or destroyed the object.
            if (item == null || item.RentalVersion != rental || item.State != PooledItem.ItemState.Active || _destroying) return null;
            item.SpawnCallbackStarted = true;
            try
            {
                if (instance.TryGetComponent<IPoolable>(out var poolable)) poolable.OnSpawn();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (item != null && item.RentalVersion == rental && item.State == PooledItem.ItemState.Active) Release(item, true);
                return null;
            }
            return item != null && item.RentalVersion == rental && item.State == PooledItem.ItemState.Active && !_destroying ? instance : null;
        }

        public GameObject Spawn(GameObject prefab) => Spawn(prefab, Vector3.zero, Quaternion.identity);
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            if (prefab == null) return null;
            GameObject instance = Spawn(prefab.gameObject, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }
        public T Spawn<T>(T prefab) where T : Component => Spawn(prefab, Vector3.zero, Quaternion.identity);

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            if (instance.TryGetComponent(out PooledItem item)) item.Release();
            else DestroyInstance(instance);
        }

        internal void Release(PooledItem item, bool forceDestroy = false)
        {
            if (item == null) return;
            if (item.State != PooledItem.ItemState.Active)
            {
                Debug.LogWarning("[Pooling] Item was already released or is being returned.", item);
                return;
            }
            // Mark before callbacks: reentrant Release must never duplicate cache entries/callbacks.
            item.State = PooledItem.ItemState.Returning;
            bool notify = item.SpawnCallbackStarted;
            item.SpawnCallbackStarted = false;
            try
            {
                if (notify && item.TryGetComponent<IPoolable>(out var poolable)) poolable.OnDespawn();
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
            finally
            {
                if (item != null && item.State != PooledItem.ItemState.Destroyed)
                {
                    item.gameObject.SetActive(false);
                    if (item != null && item.State != PooledItem.ItemState.Destroyed)
                    {
                        PoolEntry entry = item.Entry;
                        if (forceDestroy || _destroying || entry.Retired) DestroyItem(item);
                        else
                        {
                            item.transform.SetParent(transform, false);
                            // Only scan for externally destroyed entries when the cache appears full.
                            if (entry.Idle.Count >= entry.MaxSize) entry.Prune();
                            if (entry.Idle.Count >= entry.MaxSize) DestroyItem(item);
                            else
                            {
                                item.State = PooledItem.ItemState.Idle;
                                entry.Idle.Push(item);
                            }
                        }
                    }
                }
            }
        }

        public void ClearPools()
        {
            PoolEntry[] entries = new List<PoolEntry>(_pools.Values).ToArray();
            _pools.Clear();
            foreach (PoolEntry entry in entries) Retire(entry);
        }

        internal void Retire(PoolEntry entry)
        {
            if (entry.Retired) return;
            entry.Retired = true;
            if (entry.Prefab != null && _pools.TryGetValue(entry.Prefab.GetInstanceID(), out PoolEntry current) && ReferenceEquals(current, entry))
                _pools.Remove(entry.Prefab.GetInstanceID());
            while (entry.Idle.Count > 0) DestroyItem(entry.Idle.Pop());
            // Active items retain the retired entry and are destroyed when returned.
        }

        internal void Forget(PooledItem item) => _owned.Remove(item);

        private void DestroyItem(PooledItem item)
        {
            if (item == null) return;
            item.State = PooledItem.ItemState.Destroyed;
            _owned.Remove(item);
            DestroyInstance(item.gameObject);
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);
        }

        protected override void OnDestroy()
        {
            if (ExistingInstance == this)
            {
                _destroying = true;
                ClearPools();
                foreach (PooledItem item in new List<PooledItem>(_owned))
                {
                    if (item == null) continue;
                    if (item.State == PooledItem.ItemState.Active) Release(item, true);
                    else DestroyItem(item);
                }
                _owned.Clear();
            }
            base.OnDestroy();
        }
    }
}
