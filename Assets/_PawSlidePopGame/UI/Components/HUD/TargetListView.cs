using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class TargetListView : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private TargetItemView targetItemPrefab;

        private readonly List<TargetItemView> _spawnedItems = new List<TargetItemView>();

        private void Awake()
        {
            BootstrapTemplateFromScene();
        }

        private void OnValidate()
        {
            if (contentRoot == null)
            {
                contentRoot = transform;
            }

            // BootstrapTemplateFromScene();
        }

        public void SetTargets(IReadOnlyList<TargetProgressData> targets)
        {
            int count = targets != null ? targets.Count : 0;
            EnsureItemCount(count);

            AdjustLayoutSpacing(count);

            var layoutGroup = GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = true;
            }

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                TargetItemView item = _spawnedItems[i];
                if (item == null)
                {
                    continue;
                }

                if (i < count)
                {
                    item.SetData(targets[i]);
                }
                else
                {
                    item.SetData(null);
                }
            }
        }

        public TargetItemView GetTargetItemView(int tileId)
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null && _spawnedItems[i].gameObject.activeSelf && _spawnedItems[i].BoundTileId == tileId)
                {
                    return _spawnedItems[i];
                }
            }
            return null;
        }

        public void PlayProgressFx(TargetProgressChangedPayload payload)
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                _spawnedItems[i]?.PlayProgressFx(payload);
            }
        }

        public void PlayCompletedFx()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.06f, 0.22f, 4)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void ResetView()
        {
            var layoutGroup = GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = true;
            }

            transform.DOKill();
            transform.localScale = Vector3.one;

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                {
                    _spawnedItems[i].SetData(null);
                }
            }
        }

        public Sequence GetEntranceSequence(float delayPerItem = 0.08f, float duration = 0.45f)
        {
            Sequence seq = DOTween.Sequence().SetUpdate(true);

            int activeCount = 0;
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null && _spawnedItems[i].gameObject.activeSelf)
                {
                    activeCount++;
                }
            }
            AdjustLayoutSpacing(activeCount);

            var layoutGroup = GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = true;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
                layoutGroup.enabled = false;
            }

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                TargetItemView item = _spawnedItems[i];
                if (item == null || !item.gameObject.activeSelf)
                {
                    continue;
                }

                Vector3 targetLocalPos = item.transform.localPosition;
                item.transform.localPosition = targetLocalPos + new Vector3(0f, -80f, 0f);
                item.transform.localScale = Vector3.zero;

                seq.Join(item.transform.DOLocalMoveY(targetLocalPos.y, duration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(i * delayPerItem)
                    .SetUpdate(true)
                    .SetLink(item.gameObject, LinkBehaviour.KillOnDisable));

                seq.Join(item.transform.DOScale(Vector3.one, duration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(i * delayPerItem)
                    .SetUpdate(true)
                    .SetLink(item.gameObject, LinkBehaviour.KillOnDisable));
            }

            return seq;
        }

        private void AdjustLayoutSpacing(int count)
        {
            var layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                return;
            }

            if (count <= 1)
            {
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.spacing = 0f;
            }
            else
            {
                RectTransform rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    float containerWidth = rectTransform.rect.width;
                    if (containerWidth <= 0f)
                    {
                        containerWidth = rectTransform.sizeDelta.x;
                    }

                    if (containerWidth <= 0f)
                    {
                        containerWidth = 1080f;
                    }

                    float paddingLeft = layoutGroup.padding.left;
                    float paddingRight = layoutGroup.padding.right;

                    float itemWidth = 200f; // fallback
                    if (_spawnedItems.Count > 0 && _spawnedItems[0] != null)
                    {
                        var itemRect = _spawnedItems[0].transform as RectTransform;
                        if (itemRect != null)
                        {
                            itemWidth = itemRect.rect.width;
                            if (itemWidth <= 0f)
                            {
                                itemWidth = itemRect.sizeDelta.x;
                            }
                        }
                    }
                    else if (targetItemPrefab != null)
                    {
                        var prefabRect = targetItemPrefab.transform as RectTransform;
                        if (prefabRect != null)
                        {
                            itemWidth = prefabRect.rect.width;
                            if (itemWidth <= 0f)
                            {
                                itemWidth = prefabRect.sizeDelta.x;
                            }
                        }
                    }

                    if (itemWidth <= 0f)
                    {
                        itemWidth = 200f;
                    }

                    float totalItemWidth = count * itemWidth;
                    float availableSpace = containerWidth - paddingLeft - paddingRight - totalItemWidth;

                    if (availableSpace > 0f)
                    {
                        layoutGroup.spacing = availableSpace / (count - 1);
                        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    }
                    else
                    {
                        layoutGroup.spacing = 32f;
                        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    }
                }
            }
        }

        private void BootstrapTemplateFromScene()
        {
            if (targetItemPrefab == null)
            {
                targetItemPrefab = contentRoot.GetComponentInChildren<TargetItemView>(true);
            }

            if (targetItemPrefab == null)
            {
                Debug.LogWarning("[TargetListView] Missing target item prefab/template.", this);
                return;
            }

            List<TargetItemView> existingItems = new List<TargetItemView>(contentRoot.GetComponentsInChildren<TargetItemView>(true));
            for (int i = 0; i < existingItems.Count; i++)
            {
                TargetItemView item = existingItems[i];
                if (item == null || item == targetItemPrefab)
                {
                    continue;
                }

                Destroy(item.gameObject);
            }

            targetItemPrefab.gameObject.SetActive(false);
        }

        private void EnsureItemCount(int count)
        {
            if (targetItemPrefab == null)
            {
                return;
            }

            while (_spawnedItems.Count < count)
            {
                TargetItemView instance = Instantiate(targetItemPrefab, contentRoot);
                instance.name = $"{targetItemPrefab.name}_{_spawnedItems.Count}";
                instance.gameObject.SetActive(true);
                _spawnedItems.Add(instance);
            }

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                {
                    _spawnedItems[i].gameObject.SetActive(i < count);
                }
            }
        }
    }
}
