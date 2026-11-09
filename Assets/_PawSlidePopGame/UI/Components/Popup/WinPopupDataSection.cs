using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.Popup
{
    public class WinPopupDataSection : MonoBehaviour
    {
        [SerializeField] private PopupElement dataElement;
        [SerializeField] private TMP_Text levelText, scoreText;
        [SerializeField] private float countDuration = 0.6f;

        public void Prepare() => dataElement?.PrepareForShow();

        public Sequence GetShowSequence(int level, int score)
        {
            Sequence seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);

            if (dataElement != null) seq.Append(dataElement.DoPopUp());
            
            levelText?.SetText("Level {0}", level);
            
            int displayedScore = 0;
            // Tween chạy số điểm cũng cần được link chặt chẽ
            seq.Join(DOTween.To(() => displayedScore, x => {
                    displayedScore = x;
                    scoreText?.SetText("Your score: {0}", displayedScore);
                }, score, countDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject)); 

            return seq;
        }
    }
}