using System;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    /// <summary>
    /// Component quản lý hiển thị cho từng dòng phần thưởng trong màn hình Star Rewards.
    /// </summary>
    public sealed class RewardItemView : MonoBehaviour
    {
        [Header("Thông Tin Phần Thưởng")]
        [SerializeField] private Image rewardIconImage;
        [SerializeField] private TMP_Text rewardAmountText;
        [SerializeField] private TMP_Text requiredStarsText;
        [SerializeField] private Image itemBubbleImage;

        [Header("Hình Nền Dòng")]
        [SerializeField] private Image rowBackgroundImage;
        [SerializeField] private Color bgLockedColor = new Color(0.85f, 0.88f, 0.9f, 1f);     // Màu nền xám xanh nhạt
        [SerializeField] private Color bgUnlockedColor = Color.white;                             // Màu nền nguyên bản
        [SerializeField] private Color bgClaimedColor = new Color(0.75f, 0.78f, 0.8f, 1f);       // Màu nền xám tối hơn

        [Header("Nút Nhận")]
        [SerializeField] private Button getButton;
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private Image buttonIconImage; // Icon bên trong nút bấm

        [Header("Cấu Hình 3 Trạng Thái")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Sprite unlockedButtonIcon; // Icon móng vuốt mèo
        [SerializeField] private Sprite lockedButtonIcon;   // Icon ổ khóa
        [SerializeField] private Sprite claimedButtonIcon;  // Icon dấu check xanh lá
        [Space]
        [SerializeField] private Sprite unlockedButtonSprite; // Sprite nút màu xanh lá
        [SerializeField] private Sprite lockedButtonSprite;   // Sprite nút màu xám
        [SerializeField] private Sprite claimedButtonSprite;  // Sprite nút màu xám/xanh dương
        [Space]
        [SerializeField] private Color starsLockedColor = new Color(0.9f, 0.35f, 0.35f, 1f); // Đỏ pastel mềm mại
        [SerializeField] private Color starsUnlockedColor = Color.white;
        [SerializeField] private Color starsClaimedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [Space]
        [SerializeField] private Color btnTextUnlockedColor = Color.white;
        [SerializeField] private Color btnTextLockedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color btnTextClaimedColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        private StarRewardSO _reward;
        private Action<StarRewardSO> _onClaimClicked;

        public void Bind(StarRewardSO reward, Action<StarRewardSO> onClaimClicked, int playerStars, bool isClaimed)
        {
            _reward = reward;
            _onClaimClicked = onClaimClicked;

            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // 1. Giữ nguyên độ sáng và màu sắc của dòng (không làm mờ hay chuyển sang màu xám)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1.0f;
            }

            // 2. Giữ nguyên màu nền nguyên bản (BlueDark02) của dòng để không bị mất màu
            if (rowBackgroundImage != null)
            {
                rowBackgroundImage.color = Color.white;
            }

            // Icon & Amount
            if (rewardIconImage != null)
            {
                rewardIconImage.sprite = reward.Icon;
                rewardIconImage.enabled = reward.Icon != null;
            }

            if (itemBubbleImage != null)
            {
                // Bubble chỉ hiện khi vật phẩm bị Khóa (chưa nhận và chưa đủ sao)
                bool isLocked = !isClaimed && playerStars < reward.RequiredStars;
                itemBubbleImage.enabled = isLocked;
            }

            if (rewardAmountText != null)
            {
                if (reward.Rewards != null && reward.Rewards.Count == 1)
                {
                    rewardAmountText.gameObject.SetActive(true);
                    rewardAmountText.SetText("x{0}", reward.Rewards[0].Amount);
                }
                else
                {
                    // Gói nhiều phần thưởng (Bundle)
                    rewardAmountText.gameObject.SetActive(false);
                }
            }

            // 3. Số sao yêu cầu và màu sắc theo trạng thái (đỏ pastel cho khóa, xám cho đã nhận)
            if (requiredStarsText != null)
            {
                requiredStarsText.SetText("X {0}", reward.RequiredStars);
                if (isClaimed)
                    requiredStarsText.color = starsClaimedColor;
                else if (playerStars >= reward.RequiredStars)
                    requiredStarsText.color = starsUnlockedColor;
                else
                    requiredStarsText.color = starsLockedColor;
            }

            // 4. Cấu hình nút nhận, màu chữ và thay đổi hình dạng nút bấm theo trạng thái
            if (getButton != null && buttonText != null)
            {
                getButton.onClick.RemoveAllListeners();
                var btnImage = getButton.GetComponent<Image>();

                if (isClaimed)
                {
                    getButton.interactable = false;
                    buttonText.SetText("Claimed");
                    buttonText.color = btnTextClaimedColor;
                    if (btnImage != null && claimedButtonSprite != null)
                    {
                        btnImage.sprite = claimedButtonSprite;
                    }
                    if (buttonIconImage != null)
                    {
                        buttonIconImage.sprite = unlockedButtonIcon;
                        buttonIconImage.gameObject.SetActive(unlockedButtonIcon != null);
                    }
                }
                else if (playerStars >= reward.RequiredStars)
                {
                    getButton.interactable = true;
                    buttonText.SetText("Get");
                    buttonText.color = btnTextUnlockedColor;
                    if (btnImage != null && unlockedButtonSprite != null)
                    {
                        btnImage.sprite = unlockedButtonSprite;
                    }
                    if (buttonIconImage != null)
                    {
                        buttonIconImage.sprite = unlockedButtonIcon;
                        buttonIconImage.gameObject.SetActive(unlockedButtonIcon != null);
                    }
                    getButton.onClick.AddListener(OnGetButtonClicked);
                }
                else
                {
                    getButton.interactable = false;
                    buttonText.SetText("Locked");
                    buttonText.color = btnTextLockedColor;
                    if (btnImage != null && lockedButtonSprite != null)
                    {
                        btnImage.sprite = lockedButtonSprite;
                    }
                    if (buttonIconImage != null)
                    {
                        buttonIconImage.sprite = lockedButtonIcon;
                        buttonIconImage.gameObject.SetActive(lockedButtonIcon != null);
                    }
                }
            }
        }

        private void OnGetButtonClicked()
        {
            _onClaimClicked?.Invoke(_reward);
        }
    }
}
