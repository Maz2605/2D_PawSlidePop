using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Flow
{
    public sealed class ChargedAbilityTracker
    {
        private readonly Match3LevelData.ChargedAbilityConfig _config;
        private readonly Match3TileDatabaseSO _tileDatabase;

        public int CurrentEnergy { get; private set; }
        public int AvailableCharges { get; private set; }

        public bool IsEnabled => _config != null && _config.IsEnabled;
        public int SpecialTileId => _config != null ? _config.specialTileId : 0;
        public int EnergyToCharge => _config != null ? _config.energyToCharge : 0;
        public int MaxStoredCharges => _config != null ? _config.maxStoredCharges : 0;
        public bool CanPlace => IsEnabled && AvailableCharges > 0;

        public ChargedAbilityTracker(Match3LevelData levelData, Match3TileDatabaseSO tileDatabase)
        {
            _config = levelData?.GetChargedAbilityConfig();
            _tileDatabase = tileDatabase;
        }

        public GameplayHudSnapshot.ChargedAbilityHudData BuildHudData(bool isPlacementMode, bool isComboSelectionMode)
        {
            TileDefinitionSO definition = _tileDatabase != null ? _tileDatabase.GetTileDefinition(SpecialTileId) : null;
            return new GameplayHudSnapshot.ChargedAbilityHudData
            {
                isEnabled = IsEnabled,
                tileId = SpecialTileId,
                icon = definition != null ? definition.Icon : null,
                currentEnergy = AvailableCharges >= MaxStoredCharges ? EnergyToCharge : CurrentEnergy,
                requiredEnergy = EnergyToCharge,
                availableCharges = AvailableCharges,
                maxStoredCharges = MaxStoredCharges,
                isPlacementMode = isPlacementMode,
                isComboSelectionMode = isComboSelectionMode
            };
        }

        public bool ApplyAcceptedTurn(BoardMoveExecutionResult executionResult)
        {
            if (!IsEnabled || executionResult == null || !executionResult.IsAccepted || executionResult.PresentationTrace == null)
            {
                return false;
            }

            int energyBefore = CurrentEnergy;
            int chargesBefore = AvailableCharges;
            int gain = CalculateEnergyGain(executionResult.PresentationTrace);
            if (gain > 0)
            {
                AddEnergy(gain);
            }

            return energyBefore != CurrentEnergy || chargesBefore != AvailableCharges;
        }

        public bool TryConsumeCharge()
        {
            if (!CanPlace)
            {
                return false;
            }

            AvailableCharges--;
            if (AvailableCharges <= 0)
            {
                AvailableCharges = 0;
                CurrentEnergy = 0;
            }

            return true;
        }

        private int CalculateEnergyGain(BoardPresentationTrace trace)
        {
            if (trace?.Cascades == null || _tileDatabase == null || _config == null)
            {
                return 0;
            }

            int energyGain = 0;
            for (int cascadeIndex = 0; cascadeIndex < trace.Cascades.Count; cascadeIndex++)
            {
                CascadeTrace cascade = trace.Cascades[cascadeIndex];
                if (cascade?.ClearPhase == null)
                {
                    continue;
                }

                for (int activateIndex = 0; activateIndex < cascade.ClearPhase.ActivateOps.Count; activateIndex++)
                {
                    TileActivateOp activateOp = cascade.ClearPhase.ActivateOps[activateIndex];
                    if (activateOp.LogicType == TileLogicType.ChargedSweepBooster)
                    {
                        continue;
                    }

                    TileDefinitionSO activateDefinition = _tileDatabase.GetTileDefinition(activateOp.TileId);
                    if (activateDefinition != null && activateDefinition.TileKind == TileKind.Booster)
                    {
                        energyGain += _config.energyFromBoosterActivate;
                    }
                }

                for (int clearIndex = 0; clearIndex < cascade.ClearPhase.ClearOps.Count; clearIndex++)
                {
                    TileClearOp clearOp = cascade.ClearPhase.ClearOps[clearIndex];
                    TileDefinitionSO clearDefinition = _tileDatabase.GetTileDefinition(clearOp.TileId);
                    if (clearDefinition == null)
                    {
                        continue;
                    }

                    switch (clearDefinition.TileKind)
                    {
                        case TileKind.Blocker:
                            energyGain += _config.energyFromBlockerClear;
                            break;
                        case TileKind.Target:
                            energyGain += _config.energyFromMechanicClear;
                            break;
                    }
                }
            }

            return energyGain;
        }

        private void AddEnergy(int amount)
        {
            if (amount <= 0 || !IsEnabled || AvailableCharges >= MaxStoredCharges)
            {
                if (AvailableCharges >= MaxStoredCharges)
                {
                    CurrentEnergy = EnergyToCharge;
                }

                return;
            }

            CurrentEnergy += amount;
            while (CurrentEnergy >= EnergyToCharge && AvailableCharges < MaxStoredCharges)
            {
                CurrentEnergy -= EnergyToCharge;
                AvailableCharges++;
            }

            if (AvailableCharges >= MaxStoredCharges)
            {
                AvailableCharges = MaxStoredCharges;
                CurrentEnergy = EnergyToCharge;
            }
        }
    }
}

