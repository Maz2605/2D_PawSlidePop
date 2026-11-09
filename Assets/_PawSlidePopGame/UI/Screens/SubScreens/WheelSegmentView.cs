using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    public sealed class WheelSegmentView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text nameText;

        public void Bind(WheelRewardEntryData reward)
        {
            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (iconImage != null)
            {
                iconImage.sprite = reward.Icon;
                iconImage.enabled = reward.Icon != null;
                if (reward.Icon == null)
                {
                    Debug.LogWarning($"[WheelSegmentView] Reward '{reward.RewardId}' has no data icon.", this);
                }
            }

            if (amountText != null)
            {
                amountText.SetText("x{0}", reward.Amount);
            }

            if (nameText != null)
            {
                nameText.SetText(reward.DisplayName);
            }
        }
    }
}
