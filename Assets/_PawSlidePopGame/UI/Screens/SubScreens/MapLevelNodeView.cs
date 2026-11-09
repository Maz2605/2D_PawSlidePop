using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Components;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    [DisallowMultipleComponent]
    public sealed class MapLevelNodeView : MonoBehaviour
    {
        private enum NodeVisualState
        {
            Locked,
            Unlocked,
            Available,
            Current,
            Completed,
            Perfect,
            HardUnlocked
        }

        [Header("Core")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image nodeBackground;

        [Header("State Sprites")]
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;
        [SerializeField] private Sprite availableSprite;
        [SerializeField] private Sprite currentSprite;
        [SerializeField] private Sprite completedSprite;
        [SerializeField] private Sprite perfectSprite;
        [SerializeField] private Sprite hardUnlockedSprite;

        [Header("Locked Visuals")]
        [SerializeField] private RectTransform bubbleRoot;
        [SerializeField] private RectTransform lockIconRoot;

        [Header("Stars")]
        [SerializeField] private GameObject starsRoot;
        [SerializeField] private List<StarItemView> starViews = new List<StarItemView>();
        [SerializeField] private bool showLockedStarsForPlayedLevels;

        [Header("Animation")]
        [SerializeField] private float bubblePulseScale = 1.06f;
        [SerializeField] private float bubblePulseDuration = 1.15f;
        [SerializeField] private float bubbleFloatDistance = 8f;
        [SerializeField] private float bubbleFloatDuration = 1.4f;
        [SerializeField] private float lockSwingAngle = 12f;
        [SerializeField] private float lockSwingDuration = 0.9f;

        private Sequence _bubbleSequence;
        private Tween _lockSwingTween;
        private bool _cachedBubblePose;
        private bool _cachedLockPose;
        private Vector3 _bubbleBaseScale;
        private Vector2 _bubbleBaseAnchoredPosition;
        private Quaternion _lockBaseRotation;

        public Button Button => button;

        private void Awake()
        {
            CacheVisualPose();
        }

        private void OnDisable()
        {
            StopLockedVisuals(resetPose: true);
        }

        private void OnDestroy()
        {
            StopLockedVisuals(resetPose: false);
        }

        public void Setup(MapLevelEntry entry, Action<string> onPressed)
        {
            CacheVisualPose();

            if (levelText != null)
            {
                levelText.text = entry.DisplayLevelNumber.ToString();
            }

            ApplyState(entry);
            ApplyStars(entry);

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            // Nút luôn có thể tương tác (interactable = true) để người chơi có thể click vào màn bị khóa và mở LevelLockedPopup
            button.interactable = true;
            string capturedLevelId = entry.LevelId;
            button.onClick.AddListener(() => onPressed?.Invoke(capturedLevelId));
        }

        private void ApplyState(MapLevelEntry entry)
        {
            NodeVisualState visualState = ResolveVisualState(entry);
            ApplyNodeSprite(ResolveSprite(visualState));

            bool isLocked = visualState == NodeVisualState.Locked;
            SetActive(bubbleRoot, isLocked);
            SetActive(lockIconRoot, isLocked);

            if (isLocked)
            {
                PlayLockedVisuals();
                return;
            }

            StopLockedVisuals(resetPose: true);
        }

        private void ApplyStars(MapLevelEntry entry)
        {
            int clampedStars = Mathf.Clamp(entry.BestStars, 0, starViews.Count);
            bool shouldShowStars = entry.State != MapLevelState.Locked && clampedStars > 0;

            if (starsRoot != null)
            {
                starsRoot.SetActive(shouldShowStars);
            }

            if (!shouldShowStars)
            {
                for (int i = 0; i < starViews.Count; i++)
                {
                    if (starViews[i] != null)
                    {
                        starViews[i].gameObject.SetActive(false);
                    }
                }

                return;
            }

            StarVisualState reachedState = clampedStars >= 4
                ? StarVisualState.ReachedMax
                : StarVisualState.ReachedNormal;

            for (int i = 0; i < starViews.Count; i++)
            {
                StarItemView star = starViews[i];
                if (star == null)
                {
                    continue;
                }

                bool isReached = i < clampedStars;
                star.gameObject.SetActive(isReached || showLockedStarsForPlayedLevels);
                star.SetState(isReached ? reachedState : StarVisualState.Locked, true);
            }
        }

        private NodeVisualState ResolveVisualState(MapLevelEntry entry)
        {
            if (entry.State == MapLevelState.Locked)
            {
                return NodeVisualState.Locked;
            }

            if (entry.State == MapLevelState.Perfect)
            {
                return NodeVisualState.Perfect;
            }

            if (entry.State == MapLevelState.Completed)
            {
                return NodeVisualState.Completed;
            }

            if (entry.State == MapLevelState.Current)
            {
                return NodeVisualState.Current;
            }

            if (entry.State == MapLevelState.Hard || entry.IsHardLevel)
            {
                return NodeVisualState.HardUnlocked;
            }

            if (entry.State == MapLevelState.FailedOrUnlocked || entry.State == MapLevelState.Available)
            {
                return NodeVisualState.Available;
            }

            return NodeVisualState.Unlocked;
        }

        private Sprite ResolveSprite(NodeVisualState visualState)
        {
            switch (visualState)
            {
                case NodeVisualState.Locked:
                    return FirstAssigned(lockedSprite, unlockedSprite);
                case NodeVisualState.Available:
                    return FirstAssigned(availableSprite, unlockedSprite);
                case NodeVisualState.Current:
                    return FirstAssigned(currentSprite, availableSprite, unlockedSprite);
                case NodeVisualState.Completed:
                    return FirstAssigned(completedSprite, availableSprite, unlockedSprite);
                case NodeVisualState.Perfect:
                    return FirstAssigned(perfectSprite, completedSprite, availableSprite, unlockedSprite);
                case NodeVisualState.HardUnlocked:
                    return FirstAssigned(hardUnlockedSprite, availableSprite, unlockedSprite);
                case NodeVisualState.Unlocked:
                default:
                    return unlockedSprite;
            }
        }

        private void ApplyNodeSprite(Sprite sprite)
        {
            if (nodeBackground == null || sprite == null)
            {
                return;
            }

            nodeBackground.sprite = sprite;
        }

        private void PlayLockedVisuals()
        {
            StopLockedVisuals(resetPose: true);

            if (bubbleRoot != null)
            {
                _bubbleSequence = DOTween.Sequence()
                    .SetAutoKill(false)
                    .SetLoops(-1, LoopType.Restart)
                    .SetLink(gameObject);

                _bubbleSequence.Join(
                    bubbleRoot.DOScale(_bubbleBaseScale * bubblePulseScale, bubblePulseDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(2, LoopType.Yoyo));

                _bubbleSequence.Join(
                    bubbleRoot.DOAnchorPosY(_bubbleBaseAnchoredPosition.y + bubbleFloatDistance, bubbleFloatDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(2, LoopType.Yoyo));

                _bubbleSequence.Play();
            }

            if (lockIconRoot != null)
            {
                _lockSwingTween = lockIconRoot
                    .DOLocalRotate(new Vector3(0f, 0f, lockSwingAngle), lockSwingDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            }
        }

        private void StopLockedVisuals(bool resetPose)
        {
            _bubbleSequence?.Kill(false);
            _bubbleSequence = null;

            _lockSwingTween?.Kill(false);
            _lockSwingTween = null;

            if (resetPose)
            {
                ResetVisualPose();
            }
        }

        private void CacheVisualPose()
        {
            if (!_cachedBubblePose && bubbleRoot != null)
            {
                _bubbleBaseScale = bubbleRoot.localScale;
                _bubbleBaseAnchoredPosition = bubbleRoot.anchoredPosition;
                _cachedBubblePose = true;
            }

            if (!_cachedLockPose && lockIconRoot != null)
            {
                _lockBaseRotation = lockIconRoot.localRotation;
                _cachedLockPose = true;
            }
        }

        private void ResetVisualPose()
        {
            if (_cachedBubblePose && bubbleRoot != null)
            {
                bubbleRoot.localScale = _bubbleBaseScale;
                bubbleRoot.anchoredPosition = _bubbleBaseAnchoredPosition;
            }

            if (_cachedLockPose && lockIconRoot != null)
            {
                lockIconRoot.localRotation = _lockBaseRotation;
            }
        }

        private static Sprite FirstAssigned(params Sprite[] sprites)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    return sprites[i];
                }
            }

            return null;
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }
    }
}
