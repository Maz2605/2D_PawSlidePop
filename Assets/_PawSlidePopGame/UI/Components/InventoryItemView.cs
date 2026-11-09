using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    public class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtAmount;
        [SerializeField] private GameObject lockOverlay;

        public void SetData(Sprite icon, int amount, bool isLocked)
        {
            if (imgIcon != null)
            {
                imgIcon.sprite = icon;
                imgIcon.gameObject.SetActive(icon != null);
            }

            if (txtAmount != null)
            {
                txtAmount.text = $"x{amount}";
                txtAmount.gameObject.SetActive(amount > 0);
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(isLocked);
            }
        }
    }
}
