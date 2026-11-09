using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.Popup
{
    public class WinPopupStarSection : MonoBehaviour
    {
        [SerializeField] private PopupElement starsElement;
        [SerializeField] private List<StarItemView> starViews = new List<StarItemView>();
        [SerializeField] private float starDelay = 0.08f;
        [SerializeField] private float starHorizontalOffset = 30f;
        [SerializeField] private HorizontalLayoutGroup starsLayoutGroup;

        public void Prepare() => starsElement?.PrepareForShow();

        public void Bind(int reachedStars)
        {
            for (int i = 0; i < starViews.Count; i++)
                starViews[i]?.SetState(StarVisualState.Locked, true);
        }

        public Sequence GetShowSequence(int reachedStars)
        {
            // SetLink giúp tự động Kill sequence nếu StarSection bị destroy
            Sequence seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject); 

            if (starsLayoutGroup != null)
                seq.AppendCallback(() => starsLayoutGroup.enabled = false);

            if (starsElement != null) seq.Join(starsElement.DoPopUp());
            
            bool isSuper = reachedStars >= 4;
            int visualStars = Mathf.Min(reachedStars, starViews.Count);

            float xRange = starHorizontalOffset * 2f;
            for (int i = 0; i < visualStars; i++)
            {
                var star = starViews[i];
                var state = isSuper ? StarVisualState.ReachedMax : StarVisualState.ReachedNormal;
                float xStart = visualStars > 1 ? -starHorizontalOffset + i * (xRange / (visualStars - 1)) : 0f;
                // Play từng sao một, bay lên từ trái sang phải bằng x offset
                seq.Insert(i * starDelay, DOVirtual.DelayedCall(0f, () => star.PlayUnlockFx(state, 0f, xStart))
                    .SetLink(star.gameObject)); 
            }

            if (starsLayoutGroup != null)
                seq.AppendCallback(() => starsLayoutGroup.enabled = true);

            return seq;
        }
    }
}