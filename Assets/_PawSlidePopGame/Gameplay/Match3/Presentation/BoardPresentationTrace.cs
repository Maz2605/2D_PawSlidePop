using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Presentation
{
    [Serializable]
    public readonly struct BoardCellPosition : IEquatable<BoardCellPosition>
    {
        public int X { get; }
        public int Y { get; }

        public BoardCellPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static BoardCellPosition FromCell(CellModel cell)
        {
            return cell == null ? default : new BoardCellPosition(cell.X, cell.Y);
        }

        public bool Equals(BoardCellPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardCellPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }
    }

    [Serializable]
    public sealed class BoardMoveExecutionResult
    {
        public BoardExecutionKind Kind { get; set; } = BoardExecutionKind.None;
        public bool IsApplied { get; set; }
        public bool IsAccepted { get; set; }
        public BoardResolutionResult ResolutionResult { get; set; } = new BoardResolutionResult();
        public BoardPresentationTrace PresentationTrace { get; set; } = new BoardPresentationTrace();
        public Boosters.BoosterType BoosterType { get; set; } = Boosters.BoosterType.None;
        public Boosters.BoosterDefinitionSO BoosterDefinition { get; set; }
    }

    public enum BoardExecutionKind
    {
        None = 0,
        Move = 1,
        TileActivation = 2,
        ChargedPlacement = 3,
        ChargedCombo = 4,
        Booster = 5
    }

    [Serializable]
    public sealed class BoardPresentationTrace
    {
        public MoveAttemptTrace MoveAttempt { get; set; }
        public RollbackTrace Rollback { get; set; }
        public List<CascadeTrace> Cascades { get; } = new List<CascadeTrace>();
    }

    [Serializable]
    public sealed class MoveAttemptTrace
    {
        public MoveAxis Axis { get; set; }
        public int LineIndex { get; set; }
        public LineSlideDirection Direction { get; set; }
        public List<TileTravelOp> TravelOps { get; } = new List<TileTravelOp>();
    }

    [Serializable]
    public sealed class RollbackTrace
    {
        public MoveAxis Axis { get; set; }
        public int LineIndex { get; set; }
        public LineSlideDirection Direction { get; set; }
        public List<TileTravelOp> TravelOps { get; } = new List<TileTravelOp>();
    }

    [Serializable]
    public sealed class CascadeTrace
    {
        public ClearPhaseTrace ClearPhase { get; } = new ClearPhaseTrace();
        public GravityPhaseTrace GravityPhase { get; } = new GravityPhaseTrace();
        public RefillPhaseTrace RefillPhase { get; } = new RefillPhaseTrace();
    }

    [Serializable]
    public sealed class ClearPhaseTrace
    {
        public List<TileActivateOp> ActivateOps { get; } = new List<TileActivateOp>();
        public List<TargetSelectionOp> TargetSelectionOps { get; } = new List<TargetSelectionOp>();
        public List<TileDamageOp> DamageOps { get; } = new List<TileDamageOp>();
        public List<TileClearOp> ClearOps { get; } = new List<TileClearOp>();
        public List<SpecialCreateOp> SpecialCreateOps { get; } = new List<SpecialCreateOp>();
        public List<ScoreGainOp> ScoreGainOps { get; } = new List<ScoreGainOp>();
    }

    [Serializable]
    public sealed class GravityPhaseTrace
    {
        public List<TileTravelOp> TravelOps { get; } = new List<TileTravelOp>();
    }

    [Serializable]
    public sealed class RefillPhaseTrace
    {
        public List<TileSpawnOp> SpawnOps { get; } = new List<TileSpawnOp>();
    }

    [Serializable]
    public sealed class TileClearOp
    {
        public int TileInstanceId { get; set; }
        public int TileId { get; set; }
        public TileStackLayer Layer { get; set; }
        public BoardCellPosition Cell { get; set; }
        public bool WasMatched { get; set; }
    }

    [Serializable]
    public sealed class TileDamageOp
    {
        public int TileInstanceId { get; set; }
        public int TileId { get; set; }
        public TileStackLayer Layer { get; set; }
        public BoardCellPosition Cell { get; set; }
        public int PreviousHP { get; set; }
        public int CurrentHP { get; set; }
        public bool Destroyed { get; set; }
    }

    [Serializable]
    public sealed class TileActivateOp
    {
        public int TileInstanceId { get; set; }
        public int TileId { get; set; }
        public TileStackLayer Layer { get; set; }
        public BoardCellPosition Cell { get; set; }
        public TileLogicType LogicType { get; set; }
    }

    [Serializable]
    public sealed class TargetSelectionOp
    {
        public int SourceTileInstanceId { get; set; }
        public BoardCellPosition TargetCell { get; set; }
        public int TargetTileInstanceId { get; set; }
        public TileStackLayer TargetLayer { get; set; }
    }

    [Serializable]
    public sealed class TileTravelOp
    {
        public int TileInstanceId { get; set; }
        public int TileId { get; set; }
        public TileStackLayer Layer { get; set; }
        public BoardCellPosition FromCell { get; set; }
        public BoardCellPosition ToCell { get; set; }
        public int Distance { get; set; }
        public bool IsWrapAround { get; set; }
    }

    [Serializable]
    public sealed class TileSpawnOp
    {
        public int TileInstanceId { get; set; }
        public int TileId { get; set; }
        public TileStackLayer Layer { get; set; }
        public BoardContentDefinitionSO Definition { get; set; }
        public int SpawnFromRowAboveBoard { get; set; }
        public BoardCellPosition ToCell { get; set; }
    }

    [Serializable]
    public sealed class SpecialCreateOp
    {
        public int SourceTileInstanceId { get; set; }
        public int FromTileId { get; set; }
        public int NewTileInstanceId { get; set; }
        public int ToTileId { get; set; }
        public TileStackLayer Layer { get; set; }
        public BoardContentDefinitionSO Definition { get; set; }
        public TileLogicType LogicType { get; set; }
        public BoardCellPosition Cell { get; set; }
        public bool ReplaceSourceTileView { get; set; } = true;
    }

    [Serializable]
    public sealed class ScoreGainOp
    {
        public int TileInstanceId { get; set; }
        public int TileId { get; set; }
        public TileStackLayer Layer { get; set; }
        public BoardCellPosition Cell { get; set; }
        public int Amount { get; set; }
        public int RunningScore { get; set; }
    }

    [Serializable]
    public readonly struct BoardLinePreview
    {
        public CellModel FocusedCell { get; }
        public MoveAxis? Axis { get; }
        public int LineIndex { get; }
        public bool HasLockedLine => Axis.HasValue;

        public BoardLinePreview(CellModel focusedCell, MoveAxis? axis, int lineIndex)
        {
            FocusedCell = focusedCell;
            Axis = axis;
            LineIndex = lineIndex;
        }
    }

    public sealed class BoardFxContext
    {
        private readonly CascadeTrace _cascadeTrace;
        private readonly Random _random;

        public BoardFxContext(CascadeTrace cascadeTrace)
            : this(cascadeTrace, null)
        {
        }

        public BoardFxContext(CascadeTrace cascadeTrace, Random random)
        {
            _cascadeTrace = cascadeTrace ?? throw new ArgumentNullException(nameof(cascadeTrace));
            _random = random;
        }

        public Random Random => _random;

        public void RecordActivate(TileModel tile, CellModel cell)
        {
            if (tile == null || cell == null)
            {
                return;
            }

            _cascadeTrace.ClearPhase.ActivateOps.Add(new TileActivateOp
            {
                TileInstanceId = tile.InstanceId,
                TileId = tile.TileId,
                Layer = ResolveLayer(cell, tile),
                Cell = BoardCellPosition.FromCell(cell),
                LogicType = tile.LogicType
            });
        }

        public void RecordDamage(TileModel tile, CellModel cell, int previousHp, int currentHp, bool destroyed)
        {
            if (tile == null || cell == null)
            {
                return;
            }

            _cascadeTrace.ClearPhase.DamageOps.Add(new TileDamageOp
            {
                TileInstanceId = tile.InstanceId,
                TileId = tile.TileId,
                Layer = ResolveLayer(cell, tile),
                Cell = BoardCellPosition.FromCell(cell),
                PreviousHP = previousHp,
                CurrentHP = currentHp,
                Destroyed = destroyed
            });
        }

        public void RecordClear(TileModel tile, CellModel cell, bool wasMatched)
        {
            if (tile == null || cell == null)
            {
                return;
            }

            _cascadeTrace.ClearPhase.ClearOps.Add(new TileClearOp
            {
                TileInstanceId = tile.InstanceId,
                TileId = tile.TileId,
                Layer = ResolveLayer(cell, tile),
                Cell = BoardCellPosition.FromCell(cell),
                WasMatched = wasMatched
            });
        }

        public void RecordTargetSelection(TileModel sourceTile, CellModel targetCell)
        {
            RecordTargetSelection(sourceTile, targetCell, targetCell?.TopTile);
        }

        public void RecordTargetSelection(TileModel sourceTile, CellModel targetCell, TileModel targetTile)
        {
            if (sourceTile == null || targetCell == null)
            {
                return;
            }

            TileStackLayer targetLayer = targetCell.Overlay != null && targetCell.Overlay == targetTile
                ? TileStackLayer.Overlay
                : TileStackLayer.Base;

            _cascadeTrace.ClearPhase.TargetSelectionOps.Add(new TargetSelectionOp
            {
                SourceTileInstanceId = sourceTile.InstanceId,
                TargetCell = BoardCellPosition.FromCell(targetCell),
                TargetTileInstanceId = targetTile != null ? targetTile.InstanceId : 0,
                TargetLayer = targetLayer
            });
        }

        public void RecordSpecialCreate(
            TileModel sourceTile,
            TileModel createdTile,
            CellModel cell,
            TileStackLayer layer = TileStackLayer.Base,
            bool replaceSourceTileView = true)
        {
            if (sourceTile == null || createdTile == null || cell == null)
            {
                return;
            }

            _cascadeTrace.ClearPhase.SpecialCreateOps.Add(new SpecialCreateOp
            {
                SourceTileInstanceId = sourceTile.InstanceId,
                FromTileId = sourceTile.TileId,
                NewTileInstanceId = createdTile.InstanceId,
                ToTileId = createdTile.TileId,
                Layer = layer,
                Definition = createdTile.Definition,
                LogicType = createdTile.LogicType,
                Cell = BoardCellPosition.FromCell(cell),
                ReplaceSourceTileView = replaceSourceTileView
            });
        }

        public void RecordScore(TileModel tile, CellModel cell, int amount, int runningScore)
        {
            if (tile == null || cell == null || amount == 0)
            {
                return;
            }

            _cascadeTrace.ClearPhase.ScoreGainOps.Add(new ScoreGainOp
            {
                TileInstanceId = tile.InstanceId,
                TileId = tile.TileId,
                Layer = ResolveLayer(cell, tile),
                Cell = BoardCellPosition.FromCell(cell),
                Amount = amount,
                RunningScore = runningScore
            });
        }

        private static TileStackLayer ResolveLayer(CellModel cell, TileModel tile)
        {
            TileStackLayer? layer = cell?.GetTileLayer(tile);
            if (layer.HasValue)
            {
                return layer.Value;
            }

            if (tile == null)
            {
                return TileStackLayer.Tile;
            }

            switch (tile.Layer)
            {
                case BoardLayer.Underlay:
                    return TileStackLayer.Underlay;
                case BoardLayer.Overlay:
                    return TileStackLayer.Overlay;
                default:
                    return TileStackLayer.Tile;
            }
        }
    }

    public sealed class BoardPresentationTraceBuilder
    {
        private readonly BoardPresentationTrace _trace;

        public BoardPresentationTraceBuilder()
        {
            _trace = new BoardPresentationTrace();
        }

        public BoardPresentationTraceBuilder(BoardMoveRequest request)
        {
            _trace = new BoardPresentationTrace
            {
                MoveAttempt = new MoveAttemptTrace
                {
                    Axis = request.Axis,
                    LineIndex = request.LineIndex,
                    Direction = request.Direction
                }
            };
        }

        public BoardPresentationTrace Build()
        {
            return _trace;
        }

        public void RecordMoveAttempt(IEnumerable<TileTravelOp> operations)
        {
            if (operations == null)
            {
                return;
            }

            _trace.MoveAttempt.TravelOps.AddRange(operations);
        }

        public void RecordRollback(BoardMoveRequest request, IEnumerable<TileTravelOp> operations)
        {
            RollbackTrace rollbackTrace = new RollbackTrace
            {
                Axis = request.Axis,
                LineIndex = request.LineIndex,
                Direction = request.Direction
            };

            if (operations != null)
            {
                rollbackTrace.TravelOps.AddRange(operations);
            }

            _trace.Rollback = rollbackTrace;
        }

        public CascadeTrace BeginCascade()
        {
            CascadeTrace cascadeTrace = new CascadeTrace();
            _trace.Cascades.Add(cascadeTrace);
            return cascadeTrace;
        }
    }
}

