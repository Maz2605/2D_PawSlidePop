using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorItemTileButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Transform hoverScaleTarget;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image shadowImage;
        [SerializeField] private GameObject selectedMarker;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverDuration = 0.16f;
        [SerializeField] private Ease hoverEase = Ease.OutBack;
        [SerializeField] private float unhoverDuration = 0.12f;
        [SerializeField] private Ease unhoverEase = Ease.OutQuad;
        [SerializeField] private Color shadowDefaultColor = Color.black;
        [SerializeField] private Color shadowHoverColor = Color.white;

        private int _contentId;
        private Action<int> _onClicked;
        private Tween _scaleTween;
        private Tween _shadowTween;
        private Vector3 _baseScale = Vector3.one;

        public void Bind(LevelEditorPaletteEntryData entry, bool selected, Action<int> onClick)
        {
            _contentId = entry != null ? entry.Id : 0;
            _onClicked = onClick;

            if (hoverScaleTarget != null)
            {
                _baseScale = hoverScaleTarget.localScale;
            }

            if (labelText != null)
            {
                labelText.text = entry != null ? entry.Description : string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = entry != null ? entry.Icon : null;
                iconImage.enabled = entry != null && entry.Icon != null;
            }

            if (shadowImage != null)
            {
                shadowImage.sprite = entry != null ? entry.Icon : null;
                shadowImage.enabled = entry != null && entry.Icon != null;
                shadowImage.color = shadowDefaultColor;
            }

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(selected);
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_contentId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverScaleTarget != null)
            {
                _scaleTween?.Kill(false);
                _scaleTween = hoverScaleTarget.DOScale(_baseScale * hoverScale, hoverDuration)
                    .SetEase(hoverEase)
                    .SetLink(gameObject);
            }

            if (shadowImage != null)
            {
                _shadowTween?.Kill(false);
                _shadowTween = shadowImage.DOColor(shadowHoverColor, hoverDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverScaleTarget != null)
            {
                _scaleTween?.Kill(false);
                _scaleTween = hoverScaleTarget.DOScale(_baseScale, unhoverDuration)
                    .SetEase(unhoverEase)
                    .SetLink(gameObject);
            }

            if (shadowImage != null)
            {
                _shadowTween?.Kill(false);
                _shadowTween = shadowImage.DOColor(shadowDefaultColor, unhoverDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
            }
        }

        private void OnDisable()
        {
            _scaleTween?.Kill(false);
            _shadowTween?.Kill(false);
        }
    }
}
