using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    [DisallowMultipleComponent]
    public sealed class MapSectionView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform nodeRoot;
        [SerializeField] private List<RectTransform> nodeAnchors = new List<RectTransform>();

        private readonly List<MapLevelNodeView> _spawnedNodes = new List<MapLevelNodeView>();

        public RectTransform RectTransform => transform as RectTransform;
        public RectTransform NodeRoot => nodeRoot != null ? nodeRoot : RectTransform;
        public IReadOnlyList<RectTransform> NodeAnchors => GetNodeAnchors();
        public int Capacity => GetNodeAnchors().Count;
        public float SectionHeight => Mathf.Max(200f, RectTransform != null ? RectTransform.rect.height : 0f);

        public void BindSection(IReadOnlyList<MapLevelEntry> entries, MapLevelNodeView nodePrefab, Action<string> onPressed)
        {
            ClearNodes();

            if (entries == null || entries.Count == 0)
            {
                return;
            }

            IReadOnlyList<RectTransform> anchors = GetNodeAnchors();
            if (anchors.Count == 0)
            {
                Debug.LogWarning($"[MapSectionView] Section '{name}' has no node anchors assigned.", this);
                return;
            }

            if (backgroundImage == null)
            {
                Debug.LogWarning($"[MapSectionView] Section '{name}' has no background image assigned.", this);
            }

            if (nodePrefab == null)
            {
                Debug.LogWarning($"[MapSectionView] Cannot bind section '{name}' because node prefab is missing.", this);
                return;
            }

            int count = entries != null ? Mathf.Min(entries.Count, anchors.Count) : 0;
            for (int i = 0; i < count; i++)
            {
                RectTransform anchor = anchors[i];
                if (anchor == null)
                {
                    Debug.LogWarning($"[MapSectionView] Section '{name}' has a missing node anchor at index {i}.", this);
                    continue;
                }

                MapLevelNodeView node = Instantiate(nodePrefab, anchor, false);
                node.name = $"MapLevelNode_{entries[i].DisplayLevelNumber:000}";
                RectTransform nodeRect = node.transform as RectTransform;
                if (nodeRect != null)
                {
                    nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
                    nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
                    nodeRect.pivot = new Vector2(0.5f, 0.5f);
                    nodeRect.anchoredPosition = Vector2.zero;
                    nodeRect.localScale = Vector3.one;
                }

                node.gameObject.SetActive(true);
                node.Setup(entries[i], onPressed);
                _spawnedNodes.Add(node);
            }

            if (entries != null && entries.Count > anchors.Count)
            {
                Debug.LogWarning(
                    $"[MapSectionView] Section '{name}' only has {anchors.Count} anchors but received {entries.Count} levels. Extra levels were skipped.",
                    this);
            }
        }

        public Vector2 GetAnchorLocalPosition(int index)
        {
            IReadOnlyList<RectTransform> anchors = GetNodeAnchors();
            if (index < 0 || index >= anchors.Count || anchors[index] == null)
            {
                return Vector2.zero;
            }

            return GetLocalPositionRelativeToRoot(anchors[index]);
        }

        private IReadOnlyList<RectTransform> GetNodeAnchors()
        {
            if (nodeAnchors == null)
            {
                nodeAnchors = new List<RectTransform>();
            }

            for (int i = nodeAnchors.Count - 1; i >= 0; i--)
            {
                if (nodeAnchors[i] == null)
                {
                    nodeAnchors.RemoveAt(i);
                }
            }

            if (nodeAnchors.Count == 0 && nodeRoot != null)
            {
                for (int i = 0; i < nodeRoot.childCount; i++)
                {
                    if (nodeRoot.GetChild(i) is RectTransform anchor)
                    {
                        nodeAnchors.Add(anchor);
                    }
                }
            }

            return nodeAnchors;
        }

        private Vector2 GetLocalPositionRelativeToRoot(RectTransform target)
        {
            Vector2 position = Vector2.zero;
            Transform current = target;
            while (current != null && current != transform)
            {
                if (current is RectTransform rectTransform)
                {
                    position += rectTransform.anchoredPosition;
                }

                current = current.parent;
            }

            return position;
        }

        private void ClearNodes()
        {
            for (int i = _spawnedNodes.Count - 1; i >= 0; i--)
            {
                MapLevelNodeView node = _spawnedNodes[i];
                if (node == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(node.gameObject);
                }
                else
                {
                    DestroyImmediate(node.gameObject);
                }
            }

            _spawnedNodes.Clear();
        }

        private void OnValidate()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponentInChildren<Image>(true);
            }

            if (nodeRoot == null)
            {
                Transform foundNodeRoot = transform.Find("NodeRoot");
                nodeRoot = foundNodeRoot as RectTransform;
            }

            if (nodeRoot != null)
            {
                nodeAnchors.Clear();
                for (int i = 0; i < nodeRoot.childCount; i++)
                {
                    if (nodeRoot.GetChild(i) is RectTransform anchor)
                    {
                        nodeAnchors.Add(anchor);
                    }
                }
            }
        }
    }
}
