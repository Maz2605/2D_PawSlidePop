using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.Popup
{
    public class WinPopupCompleteSection : MonoBehaviour
    {
        [SerializeField] private PopupElement baseElement;
        [SerializeField] private Transform effect;
        [SerializeField] private Transform headline;
        [SerializeField] private PopupElement[] tier1, tier2, tier3;
        [SerializeField] private float tierGap = 0.1f;

        private Vector3 _effectOriginalScale;
        private Vector3 _headlineOriginalScale;
        private bool _cachedOriginalScales;

        private void Awake()
        {
            CacheOriginalScales();
        }

        public void Prepare()
        {
            CacheOriginalScales();
            baseElement?.PrepareForShow();
            if(effect) effect.localScale = Vector3.zero;
            if(headline) headline.localScale = Vector3.zero;
            
            foreach (var a in tier1) a?.PrepareForShow();
            foreach (var a in tier2) a?.PrepareForShow();
            foreach (var a in tier3) a?.PrepareForShow();
        }

        public Sequence GetShowSequence()
        {
            Sequence seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);

            if (baseElement != null) seq.Append(baseElement.DoPopUp());

            float start = 0.15f;
            InsertScaleTween(seq, effect, _effectOriginalScale, start);
            InsertScaleTween(seq, headline, _headlineOriginalScale, start + 0.05f);
            AddTierToSequence(seq, tier1, start);
            AddTierToSequence(seq, tier2, start + tierGap);
            AddTierToSequence(seq, tier3, start + tierGap * 2);
            
            return seq;
        }

        private void AddTierToSequence(Sequence s, PopupElement[] tier, float time)
        {
            if (tier == null) return;
            foreach (var a in tier) 
            {
                if (a != null) s.Insert(time, a.DoPopUp()); 
            }
        }

        private void CacheOriginalScales()
        {
            if (_cachedOriginalScales)
            {
                return;
            }

            _effectOriginalScale = effect ? effect.localScale : Vector3.one;
            _headlineOriginalScale = headline ? headline.localScale : Vector3.one;
            _cachedOriginalScales = true;
        }

        private void InsertScaleTween(Sequence sequence, Transform target, Vector3 targetScale, float atTime)
        {
            if (target == null)
            {
                return;
            }

            sequence.Insert(atTime,
                target.DOScale(targetScale, 0.28f)
                    .SetEase(Ease.OutBack)
                    .SetLink(gameObject));
        }
    }
}
