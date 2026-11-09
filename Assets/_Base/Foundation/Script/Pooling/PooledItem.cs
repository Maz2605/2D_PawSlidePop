using System;
using UnityEngine;
using UnityEngine.Pool;

namespace MazFramework.Foundation.Pooling
{
    [DisallowMultipleComponent]
    public class PooledItem : MonoBehaviour
    {
        internal enum ItemState { Idle, Active, Returning, Destroyed }
        internal PoolingManager Owner { get; private set; }
        internal PoolingManager.PoolEntry Entry { get; private set; }
        internal ItemState State { get; set; }
        internal bool SpawnCallbackStarted { get; set; }
        internal uint RentalVersion { get; set; }
        private IObjectPool<GameObject> _legacyPool;
        private bool _managed;

        [Obsolete("Use PoolingManager.Spawn/Despawn or PooledItem.Release. Managed ownership cannot be reassigned.")]
        public IObjectPool<GameObject> Pool
        {
            get => _managed ? Entry : _legacyPool;
            set
            {
                if (_managed)
                {
                    Debug.LogWarning("[Pooling] Cannot reassign the pool of a managed item.", this);
                    return;
                }
                _legacyPool = value;
            }
        }

        internal void Bind(PoolingManager owner, PoolingManager.PoolEntry entry)
        {
            Owner = owner;
            Entry = entry;
            State = ItemState.Idle;
            SpawnCallbackStarted = false;
            _managed = true;
            _legacyPool = null;
        }

        public void Release()
        {
            if (_managed)
            {
                if (Owner != null) Owner.Release(this);
                else if (State != ItemState.Destroyed)
                {
                    State = ItemState.Destroyed;
                    if (Application.isPlaying) Destroy(gameObject);
                    else DestroyImmediate(gameObject);
                }
                return;
            }
            // External pools own their rental state/collection checks. They may assign Pool only once.
            if (_legacyPool != null) _legacyPool.Release(gameObject);
            else if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void OnDestroy()
        {
            State = ItemState.Destroyed;
            if (Owner != null) Owner.Forget(this);
        }
    }
}
