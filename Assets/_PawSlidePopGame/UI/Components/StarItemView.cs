using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    public enum StarVisualState
    {
        Locked,         
        ReachedNormal,  
        ReachedMax     
    }

    public class StarItemView : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private Image starImage;
        [SerializeField] private Image shadowImage;

        [Header("--- Sprites ---")]
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite yellowSprite;
        [FormerlySerializedAs("greenSprite")]
        [SerializeField] private Sprite superBlueSprite;

        [Header("--- VFX Hooks ---")]
        [SerializeField] private ParticleSystem unlockParticlePrefab;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float animDuration = 0.5f;
        [SerializeField] private float flyOffsetY = 30f;

        private ParticleSystem _cachedParticleInstance;
        private StarVisualState _currentState = StarVisualState.Locked;
        private Vector3 _initialLocalPosition;
       

        private void Awake()
        {
            if (starImage != null)
                _initialLocalPosition = starImage.transform.localPosition;
        }

        public void SetState(StarVisualState state, bool instant = true)
        {

            _currentState = state;
            starImage.DOKill();
            ApplyVisualState(state);

            if (instant)
            {
                starImage.transform.localScale = Vector3.one;
                starImage.transform.localPosition = _initialLocalPosition;
            }
        }

        public void PlayUnlockFx(StarVisualState newState, float delay = 0f, float startXOffset = 0f)
        {
            _currentState = newState;
            starImage.DOKill();
            starImage.transform.DOKill();

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            if (delay > 0f)
            {
                seq.AppendInterval(delay);
            }

            // Reset state và start từ scale 0, vị trí thấp hơn để bay lên từ bên trái
            seq.AppendCallback(() => 
            {
                ApplyVisualState(newState);
                starImage.transform.localScale = Vector3.zero;
                starImage.transform.localPosition = _initialLocalPosition + new Vector3(startXOffset, -flyOffsetY, 0f);
            });
            
            // Improved visual effect: Scale from 0 + Move to final position + Rotate 360 + settle
            seq.Append(starImage.transform.DOScale(1.3f, animDuration * 0.6f).SetEase(Ease.OutBack))
                .Join(starImage.transform.DOLocalMove(_initialLocalPosition, animDuration * 0.6f).SetEase(Ease.OutQuad))
                .Join(starImage.transform.DORotate(new Vector3(0f, 0f, -360f), animDuration * 0.6f, RotateMode.FastBeyond360).SetEase(Ease.InOutElastic).SetRelative(true))
                .Append(starImage.transform.DOScale(1f, animDuration * 0.4f).SetEase(Ease.InOutQuad))
                .SetLink(starImage.gameObject, LinkBehaviour.KillOnDisable);

            DOVirtual.DelayedCall(delay, PlayParticles).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void PlayBounce()
        {
            transform.DOKill(true);
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.25f, 5, 0.5f)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void ApplyVisualState(StarVisualState state)
        {

            switch (state)
            {
                case StarVisualState.Locked:
                    starImage.sprite = lockedSprite;
                    if (shadowImage != null)
                    {
                        shadowImage.enabled = false;
                    }

                    break;
                case StarVisualState.ReachedNormal:
                    starImage.sprite = yellowSprite;
                    if (shadowImage != null)
                    {
                        shadowImage.enabled = true;
                    }

                    break;
                case StarVisualState.ReachedMax:
                    starImage.sprite = superBlueSprite;
                    if (shadowImage != null)
                    {
                        shadowImage.enabled = true;
                    }
                    break;
            }
        }
        

        private void PlayParticles()
        {
            if (unlockParticlePrefab == null)
            {
                return;
            }

            if (_cachedParticleInstance == null)
            {
                _cachedParticleInstance = Instantiate(unlockParticlePrefab, transform);
                _cachedParticleInstance.transform.localPosition = starImage.transform.localPosition;
            }

            _cachedParticleInstance.Play();
        }
    }
}
