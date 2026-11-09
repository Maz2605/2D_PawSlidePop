using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.UI.Components.HUD;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.Popup
{
    public sealed class LosePopupProgressSection : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TargetListView targetListView;
        [SerializeField] private float countDuration = 0.6f;

        private int _displayedScore;

        public void Prepare()
        {
            scoreText?.DOKill();
            _displayedScore = 0;

            if (scoreText != null)
            {
                scoreText.SetText("Your score: {0}", 0);
            }

            if (levelText != null)
            {
                levelText.SetText("Level {0}", 0);
            }

            targetListView?.SetTargets(null);
        }

        public void Bind(GameplayHudSnapshot snapshot)
        {
            if (levelText != null)
            {
                levelText.SetText("Level {0}", snapshot?.levelNumber ?? 1);
            }

            _displayedScore = snapshot?.currentScore ?? 0;
            if (scoreText != null)
            {
                scoreText.SetText("Your score: {0}", _displayedScore);
            }

            targetListView?.SetTargets(snapshot?.targets);
        }

        public Sequence GetShowSequence(GameplayHudSnapshot snapshot)
        {
            if (levelText != null)
            {
                levelText.SetText("Level {0}", snapshot?.levelNumber ?? 1);
            }

            targetListView?.SetTargets(snapshot?.targets);

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            int targetScore = snapshot?.currentScore ?? 0;
            sequence.Append(DOTween.To(
                    () => _displayedScore,
                    value =>
                    {
                        _displayedScore = value;
                        scoreText?.SetText("Your score: {0}", _displayedScore);
                    },
                    targetScore,
                    countDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable));

            if (targetListView != null)
            {
                sequence.Join(targetListView.GetEntranceSequence());
            }

            return sequence;
        }
    }
}
