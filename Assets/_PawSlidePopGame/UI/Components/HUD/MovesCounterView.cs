using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class MovesCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesAmountText;
        
        [Header("Animation Settings")]
        [SerializeField] private float countDuration = 0.4f;
        [SerializeField] private float bounceScale = 1.3f; 
        [SerializeField] private float bounceDuration = 0.35f; 

        private int _currentDisplayValue = -1;
        private int _targetValue;
        private Tween _scaleTween;

        public void SetValue(int moves)
        {
            _targetValue = moves;
            
            if (_currentDisplayValue == -1)
            {
                _currentDisplayValue = moves;
                UpdateTextUI(_currentDisplayValue);
            }
        }

        public void ResetView(int value = 0)
        {
            movesAmountText?.DOKill();
            _scaleTween?.Kill();
            _scaleTween = null;

            _currentDisplayValue = -1;
            _targetValue = value;

            if (movesAmountText != null)
            {
                movesAmountText.transform.localScale = Vector3.one;
            }

            UpdateTextUI(value);
        }

        public void PlayValueChangedFx()
        {
            if (movesAmountText == null || _currentDisplayValue == _targetValue) 
            {
                return;
            }

            movesAmountText.DOKill();
            _scaleTween?.Kill();

            DOTween.To(() => _currentDisplayValue, x => 
            {
                _currentDisplayValue = x;
                UpdateTextUI(_currentDisplayValue);
            }, _targetValue, countDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(movesAmountText.gameObject, LinkBehaviour.KillOnDisable);

            Transform textTransform = movesAmountText.transform;
            textTransform.localScale = Vector3.one; 

            Sequence seq = DOTween.Sequence();
            seq.Append(textTransform.DOScale(bounceScale, bounceDuration * 0.3f).SetEase(Ease.OutQuad))
               .Append(textTransform.DOScale(1f, bounceDuration * 0.7f).SetEase(Ease.OutBack));

            _scaleTween = seq.SetLink(movesAmountText.gameObject, LinkBehaviour.KillOnDisable);
        }

        private void UpdateTextUI(int value)
        {
            if (movesAmountText == null) return;
            movesAmountText.SetText("{0}", value);
        }
    }
}
