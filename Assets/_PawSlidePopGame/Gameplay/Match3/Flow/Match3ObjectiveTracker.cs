using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Flow
{
    public sealed class Match3ObjectiveTracker
    {
        private readonly List<TargetProgressData> _targets = new List<TargetProgressData>();
        private readonly Dictionary<int, TargetProgressData> _targetsByTileId = new Dictionary<int, TargetProgressData>();
        private bool _hasCompletedFxBeenConsumed;

        public Match3ObjectiveTracker(Match3LevelData levelData, Match3TileDatabaseSO tileDatabase)
        {
            if (levelData == null)
            {
                return;
            }

            List<LevelTargetData> validTargets = levelData.GetValidTargets();
            for (int i = 0; i < validTargets.Count; i++)
            {
                LevelTargetData target = validTargets[i];
                if (_targetsByTileId.ContainsKey(target.tileId))
                {
                    continue;
                }

                BoardContentDefinitionSO definition = tileDatabase != null ? tileDatabase.GetTargetContentDefinition(target.tileId) : null;
                TargetProgressData progress = new TargetProgressData
                {
                    tileId = target.tileId,
                    icon = definition != null ? definition.Icon : null,
                    currentCount = 0,
                    requiredCount = target.requiredCount,
                    isCompleted = false
                };

                _targets.Add(progress);
                _targetsByTileId.Add(target.tileId, progress);
            }
        }

        public bool HasTargets => _targets.Count > 0;

        public bool AreAllTargetsComplete
        {
            get
            {
                if (_targets.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < _targets.Count; i++)
                {
                    if (!_targets[i].isCompleted)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public IReadOnlyList<TargetProgressData> Targets => _targets;

        public bool TryApplyClearOp(TileClearOp clearOp, out TargetProgressChangedPayload change)
        {
            change = default;
            if (clearOp == null || clearOp.TileId <= 0)
            {
                return false;
            }

            if (!_targetsByTileId.TryGetValue(clearOp.TileId, out TargetProgressData target) || target == null)
            {
                return false;
            }

            int previousCount = target.currentCount;
            if (previousCount >= target.requiredCount)
            {
                return false;
            }

            target.currentCount = previousCount + 1;
            if (target.currentCount > target.requiredCount)
            {
                target.currentCount = target.requiredCount;
            }

            bool wasCompleted = target.isCompleted;
            target.isCompleted = target.currentCount >= target.requiredCount;
            change = new TargetProgressChangedPayload(
                target.tileId,
                previousCount,
                target.currentCount,
                target.requiredCount,
                !wasCompleted && target.isCompleted);
            return true;
        }

        public bool TryConsumeTargetsCompletedFx()
        {
            if (_hasCompletedFxBeenConsumed || !AreAllTargetsComplete)
            {
                return false;
            }

            _hasCompletedFxBeenConsumed = true;
            return true;
        }

        public List<TargetProgressChangedPayload> ApplyExecutionResult(BoardMoveExecutionResult executionResult)
        {
            List<TargetProgressChangedPayload> changes = new List<TargetProgressChangedPayload>();
            if (executionResult?.PresentationTrace?.Cascades == null || _targets.Count == 0)
            {
                return changes;
            }

            for (int cascadeIndex = 0; cascadeIndex < executionResult.PresentationTrace.Cascades.Count; cascadeIndex++)
            {
                CascadeTrace cascade = executionResult.PresentationTrace.Cascades[cascadeIndex];
                if (cascade?.ClearPhase?.ClearOps == null)
                {
                    continue;
                }

                for (int clearIndex = 0; clearIndex < cascade.ClearPhase.ClearOps.Count; clearIndex++)
                {
                    TileClearOp clearOp = cascade.ClearPhase.ClearOps[clearIndex];
                    if (TryApplyClearOp(clearOp, out TargetProgressChangedPayload change))
                    {
                        changes.Add(change);
                    }
                }
            }

            return changes;
        }
    }
}

