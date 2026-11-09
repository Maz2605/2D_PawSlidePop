using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities
{
    public class TileModel
    {
        public int InstanceId { get; }
        public BoardContentDefinitionSO Definition { get; }
        public object Logic { get; }
        public int CurrentHP { get; private set; }

        public int TileId => Definition.TileId;
        public virtual BoardLayer Layer => Definition.ContentLayer;
        public TileKind TileKind => Definition is TileDefinitionSO tileDefinition ? tileDefinition.TileKind : TileKind.Normal;
        public TileLogicType LogicType
        {
            get
            {
                if (Definition is TileDefinitionSO tileDefinition)
                {
                    return tileDefinition.LogicType;
                }
                if (Definition is OverlayDefinitionSO overlayDefinition)
                {
                    switch (overlayDefinition.LogicType)
                    {
                        case OverlayLogicType.Ice:
                            return TileLogicType.IceOverlay;
                        case OverlayLogicType.Bubble:
                            return TileLogicType.BubbleOverlay;
                        case OverlayLogicType.Chocolate:
                            return TileLogicType.ChocolateOverlay;
                    }
                }
                return TileLogicType.None;
            }
        }
        public bool IsDead => CurrentHP <= 0;

        protected ITileLogic TileLogic => Logic as ITileLogic;
        protected IOverlayLogic OverlayLogic => Logic as IOverlayLogic;
        protected IUnderlayLogic UnderlayLogic => Logic as IUnderlayLogic;

        public TileModel(int instanceId, BoardContentDefinitionSO definition)
        {
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentHP = definition.DefaultHP;
            Logic = CreateLogic(definition);
        }

        protected virtual object CreateLogic(BoardContentDefinitionSO definition)
        {
            return definition is TileDefinitionSO tileDefinition
                ? TileLogicFactory.CreateLogic(tileDefinition)
                : null;
        }

        public void TakeDamage(int damage)
        {
            CurrentHP = Math.Max(0, CurrentHP - damage);
        }

        public void SetHP(int hp)
        {
            CurrentHP = hp;
        }

        public bool CanBeMoved()
        {
            if (TileLogic != null)
            {
                return TileLogic.CanBeMoved();
            }

            return OverlayLogic != null && OverlayLogic.CanBeMoved();
        }

        public bool CanFall()
        {
            if (TileLogic != null)
            {
                return TileLogic.CanFall();
            }

            return OverlayLogic != null && OverlayLogic.CanFall();
        }

        public bool CanMatch()
        {
            return TileLogic != null && TileLogic.CanMatch();
        }

        public bool BlocksTileBelow()
        {
            if (TileLogic != null)
            {
                return TileLogic.BlocksTileBelow();
            }

            return OverlayLogic != null && OverlayLogic.BlocksTileBelow();
        }

        public bool BlocksMatch(BoardModel board, CellModel cell, TileModel tile)
        {
            if (OverlayLogic != null)
            {
                return OverlayLogic.BlocksMatch(board, cell, tile);
            }

            return UnderlayLogic != null && UnderlayLogic.BlocksMatch(board, cell, tile);
        }

        public bool CanTileEnter(BoardModel board, CellModel cell, TileModel tile)
        {
            if (OverlayLogic != null)
            {
                return OverlayLogic.CanTileEnter(board, cell, tile);
            }

            return UnderlayLogic == null || UnderlayLogic.CanTileEnter(board, cell, tile);
        }

        public bool CanTileExit(BoardModel board, CellModel cell, TileModel tile)
        {
            if (OverlayLogic != null)
            {
                return OverlayLogic.CanTileExit(board, cell, tile);
            }

            return UnderlayLogic == null || UnderlayLogic.CanTileExit(board, cell, tile);
        }

        public bool LocksLine(BoardModel board, CellModel cell, MoveAxis axis)
        {
            if (OverlayLogic != null)
            {
                return OverlayLogic.LocksLine(board, cell, axis);
            }

            return UnderlayLogic != null && UnderlayLogic.LocksLine(board, cell, axis);
        }

        public bool BlocksExplosionToTile(BoardModel board, CellModel cell)
        {
            return OverlayLogic != null && OverlayLogic.BlocksExplosionToTile(board, cell, this);
        }

        public bool IsMatchableWith(TileModel other)
        {
            if (other == null || !CanMatch() || !other.CanMatch())
            {
                return false;
            }

            if (!(Definition is NormalAnimalTileDefinitionSO leftDefinition) ||
                !(other.Definition is NormalAnimalTileDefinitionSO rightDefinition))
            {
                return false;
            }

            return leftDefinition.AnimalId != AnimalTileId.None &&
                   leftDefinition.AnimalId == rightDefinition.AnimalId;
        }

        public void Match(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            if (TileLogic != null)
            {
                TileLogic.OnMatched(board, cell, this, fxContext);
                NotifyUnderlayMatched(board, cell, fxContext);
                return;
            }

            if (OverlayLogic != null)
            {
                OverlayLogic.OnMatched(board, cell, this, fxContext);
                return;
            }

            UnderlayLogic?.OnMatched(board, cell, this as UnderlayContentModel, fxContext);
        }

        public void Explode(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            if (IsDead)
            {
                return;
            }

            if (TileLogic != null)
            {
                TileLogic.OnExploded(board, cell, this, fxContext);
                NotifyUnderlayExploded(board, cell, fxContext);
                return;
            }

            if (OverlayLogic != null)
            {
                OverlayLogic.OnExploded(board, cell, this, fxContext);
                return;
            }

            UnderlayLogic?.OnExploded(board, cell, this as UnderlayContentModel, fxContext);
        }

        public void Activate(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            if (TileLogic != null)
            {
                TileLogic.OnActivated(board, cell, this, fxContext);
                return;
            }

            OverlayLogic?.OnActivated(board, cell, this, fxContext);
        }

        public void NotifyEnteredCell(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            UnderlayLogic?.OnTileEntered(board, cell, this, fxContext);
        }

        public void NotifyExitedCell(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            UnderlayLogic?.OnTileExited(board, cell, this, fxContext);
        }

        private void NotifyUnderlayMatched(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            UnderlayContentModel underlay = cell?.Underlay;
            if (underlay == null || ReferenceEquals(underlay, this))
            {
                return;
            }

            underlay.Match(board, cell, fxContext);
        }

        private void NotifyUnderlayExploded(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            UnderlayContentModel underlay = cell?.Underlay;
            if (underlay == null || ReferenceEquals(underlay, this))
            {
                return;
            }

            underlay.Explode(board, cell, fxContext);
        }
    }
}

