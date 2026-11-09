using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    public class WheelSubScreen : BaseSubScreen
    {
        [Header("Data")]
        [SerializeField] private WheelConfigSO config;


        [Header("Refs")]
        [SerializeField] private RectTransform wheelRoot;
        [SerializeField] private Button spinButton;
        [SerializeField] private List<WheelSegmentView> segmentViews = new List<WheelSegmentView>();
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text resultText;

        [Header("Animation")]
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private float pointerAngleDegrees = 90f;
        [SerializeField] private WheelSpinDirection spinDirection = WheelSpinDirection.Clockwise;
        [SerializeField, Range(0.05f, 0.4f)] private float accelerationTimeRatio = 0.18f;
        [SerializeField, Range(0.05f, 0.5f)] private float cruiseTimeRatio = 0.22f;
        [SerializeField, Range(0.02f, 0.25f)] private float accelerationDistanceRatio = 0.1f;
        [SerializeField, Range(0.05f, 0.45f)] private float cruiseDistanceRatio = 0.24f;
        [SerializeField] private float startPunchScale = 0.04f;
        [SerializeField] private float startPunchDuration = 0.18f;
        [SerializeField] private float settleOvershootDegrees = 2.5f;
        [SerializeField] private float settleDuration = 0.18f;
        [SerializeField] private Ease settleEase = Ease.OutSine;
        [SerializeField] private float settleShakeStrength = 0.8f;
        [SerializeField] private int settleShakeVibrato = 10;

        private readonly WheelStateRepository _stateRepository = new WheelStateRepository();
        private WheelConfigSnapshot _snapshot;
        private Sequence _spinSequence;
        private bool _isSpinning;
        private string _lastStatusText;

        public override void Init()
        {
            if (isInitialized)
            {
                return;
            }

            base.Init();
            BindButton(spinButton, HandleSpinClicked);
            LoadSnapshot();
            BindSegments();
            RefreshState();
        }

        public override void Show()
        {
            base.Show();
            LoadSnapshot();
            BindSegments();
            RefreshState();
        }

        public override void Hide()
        {
            base.Hide();
            KillSpinTween();
        }

        public void ResetFreeSpinCooldownForDev()
        {
            _stateRepository.ResetFreeSpinCooldown();
            _stateRepository.Reload();
            LoadSnapshot();
            BindSegments();
            RefreshState();
            SetStatus("Free spin restored!");
        }

        public void SetStatus(string status)
        {
            RefreshStatusText(status);
        }

        private void OnEnable()
        {
            WheelStateRepository.OnDataChanged += HandleWheelDataChanged;
        }

        private void OnDisable()
        {
            WheelStateRepository.OnDataChanged -= HandleWheelDataChanged;
            KillSpinTween();
        }

        private void HandleWheelDataChanged()
        {
            if (isInitialized)
            {
                LoadSnapshot();
                BindSegments();
                RefreshState();
            }
        }

        private void Update()
        {
            if (!_isSpinning)
            {
                RefreshCooldownTextOnly();
            }
        }

        private void LoadSnapshot()
        {
            _snapshot = config != null ? config.CreateSnapshot() : null;
        }

        private void BindSegments()
        {
            if (_snapshot == null || _snapshot.Rewards.Count == 0)
            {
                return;
            }

            int count = _snapshot.Rewards.Count;

            for (int i = 0; i < segmentViews.Count; i++)
            {
                WheelSegmentView view = segmentViews[i];
                if (view == null)
                {
                    continue;
                }

                if (i < count)
                {
                    view.gameObject.SetActive(true);
                    WheelRewardEntryData reward = _snapshot.Rewards[i];
                    view.Bind(reward);
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshState()
        {
            bool canSpin = CanSpinNow(out string reason);
            if (spinButton != null)
            {
                spinButton.interactable = canSpin;
            }

            if (resultText != null && !canSpin && string.IsNullOrWhiteSpace(resultText.text))
            {
                resultText.SetText(string.Empty);
            }

            RefreshStatusText(reason);
        }

        private bool CanSpinNow(out string reason)
        {
            reason = string.Empty;

            if (_isSpinning)
            {
                reason = "Spinning...";
                return false;
            }

            if (_snapshot == null)
            {
                reason = "Wheel config is missing.";
                return false;
            }

            if (_snapshot.Rewards.Count == 0 || !_snapshot.HasSpinableReward())
            {
                reason = "Wheel has no valid rewards.";
                return false;
            }

            if (segmentViews.Count > 0 && segmentViews.Count != _snapshot.Rewards.Count)
            {
                reason = "Wheel item count does not match reward data.";
                return false;
            }

            double remainingSeconds = _stateRepository.GetRemainingCooldownSeconds(_snapshot.CooldownSeconds);
            if (remainingSeconds > 0)
            {
                reason = $"Free spin in {FormatTime(remainingSeconds)}";
                return false;
            }

            reason = "Free spin ready!";
            return true;
        }

        private void HandleSpinClicked()
        {
            if (!CanSpinNow(out string reason))
            {
                RefreshStatusText(reason);
                UIManager.Instance?.ShowToast(reason);
                return;
            }

            int rewardIndex = WheelSpinService.SelectRewardIndex(_snapshot.Rewards, UnityEngine.Random.value);
            if (rewardIndex < 0)
            {
                RefreshStatusText("Wheel has no valid rewards.");
                return;
            }

            WheelRewardEntryData reward = _snapshot.Rewards[rewardIndex];
            PlaySpin(rewardIndex, reward);
        }

        private void PlaySpin(int rewardIndex, WheelRewardEntryData reward)
        {
            if (wheelRoot == null || reward == null)
            {
                Debug.LogWarning("[WheelSubScreen] Missing wheel root or reward data.", this);
                return;
            }

            KillSpinTween();
            _isSpinning = true;
            if (spinButton != null)
            {
                spinButton.interactable = false;
            }

            if (resultText != null)
            {
                resultText.SetText(string.Empty);
            }

            RefreshStatusText("Spinning...");

            float targetAngle = WheelSpinService.CalculateTargetZAngle(
                rewardIndex,
                _snapshot.Rewards.Count,
                wheelRoot.localEulerAngles.z,
                _snapshot.MinimumFullTurns,
                _snapshot.SegmentLandingPaddingDegrees,
                UnityEngine.Random.value,
                pointerAngleDegrees,
                spinDirection);

            float startAngle = wheelRoot.localEulerAngles.z;
            float spinDistance = targetAngle - startAngle;

            float totalDuration = _snapshot.SpinDuration + settleDuration;
            float targetRatio = Mathf.Abs(spinDistance) > 0.01f ? settleOvershootDegrees / Mathf.Abs(spinDistance) : 0f;

            float tZero;
            float c;
            if (targetRatio > 0.0001f)
            {
                // Clamp target ratio to a safe range to prevent extreme overshoot shapes
                targetRatio = Mathf.Min(targetRatio, 0.12f);
                tZero = FindTZero(targetRatio);
                c = 60f / (5f * tZero - 2f);
            }
            else
            {
                tZero = 1.0f;
                c = 20f;
            }

            _spinSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (startPunchScale > 0f && startPunchDuration > 0f)
            {
                _spinSequence.Join(wheelRoot
                    .DOPunchScale(Vector3.one * startPunchScale, startPunchDuration, 6, 0.65f)
                    .SetUpdate(useUnscaledTime));
            }

            int lastTickSegment = -1;
            int segmentCount = _snapshot.Rewards.Count;

            _spinSequence.Append(DOVirtual.Float(0f, 1f, totalDuration, progress =>
                {
                    float unifiedProgress = EvaluateUnifiedProgress(progress, tZero, c);
                    float angle = Mathf.LerpUnclamped(startAngle, targetAngle, unifiedProgress);
                    wheelRoot.localRotation = Quaternion.Euler(0f, 0f, angle);

                    if (segmentCount > 0)
                    {
                        float normalizedAngle = (angle % 360f + 360f) % 360f;
                        int currentSegment = Mathf.FloorToInt(normalizedAngle / (360f / segmentCount));
                        if (currentSegment != lastTickSegment)
                        {
                            lastTickSegment = currentSegment;
                            if (AudioController.Instance != null)
                            {
                                AudioController.Instance.PlayUISound(UISoundType.ClickNormal);
                            }
                            if (VibrationManager.Instance != null)
                            {
                                VibrationManager.Instance.PlayLightImpact(requireEnabled: true);
                            }
                        }
                    }
                })
                .SetEase(Ease.Linear));

            if (settleShakeStrength > 0f && settleShakeVibrato > 0 && settleDuration > 0f)
            {
                _spinSequence.Insert(_snapshot.SpinDuration, wheelRoot
                    .DOShakeRotation(settleDuration, new Vector3(0f, 0f, settleShakeStrength), settleShakeVibrato, 60f, false)
                    .SetUpdate(useUnscaledTime));
            }

            _spinSequence.OnComplete(() => CompleteSpin(reward));
        }

        private void CompleteSpin(WheelRewardEntryData reward)
        {
            _isSpinning = false;
            bool granted = WheelRewardGrantService.TryGrant(reward);
            if (granted)
            {
                if (AudioController.Instance != null)
                {
                    AudioController.Instance.PlayUISound(UISoundType.PurchaseSuccess);
                }
                if (VibrationManager.Instance != null)
                {
                    VibrationManager.Instance.PlayMediumImpact();
                }

                _stateRepository.MarkFreeSpinUsed();
                Debug.Log($"[WheelSubScreen] Reward granted: id='{reward.RewardId}', kind={reward.RewardKind}, amount={reward.Amount}, display='{reward.DisplayName}'.", this);
                if (resultText != null)
                {
                    resultText.SetText($"You got {reward.DisplayName}");
                }

                UIManager.Instance?.ShowToast($"You got {reward.DisplayName}!");
            }
            else
            {
                Debug.LogWarning($"[WheelSubScreen] Failed to grant reward: id='{reward.RewardId}', kind={reward.RewardKind}, amount={reward.Amount}, display='{reward.DisplayName}'.", this);
                UIManager.Instance?.ShowToast("Reward could not be granted.");
            }

            RefreshState();
        }

        private void RefreshCooldownTextOnly()
        {
            if (_snapshot == null)
            {
                return;
            }

            bool canSpin = CanSpinNow(out string reason);
            if (spinButton != null)
            {
                spinButton.interactable = canSpin;
            }

            RefreshStatusText(reason);
        }

        private void RefreshStatusText(string text)
        {
            if (statusText != null)
            {
                text ??= string.Empty;
                if (_lastStatusText == text)
                {
                    return;
                }

                _lastStatusText = text;
                statusText.SetText(text);
            }
        }

        private void KillSpinTween()
        {
            _spinSequence?.Kill();
            _spinSequence = null;
            _isSpinning = false;
        }

        private float EvaluateUnifiedProgress(float timeProgress, float tZero, float c)
        {
            float t = Mathf.Clamp01(timeProgress);
            float t2 = t * t;
            float t3 = t2 * t;
            float t4 = t3 * t;
            float t5 = t4 * t;

            float fRaw = tZero * (t2 / 2f)
                         - (2f * tZero + 1f) * (t3 / 3f)
                         + (tZero + 2f) * (t4 / 4f)
                         - (t5 / 5f);
            return c * fRaw;
        }

        private static float FindTZero(float targetRatio)
        {
            float low = 0.5f;
            float high = 0.88f;
            for (int i = 0; i < 15; i++)
            {
                float mid = (low + high) * 0.5f;
                float ratio = CalculateOvershootRatio(mid);
                if (ratio < targetRatio)
                {
                    high = mid; // Lower t_zero gives larger overshoot
                }
                else
                {
                    low = mid;
                }
            }
            return (low + high) * 0.5f;
        }

        private static float CalculateOvershootRatio(float tZero)
        {
            float c = 60f / (5f * tZero - 2f);
            float tZero2 = tZero * tZero;
            float tZero3 = tZero2 * tZero;
            float tZero4 = tZero3 * tZero;
            float tZero5 = tZero4 * tZero;

            float fRaw = tZero * (tZero2 / 2f)
                         - (2f * tZero + 1f) * (tZero3 / 3f)
                         + (tZero + 2f) * (tZero4 / 4f)
                         - (tZero5 / 5f);
            return c * fRaw - 1f;
        }

        private static string FormatTime(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
            }

            return $"{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}
