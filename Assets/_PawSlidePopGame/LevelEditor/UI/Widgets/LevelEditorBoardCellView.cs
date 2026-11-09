using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public enum LevelEditorBoardCellAction
    {
        Paint = 0,
        Erase = 1
    }

    public sealed class LevelEditorBoardCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float LayerTweenDuration = 0.12f;
        private const float SelectionTweenDuration = 0.1f;
        private const float PlayableTweenDuration = 0.1f;

        [SerializeField] private TextMeshPro coordinateText;
        [SerializeField] private TextMeshPro valueText;
        [SerializeField] private SpriteRenderer cellArtImage;
        [SerializeField] private SpriteRenderer underlayImage;
        [SerializeField] private SpriteRenderer itemImage;
        [SerializeField] private SpriteRenderer overlayImage;
        [SerializeField] private SpriteRenderer selectedHighlight;
        [SerializeField] private SpriteRenderer playableGraphic;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private Color playableColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        private static bool _isDraggingAction;
        private static LevelEditorBoardCellAction _dragAction;
        private int _x;
        private int _y;
        private bool _hasBound;
        private bool _lastSelected;
        private bool _lastPlayable;
        private Sprite _lastCellArtSprite;
        private Sprite _lastUnderlaySprite;
        private Sprite _lastItemSprite;
        private Sprite _lastOverlaySprite;
        private Action<int, int, LevelEditorBoardCellAction> _onActionRequested;
        private LevelEditorUIController _service;
        private Sprite _defaultSprite;
        private bool _isAwake;

        private void Awake()
        {
            EnsureAwake();
        }

        private void EnsureAwake()
        {
            if (_isAwake) return;
            _isAwake = true;

            if (playableGraphic != null)
            {
                _defaultSprite = playableGraphic.sprite;
                if (_defaultSprite == null)
                {
                    Texture2D texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _defaultSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                    playableGraphic.sprite = _defaultSprite;
                }
            }
        }

        public void Bind(
            int x,
            int y,
            Sprite cellArtSprite,
            Sprite underlaySprite,
            Sprite itemSprite,
            Sprite overlaySprite,
            Sprite cellBackgroundSprite,
            bool selected,
            bool playable,
            Action<int, int, LevelEditorBoardCellAction> onActionRequested,
            LevelEditorUIController service)
        {
            EnsureAwake();
            _x = x;
            _y = y;
            _onActionRequested = onActionRequested;
            _service = service;

            if (coordinateText != null)
            {
                coordinateText.text = string.Empty; // Hide coordinates on cells
            }

            if (valueText != null)
            {
                valueText.text = string.Empty;
            }

            if (playableGraphic != null)
            {
                playableGraphic.sprite = cellBackgroundSprite != null ? cellBackgroundSprite : _defaultSprite;
            }

            EnsureCellArtImage();
            bool animate = Application.isPlaying && _hasBound;
            SetLayerImage(cellArtImage, playable ? cellArtSprite : null, _lastCellArtSprite, animate);
            SetLayerImage(underlayImage, playable ? underlaySprite : null, _lastUnderlaySprite, animate);
            SetLayerImage(itemImage, playable ? itemSprite : null, _lastItemSprite, animate);
            SetLayerImage(overlayImage, playable ? overlaySprite : null, _lastOverlaySprite, animate);
            SetSelected(selected, animate && selected != _lastSelected);
            SetPlayable(playable, animate && playable != _lastPlayable);

            if (emptyStateRoot != null)
            {
                emptyStateRoot.SetActive(playable && cellArtSprite == null && underlaySprite == null && itemSprite == null && overlaySprite == null);
            }

            _lastCellArtSprite = cellArtSprite;
            _lastUnderlaySprite = underlaySprite;
            _lastItemSprite = itemSprite;
            _lastOverlaySprite = overlaySprite;
            _lastSelected = selected;
            _lastPlayable = playable;
            _hasBound = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left &&
                eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            _dragAction = eventData.button == PointerEventData.InputButton.Right
                ? LevelEditorBoardCellAction.Erase
                : LevelEditorBoardCellAction.Paint;
            _isDraggingAction = true;
            RequestAction(_dragAction);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _service?.SetHoveredCoordinate(_x, _y);
            if (_isDraggingAction)
            {
                RequestAction(_dragAction);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _service?.SetHoveredCoordinate(-1, -1);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if ((eventData.button == PointerEventData.InputButton.Left && _dragAction == LevelEditorBoardCellAction.Paint) ||
                (eventData.button == PointerEventData.InputButton.Right && _dragAction == LevelEditorBoardCellAction.Erase))
            {
                _isDraggingAction = false;
            }
        }

        private void OnDisable()
        {
            _isDraggingAction = false;
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void RequestAction(LevelEditorBoardCellAction action)
        {
            _onActionRequested?.Invoke(_x, _y, action);
        }

        private static void SetLayerImage(SpriteRenderer renderer, Sprite nextSprite, Sprite previousSprite, bool animate)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.DOKill(false);
            renderer.transform.DOKill(false);

            if (!animate)
            {
                renderer.sprite = nextSprite;
                renderer.enabled = nextSprite != null;
                SetGraphicAlpha(renderer, nextSprite != null ? 1f : 0f);
                renderer.transform.localScale = Vector3.one;
                return;
            }

            if (nextSprite == previousSprite)
            {
                if (nextSprite != null)
                {
                    renderer.sprite = nextSprite;
                    renderer.enabled = true;
                    SetGraphicAlpha(renderer, 1f);
                    renderer.transform.localScale = Vector3.one;
                }
                return;
            }

            if (nextSprite == null)
            {
                renderer.DOFade(0f, LayerTweenDuration).SetEase(Ease.OutQuad);
                renderer.transform.DOScale(0.85f, LayerTweenDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        renderer.sprite = null;
                        renderer.enabled = false;
                        renderer.transform.localScale = Vector3.one;
                    });
                return;
            }

            renderer.sprite = nextSprite;
            renderer.enabled = true;
            SetGraphicAlpha(renderer, 0f);
            renderer.transform.localScale = Vector3.one * 0.85f;
            renderer.DOFade(1f, LayerTweenDuration).SetEase(Ease.OutQuad);
            renderer.transform.DOScale(1f, LayerTweenDuration).SetEase(Ease.OutBack);
        }

        private void SetSelected(bool selected, bool animate)
        {
            if (selectedHighlight == null)
            {
                return;
            }

            selectedHighlight.DOKill(false);
            selectedHighlight.transform.DOKill(false);

            if (!animate)
            {
                selectedHighlight.enabled = selected;
                SetGraphicAlpha(selectedHighlight, selected ? 1f : 0f);
                selectedHighlight.transform.localScale = Vector3.one;
                return;
            }

            if (selected)
            {
                selectedHighlight.enabled = true;
                SetGraphicAlpha(selectedHighlight, 0f);
                selectedHighlight.transform.localScale = Vector3.one * 0.95f;
                selectedHighlight.DOFade(1f, SelectionTweenDuration).SetEase(Ease.OutQuad);
                selectedHighlight.transform.DOScale(1f, SelectionTweenDuration).SetEase(Ease.OutBack);
                return;
            }

            selectedHighlight.DOFade(0f, SelectionTweenDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => selectedHighlight.enabled = false);
        }

        private void SetPlayable(bool playable, bool animate)
        {
            if (playableGraphic == null)
            {
                return;
            }

            playableGraphic.DOKill(false);
            if (playable)
            {
                playableGraphic.enabled = true;
                bool hasCustomSprite = playableGraphic.sprite != null && playableGraphic.sprite != _defaultSprite;
                Color targetColor = hasCustomSprite ? Color.white : playableColor;
                if (animate)
                {
                    playableGraphic.DOColor(targetColor, PlayableTweenDuration).SetEase(Ease.OutQuad);
                }
                else
                {
                    playableGraphic.color = targetColor;
                }
            }
            else
            {
                if (animate)
                {
                    playableGraphic.DOFade(0f, PlayableTweenDuration).OnComplete(() => playableGraphic.enabled = false);
                }
                else
                {
                    Color c = playableGraphic.color;
                    c.a = 0f;
                    playableGraphic.color = c;
                    playableGraphic.enabled = false;
                }
            }
        }

        private void KillTweens()
        {
            KillGraphicTween(cellArtImage);
            KillGraphicTween(underlayImage);
            KillGraphicTween(itemImage);
            KillGraphicTween(overlayImage);
            KillGraphicTween(selectedHighlight);
            KillGraphicTween(playableGraphic);
        }

        private void EnsureCellArtImage()
        {
            if (cellArtImage != null)
            {
                return;
            }

            GameObject go = new GameObject("CellArtImage", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex(0);

            cellArtImage = go.GetComponent<SpriteRenderer>();
            cellArtImage.enabled = false;
        }

        private static void KillGraphicTween(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.DOKill(false);
            renderer.transform.DOKill(false);
        }

        private static void SetGraphicAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
