using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    [DisallowMultipleComponent]
    public sealed class TextSwingElement : MonoBehaviour
    {
        [Header("--- Swing Settings ---")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool resetRotationOnDisable = true;
        [SerializeField] private float swingAngle = 3f;
        [SerializeField] private float halfSwingDuration = 0.45f;
        [SerializeField] private Ease swingEase = Ease.InOutSine;

        private Tween _swingTween;
        private Vector3 _baseEulerAngles;

        private void Awake()
        {
            _baseEulerAngles = transform.localEulerAngles;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop(resetRotationOnDisable);
        }

        public void Play()
        {
            Stop(resetRotation: true);

            Vector3 targetEulerAngles = _baseEulerAngles + new Vector3(0f, 0f, swingAngle);
            transform.localEulerAngles = _baseEulerAngles + new Vector3(0f, 0f, -swingAngle);

            _swingTween = transform.DOLocalRotate(targetEulerAngles, halfSwingDuration)
                .SetEase(swingEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void Stop(bool resetRotation = true)
        {
            _swingTween?.Kill();
            _swingTween = null;

            if (resetRotation)
            {
                transform.localEulerAngles = _baseEulerAngles;
            }
        }
    }
}
