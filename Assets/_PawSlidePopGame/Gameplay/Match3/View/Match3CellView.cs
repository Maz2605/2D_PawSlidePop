using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3CellView : MonoBehaviour, IPoolable
    {
        [SerializeField] private Transform tileAnchor;
        [SerializeField] private Transform overlayTileAnchor;
        [SerializeField] private SpriteRenderer fallbackRenderer;

        private Sprite _initialSprite;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;

        public Transform TileAnchor => tileAnchor != null ? tileAnchor : transform;
        public Transform OverlayAnchor => overlayTileAnchor != null ? overlayTileAnchor : TileAnchor;
        public SpriteRenderer FallbackRenderer => fallbackRenderer;

        private void Awake()
        {
            EnsureRuntimeDefaults();
        }

        public void Initialize(_PawSlidePopGame._Scripts.Feature.Match3.Model.Board.CellModel cell)
        {
            if (tileAnchor == null)
            {
                tileAnchor = transform;
            }
        }

        public Vector3 GetTileAnchorLocalPosition(Transform relativeTo)
        {
            return GetTileAnchorLocalPosition(TileStackLayer.Base, relativeTo);
        }

        public Vector3 GetTileAnchorLocalPosition(TileStackLayer layer, Transform relativeTo)
        {
            Transform anchor = layer == TileStackLayer.Overlay ? OverlayAnchor : TileAnchor;
            if (relativeTo == null)
            {
                return anchor.position;
            }

            return relativeTo.InverseTransformPoint(anchor.position);
        }

        public void OnSpawn()
        {
            EnsureRuntimeDefaults();
            ResetRuntimeState();
        }

        public void OnDespawn()
        {
            ResetRuntimeState();
        }

        private void EnsureRuntimeDefaults()
        {
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;

            if (fallbackRenderer != null && _initialSprite == null)
            {
                _initialSprite = fallbackRenderer.sprite;
            }

            if (tileAnchor == null)
            {
                tileAnchor = transform;
            }

            if (overlayTileAnchor == null)
            {
                overlayTileAnchor = tileAnchor;
            }
        }

        private void ResetRuntimeState()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialLocalScale;
            if (fallbackRenderer != null)
            {
                fallbackRenderer.sprite = _initialSprite;
            }
        }
    }
}

