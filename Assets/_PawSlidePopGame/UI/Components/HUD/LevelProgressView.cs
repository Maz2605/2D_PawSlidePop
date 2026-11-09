using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class LevelProgressView : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private List<StarItemView> starViews = new List<StarItemView>(); 

        public Slider ProgressSlider => progressSlider; 

        [Header("--- Super State (Sao 4) ---")]
        [Tooltip("Kéo Image nằm trong phần Fill của Slider vào đây")]
        [SerializeField] private Image sliderFillImage; 
        [SerializeField] private Sprite normalFillSprite; // Hình thanh bar lúc bình thường (Vàng/Cam)
        [SerializeField] private Sprite superFillSprite;  // Hình thanh bar lúc đạt sao 4 (Xanh lá)

        [Header("--- Animation Settings ---")]
        [SerializeField] private float sliderAnimDuration = 0.5f;

        private int _visualMaxStars = 3; // Giới hạn số sao vật lý hiện trên thanh
        private int _totalStarsConfigured;
        private int _visualMaxScore = 1;
        private int _superScore = -1;
        private int _currentScore;
        private int _visualScore;
        private int _visualReachedStars;
        private int[] _starThresholds;
        private int _lastReachedStarIndex = -1;
        private Tween _sliderTween;

        public void SetData(GameplayHudSnapshot snapshot)
        {
            if (snapshot == null) return;

            if (levelText != null) levelText.SetText("Level {0}", snapshot.levelNumber);

            _totalStarsConfigured = snapshot.starScoreThresholds != null ? snapshot.starScoreThresholds.Length : 0;
            _currentScore = snapshot.currentScore;
            _visualScore = snapshot.currentScore;
            _visualReachedStars = snapshot.reachedStars;
            _lastReachedStarIndex = snapshot.reachedStars - 1;
            _starThresholds = snapshot.starScoreThresholds;
            
            // 1. Xác định mốc điểm để render UI
            _visualMaxScore = 1;
            _superScore = -1; // Điểm cần để đạt sao 4

            if (_totalStarsConfigured >= _visualMaxStars)
            {
                // Mốc max của thanh Slider = điểm của sao thứ 3
                _visualMaxScore = Mathf.Max(1, snapshot.starScoreThresholds[_visualMaxStars - 1]);
                
                // Nếu data có cấu hình sao thứ 4
                if (_totalStarsConfigured > _visualMaxStars)
                {
                    _superScore = snapshot.starScoreThresholds[_visualMaxStars];
                }
            }

            // 2. Chạy thanh Slider (Chỉ chạy tối đa đến mốc sao 3 là full 100%)
            UpdateSliderVisual(snapshot.currentScore, true);

            // 3. Align vị trí 3 ngôi sao theo chuẩn điểm của sao thứ 3
            AlignStarsToThresholds(snapshot.starScoreThresholds, _visualMaxScore);

            // 4. Xử lý trạng thái hiển thị của Sao và Slider
            bool isSuperReached = _superScore > 0 && snapshot.currentScore >= _superScore;
            
            // Đổi hình Slider
            if (sliderFillImage != null)
            {
                sliderFillImage.sprite = isSuperReached ? superFillSprite : normalFillSprite;
            }

            // Khôi phục trạng thái cho các sao
            for (int i = 0; i < starViews.Count; i++)
            {
                if (starViews[i] != null)
                {
                    bool isUnlocked = i < snapshot.reachedStars;
                    StarVisualState targetState = DetermineStarState(i, isUnlocked, isSuperReached);
                    starViews[i].SetState(targetState, instant: true);
                }
            }
        }

        public void PlayScoreChangedFx(ScoreChangedPayload payload)
        {
            _currentScore = payload.CurrentScore;
            _visualScore = payload.CurrentScore;
            _visualReachedStars = GameplayHudSnapshotBuilder.CountReachedStars(_currentScore, _starThresholds);
            _lastReachedStarIndex = _visualReachedStars - 1;
            UpdateSliderVisual(_currentScore, true);
        }

        public void ResetView()
        {
            _sliderTween?.Kill();
            _sliderTween = null;

            _visualMaxScore = 1;
            _superScore = -1;
            _currentScore = 0;
            _visualScore = 0;
            _visualReachedStars = 0;
            _starThresholds = null;
            _totalStarsConfigured = 0;
            _lastReachedStarIndex = -1;

            if (levelText != null)
            {
                levelText.text = string.Empty;
            }

            if (progressSlider != null)
            {
                progressSlider.DOKill();
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = 0f;
                progressSlider.transform.localScale = Vector3.one;
            }

            if (sliderFillImage != null)
            {
                sliderFillImage.sprite = normalFillSprite;
            }

            for (int i = 0; i < starViews.Count; i++)
            {
                if (starViews[i] == null)
                {
                    continue;
                }

                starViews[i].gameObject.SetActive(false);
                starViews[i].SetState(StarVisualState.Locked, instant: true);
            }
        }

        // ... Hàm AlignStarsToThresholds giữ nguyên logic như bản trước ...
        private void AlignStarsToThresholds(int[] thresholds, int maxScore)
        {
            if (thresholds == null || maxScore <= 0) return;

            for (int i = 0; i < starViews.Count; i++)
            {
                if (starViews[i] == null) continue;

                // Chỉ xử lý 3 sao vật lý. Các sao dư thừa trong list sẽ bị ẩn.
                if (i >= _visualMaxStars || i >= thresholds.Length)
                {
                    starViews[i].gameObject.SetActive(false);
                    continue;
                }

                starViews[i].gameObject.SetActive(true);
                float ratio = Mathf.Clamp01((float)thresholds[i] / maxScore);

                RectTransform starRect = starViews[i].GetComponent<RectTransform>();
                if (starRect != null)
                {
                    starRect.anchorMin = new Vector2(ratio, 0.5f);
                    starRect.anchorMax = new Vector2(ratio, 0.5f);
                    starRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        public void PlayStarReachedFx(StarReachedPayload payload)
        {
            int starIndex = payload.StarIndex; 
            _lastReachedStarIndex = starIndex - 1;

            if (starIndex > _visualMaxStars)
            {
                TriggerSuperStateFx();
                return;
            }

            int viewIndex = starIndex - 1;
            if (viewIndex < 0 || viewIndex >= starViews.Count || starViews[viewIndex] == null) return;

            StarVisualState newState = DetermineStarState(viewIndex, true, false);
            starViews[viewIndex].PlayUnlockFx(newState, delay: sliderAnimDuration);
        }

        private void TriggerSuperStateFx()
        {
            _lastReachedStarIndex = starViews.Count - 1;
            if (sliderFillImage != null)
            {
                DOVirtual.DelayedCall(sliderAnimDuration, () => 
                {
                    sliderFillImage.sprite = superFillSprite;
                    
                    progressSlider.transform.DOKill();
                    progressSlider.transform.localScale = Vector3.one;
                    progressSlider.transform.DOPunchScale(new Vector3(0.05f, 0.1f, 0f), 0.3f, 5)
                        .SetLink(progressSlider.gameObject, LinkBehaviour.KillOnDisable);

                    // 2. Ép toàn bộ 3 sao vật lý chuyển sang trạng thái xanh (ReachedMax)
                    for (int i = 0; i < starViews.Count; i++)
                    {
                        if (starViews[i] != null && starViews[i].gameObject.activeSelf)
                        {
                            starViews[i].PlayUnlockFx(StarVisualState.ReachedMax, delay: 0f);
                        }
                    }
                }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private StarVisualState DetermineStarState(int index, bool isUnlocked, bool isSuperReached)
        {
            if (!isUnlocked) return StarVisualState.Locked;
            
            if (isSuperReached) return StarVisualState.ReachedMax;

            return StarVisualState.ReachedNormal; 
        }

        private void UpdateSliderVisual(int score, bool animate)
        {
            if (progressSlider == null)
            {
                return;
            }

            progressSlider.minValue = 0f;
            progressSlider.maxValue = _visualMaxScore;

            float targetValue = Mathf.Clamp(score, 0, _visualMaxScore);
            _sliderTween?.Kill();
            if (!animate)
            {
                progressSlider.value = targetValue;
                return;
            }

            _sliderTween = progressSlider.DOValue(targetValue, sliderAnimDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(progressSlider.gameObject, LinkBehaviour.KillOnDisable);
        }

        public void PlayBounceFx()
        {
            if (starViews == null || starViews.Count == 0) return;

            int targetIndex = _lastReachedStarIndex;
            if (targetIndex < 0)
            {
                targetIndex = 0; // Bounce the first star if none reached yet
            }
            else if (targetIndex >= starViews.Count)
            {
                targetIndex = starViews.Count - 1; // Bounce the last physical star if in Super state / more than max stars
            }

            var star = starViews[targetIndex];
            if (star != null)
            {
                star.PlayBounce();
            }
        }

        public void AddScoreVisually(int amount)
        {
            _visualScore += amount;
            UpdateSliderVisual(_visualScore, true);

            if (_starThresholds != null)
            {
                int newReachedStars = 0;
                for (int i = 0; i < _starThresholds.Length; i++)
                {
                    if (_visualScore >= _starThresholds[i])
                    {
                        newReachedStars = i + 1;
                    }
                }

                if (newReachedStars > _visualReachedStars)
                {
                    for (int starIndex = _visualReachedStars + 1; starIndex <= newReachedStars; starIndex++)
                    {
                        PlayStarReachedFx(new StarReachedPayload(starIndex, _visualScore));
                    }
                    _visualReachedStars = newReachedStars;
                    _lastReachedStarIndex = _visualReachedStars - 1;
                }
            }
        }
    }
}
