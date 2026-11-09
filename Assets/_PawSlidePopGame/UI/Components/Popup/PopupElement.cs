using System;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.Popup
{
    public class PopupElement : MonoBehaviour
    {
        [Header("1. Pop-up Movement (Trồi lên)")]
        [SerializeField] private float duration = 0.6f;
        [Tooltip("Khoảng cách Y bị kéo xuống trước khi trồi lên")]
        [SerializeField] private float appearOffsetY = -150f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        [Header("2. Squash & Stretch Curves (Kéo Curve ở đây)")]
        [Tooltip("Curve trục X: Thường bắt đầu hẹp, phình to ra khi tiếp đất, rồi nảy về 1")]
        [SerializeField] private AnimationCurve scaleXCurve;
        [Tooltip("Curve trục Y: Bắt đầu dài ra, ép dẹt xuống khi tiếp đất, rồi nảy về 1")]
        [SerializeField] private AnimationCurve scaleYCurve;

        [Header("3. Idle Floating (Đứng im nảy nảy)")]
        [SerializeField] private float idleBounceHeight = 10f;
        [SerializeField] private float idleDuration = 1.5f;
        [SerializeField] private bool playIdleLoop = true;

        private Vector3 originalScale;
        private Vector3 originalPosition;
        private Vector2 originalAnchoredPosition;
        private RectTransform rectTransform;
        private Tween popTween;
        private Tween idleTween;

        public float Duration => duration;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
            originalAnchoredPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            PrepareForShow();
        }

        public void PrepareForShow()
        {
            popTween?.Kill();
            idleTween?.Kill();
            transform.localScale = Vector3.zero;

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(0f, appearOffsetY);
                return;
            }

            transform.localPosition = originalPosition + new Vector3(0f, appearOffsetY, 0f);
        }

        public void SetIdleEnabled(bool enabled)
        {
            playIdleLoop = enabled;
            if (!enabled)
            {
                idleTween?.Kill();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = originalAnchoredPosition;
                }
                else
                {
                    transform.localPosition = originalPosition;
                }
            }
        }

        public Tween PlayAnimation(float delay = 0f, Action onComplete = null)
        {
            Tween tween = DoPopUp();
            if (delay > 0f)
            {
                tween.SetDelay(delay);
            }

            if (onComplete != null)
            {
                tween.OnComplete(() =>
                {
                    StartIdleAnimation();
                    onComplete.Invoke();
                });
            }

            return tween;
        }

        public Tween DoPopUp()
        {
            PrepareForShow();

            Sequence popSequence = DOTween.Sequence().SetUpdate(true);

            if (rectTransform != null)
            {
                popSequence.Join(rectTransform.DOAnchorPosY(originalAnchoredPosition.y, duration).SetEase(moveEase));
            }
            else
            {
                popSequence.Join(transform.DOLocalMoveY(originalPosition.y, duration).SetEase(moveEase));
            }

            Tween scaleXTween = transform.DOScaleX(originalScale.x, duration);
            Tween scaleYTween = transform.DOScaleY(originalScale.y, duration);

            if (HasCurve(scaleXCurve))
            {
                scaleXTween.SetEase(scaleXCurve);
            }
            else
            {
                scaleXTween.SetEase(Ease.OutBack);
            }

            if (HasCurve(scaleYCurve))
            {
                scaleYTween.SetEase(scaleYCurve);
            }
            else
            {
                scaleYTween.SetEase(Ease.OutBack);
            }

            popSequence.Join(scaleXTween);
            popSequence.Join(scaleYTween);
            popSequence.OnComplete(StartIdleAnimation);
            popSequence.SetLink(gameObject);

            popTween = popSequence;
            return popSequence;
        }

        private void StartIdleAnimation()
        {
            idleTween?.Kill();
            if (!playIdleLoop)
            {
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = originalAnchoredPosition;
                }
                else
                {
                    transform.localPosition = originalPosition;
                }
                return;
            }

            if (rectTransform != null)
            {
                idleTween = rectTransform.DOAnchorPosY(originalAnchoredPosition.y + idleBounceHeight, idleDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            }
            else
            {
                idleTween = transform.DOLocalMoveY(originalPosition.y + idleBounceHeight, idleDuration)
                    .SetUpdate(true)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            }
        }

        private void OnDestroy()
        {
            popTween?.Kill();
            idleTween?.Kill();
        }

        private static bool HasCurve(AnimationCurve curve)
        {
            return curve != null && curve.keys != null && curve.keys.Length > 0;
        }
    }
}
