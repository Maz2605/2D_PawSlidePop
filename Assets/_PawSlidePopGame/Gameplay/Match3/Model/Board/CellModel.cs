using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Board
{
    public class CellModel
    {
        public int X { get; }
        public int Y { get; }
        public bool IsPlayable { get; }
        public UnderlayContentModel Underlay { get; private set; }
        public TileContentModel Tile { get; private set; }
        public OverlayContentModel Overlay { get; private set; }
        public TileModel TopTile => (TileModel)Overlay ?? Tile;
        public Action<TileModel> OnTileChanged;

        public CellModel(int x, int y, bool isPlayable)
        {
            X = x;
            Y = y;
            IsPlayable = isPlayable;
        }

        public void SetTile(TileModel newTile)
        {
            Tile = newTile as TileContentModel;
            OnTileChanged?.Invoke(Tile);
        }

        public void SetUnderlay(TileModel newTile)
        {
            Underlay = newTile as UnderlayContentModel;
            OnTileChanged?.Invoke(Tile);
        }

        public void SetOverlay(TileModel newTile)
        {
            if (ReferenceEquals(Overlay, newTile))
            {
                return;
            }

            if (newTile != null && (!newTile.Definition.AllowsOverlayPlacement || !CanAcceptOverlay()))
            {
                UnityEngine.Debug.Log($"[Cell] SetOverlay: Clear overlay on cell ({X}, {Y}) because newTile != null and CanAcceptOverlay() is false");
                Overlay = null;
                OnTileChanged?.Invoke(Tile);
                return;
            }

            Overlay = newTile as OverlayContentModel;
            OnTileChanged?.Invoke(Tile);
        }

        public void ClearTile()
        {
            Tile = null;
            OnTileChanged?.Invoke(null);
        }

        public void ClearOverlay()
        {
            UnityEngine.Debug.Log($"[Cell] ClearOverlay called for Cell ({X}, {Y})");
            Overlay = null;
            OnTileChanged?.Invoke(Tile);
        }

        public void ClearUnderlay()
        {
            Underlay = null;
            OnTileChanged?.Invoke(Tile);
        }

        public bool IsEmpty()
        {
            return Tile == null && IsPlayable;
        }

        public bool HasTile()
        {
            return Tile != null;
        }

        public bool HasOverlay()
        {
            return Overlay != null;
        }

        public bool HasTargetTile()
        {
            return Tile != null && Tile.TileKind == TileKind.Target;
        }

        public bool CanAcceptOverlay()
        {
            return Overlay == null;
        }

        public bool HasBlockingOverlay()
        {
            return Overlay != null && Overlay.BlocksTileBelow();
        }

        public bool HasOverlayLogic(TileLogicType logicType)
        {
            return Overlay != null && Overlay.LogicType == logicType;
        }

        public bool CanTileMatch()
        {
            return Tile != null &&
                   !HasBlockingOverlay() &&
                   (Underlay == null || !Underlay.BlocksMatch(null, this, Tile)) &&
                   (Overlay == null || !Overlay.BlocksMatch(null, this, Tile)) &&
                   Tile.CanMatch();
        }

        public bool CanTileActivate()
        {
            return Tile != null && !HasBlockingOverlay();
        }

        public bool CanTileEnter(TileModel tile)
        {
            bool overlayAllows = Overlay == null || Overlay.CanTileEnter(null, this, tile);
            bool underlayAllows = Underlay == null || Underlay.CanTileEnter(null, this, tile);
            return overlayAllows && underlayAllows;
        }

        public bool CanTileExit(TileModel tile)
        {
            bool overlayAllows = Overlay == null || Overlay.CanTileExit(null, this, tile);
            bool underlayAllows = Underlay == null || Underlay.CanTileExit(null, this, tile);
            return overlayAllows && underlayAllows;
        }

        public bool LocksLine(MoveAxis axis)
        {
            bool overlayLocks = Overlay != null && Overlay.LocksLine(null, this, axis);
            bool underlayLocks = Underlay != null && Underlay.LocksLine(null, this, axis);
            return overlayLocks || underlayLocks;
        }

        public TileModel GetTile(TileStackLayer layer)
        {
            switch (layer)
            {
                case TileStackLayer.Underlay:
                    return Underlay;
                case TileStackLayer.Overlay:
                    return Overlay;
                default:
                    return Tile;
            }
        }

        public void SetTile(TileStackLayer layer, TileModel tile)
        {
            if (layer == TileStackLayer.Overlay)
            {
                SetOverlay(tile);
                return;
            }

            if (layer == TileStackLayer.Underlay)
            {
                SetUnderlay(tile);
                return;
            }

            SetTile(tile);
        }

        public void ClearTile(TileStackLayer layer)
        {
            if (layer == TileStackLayer.Overlay)
            {
                ClearOverlay();
                return;
            }

            if (layer == TileStackLayer.Underlay)
            {
                ClearUnderlay();
                return;
            }

            ClearTile();
        }

        public TileStackLayer? GetTileLayer(TileModel tile)
        {
            if (tile == null)
            {
                return null;
            }

            if (ReferenceEquals(Overlay, tile))
            {
                return TileStackLayer.Overlay;
            }

            if (ReferenceEquals(Tile, tile))
            {
                return TileStackLayer.Tile;
            }

            if (ReferenceEquals(Underlay, tile))
            {
                return TileStackLayer.Underlay;
            }

            return null;
        }
    }
}
