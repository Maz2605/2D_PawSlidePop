using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [Serializable]
    public struct LevelTargetData
    {
        public int tileId;
        public int requiredCount;

        public LevelTargetData(int tileId, int requiredCount)
        {
            this.tileId = tileId;
            this.requiredCount = requiredCount;
        }
    }

    [Serializable]
    public struct LevelSpawnableTileConfig
    {
        public int tileId;
        public int weight;
        public bool enabled;

        public LevelSpawnableTileConfig(int tileId, int weight, bool enabled)
        {
            this.tileId = tileId;
            this.weight = weight;
            this.enabled = enabled;
        }
    }

    [Serializable]
    public class Match3LevelData
    {
        [Serializable]
        public class ChargedAbilityConfig
        {
            public bool enabled = true;
            public int specialTileId = 155;
            public int energyToCharge = 12;
            public int energyFromBoosterActivate = 1;
            public int energyFromBlockerClear = 2;
            public int energyFromMechanicClear = 2;
            public int maxStoredCharges = 1;

            [JsonIgnore]
            public bool IsEnabled => enabled && specialTileId > 0 && energyToCharge > 0 && maxStoredCharges > 0;
        }

        public string levelID;
        public int displayLevelNumber = 1;
        public int width = 8;
        public int height = 8;
        public int movesLimit = 30;
        public int[] underlayLayout;
        public int[] tileLayout;
        public int[] overlayLayout;
        public int[] cellArtLayout;
        public bool[] playableMask;
        public List<int> spawnableTileIds = new List<int>();
        public List<LevelSpawnableTileConfig> spawnableTileConfigs = new List<LevelSpawnableTileConfig>();
        public float targetSpawnBias = 1.25f;
        public bool enableDynamicBalancing = true;
        public List<LevelTargetData> targets = new List<LevelTargetData>();
        public int[] starScoreThresholds = { 100, 250, 500, 1500 };
        public ChargedAbilityConfig chargedAbility = new ChargedAbilityConfig();

        [JsonIgnore]
        public int CellCount => Math.Max(0, width) * Math.Max(0, height);

        [JsonIgnore]
        public int DisplayLevelNumber => displayLevelNumber > 0 ? displayLevelNumber : 1;

        public bool HasValidTileLayout()
        {
            return tileLayout != null && tileLayout.Length == CellCount;
        }

        public bool HasValidUnderlayLayout()
        {
            return underlayLayout != null && underlayLayout.Length == CellCount;
        }

        public bool HasValidOverlayLayout()
        {
            return overlayLayout != null && overlayLayout.Length == CellCount;
        }

        public bool HasValidCellArtLayout()
        {
            return cellArtLayout != null && cellArtLayout.Length == CellCount;
        }

        public bool HasPlayableMask()
        {
            return playableMask != null && playableMask.Length == CellCount;
        }

        public bool IsPlayableCell(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            if (!HasPlayableMask())
            {
                return true;
            }

            return playableMask[(y * width) + x];
        }

        public int GetLayoutValue(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return 0;
            }

            if (HasValidTileLayout())
            {
                return tileLayout[(y * width) + x];
            }

            return 0;
        }

        public int GetUnderlayValue(int x, int y)
        {
            if (!HasValidUnderlayLayout() || x < 0 || x >= width || y < 0 || y >= height)
            {
                return 0;
            }

            return underlayLayout[(y * width) + x];
        }

        public int[] GetResolvedTileLayout()
        {
            return HasValidTileLayout() ? tileLayout : null;
        }

        public int GetOverlayValue(int x, int y)
        {
            if (!HasValidOverlayLayout() || x < 0 || x >= width || y < 0 || y >= height)
            {
                return 0;
            }

            return overlayLayout[(y * width) + x];
        }

        public int[] GetSafeStarThresholds()
        {
            if (starScoreThresholds == null || starScoreThresholds.Length == 0)
            {
                return new[] { 100, 250, 500, 1500 };
            }

            int[] clone = new int[starScoreThresholds.Length];
            int lastValue = 0;
            for (int i = 0; i < starScoreThresholds.Length; i++)
            {
                lastValue = Math.Max(lastValue, Math.Max(0, starScoreThresholds[i]));
                clone[i] = lastValue;
            }

            return clone;
        }

        public List<LevelTargetData> GetValidTargets()
        {
            List<LevelTargetData> validTargets = new List<LevelTargetData>();
            if (targets == null)
            {
                return validTargets;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                LevelTargetData target = targets[i];
                if (target.tileId <= 0 || target.requiredCount <= 0)
                {
                    continue;
                }

                validTargets.Add(target);
            }

            return validTargets;
        }

        public ChargedAbilityConfig GetChargedAbilityConfig()
        {
            if (chargedAbility == null)
            {
                chargedAbility = new ChargedAbilityConfig();
            }

            if (chargedAbility.energyToCharge < 1)
            {
                chargedAbility.energyToCharge = 1;
            }

            if (chargedAbility.maxStoredCharges < 1)
            {
                chargedAbility.maxStoredCharges = 1;
            }

            return chargedAbility;
        }
    }
}
