using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Boosters
{
    [CreateAssetMenu(fileName = "BoosterDefinition", menuName = "_PawSlidePopGame/Match3/Boosters/Booster Definition")]
    public sealed class BoosterDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string boosterId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Behavior")]
        [SerializeField] private BoosterUsagePhase usagePhase = BoosterUsagePhase.Gameplay;
        [SerializeField] private BoosterType boosterType = BoosterType.Hammer;
        [SerializeField] private BoosterTargetingMode targetingMode = BoosterTargetingMode.TapCell;
        [SerializeField] private bool isUnlocked = true;
        [SerializeField] private int unlockLevel = 1;
        [SerializeField] private bool consumesMove;
        [SerializeField] private bool isUnlimitedForDev;
        [SerializeField] private int createdTileId;
        [SerializeField] private int initialCount = 3;
        [SerializeField] private int coinPrice;

        [Header("Pre-Level Effect")]
        [SerializeField] private PreLevelBoosterEffectType preLevelEffectType = PreLevelBoosterEffectType.None;
        [SerializeField] private int extraMovesAmount;
        [SerializeField] private int startingTileId;

        public string BoosterId => boosterId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public BoosterUsagePhase UsagePhase => usagePhase;
        public BoosterType BoosterType => boosterType;
        public BoosterTargetingMode TargetingMode => targetingMode;
        public bool IsUnlocked => isUnlocked;
        public int UnlockLevel => unlockLevel;
        public bool ConsumesMove => consumesMove;
        public bool IsUnlimitedForDev => isUnlimitedForDev;
        public int CreatedTileId => createdTileId;
        public int InitialCount => initialCount;
        public int CoinPrice => coinPrice;
        public PreLevelBoosterEffectType PreLevelEffectType => preLevelEffectType;
        public int ExtraMovesAmount => extraMovesAmount;
        public int StartingTileId => startingTileId;

        public bool IsUnlockedAtLevel(int playerLevel)
        {
            return isUnlocked && playerLevel >= unlockLevel;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(boosterId))
            {
                boosterId = name;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = boosterId;
            }

            if (usagePhase == BoosterUsagePhase.PreLevel)
            {
                boosterType = BoosterType.None;
                targetingMode = BoosterTargetingMode.None;
                consumesMove = false;
                coinPrice = 0;
            }
            else if (boosterType == BoosterType.Shuffle)
            {
                targetingMode = BoosterTargetingMode.Immediate;
            }
            else if (boosterType == BoosterType.LineClear)
            {
                targetingMode = BoosterTargetingMode.SwipeLine;
            }
            else if (targetingMode == BoosterTargetingMode.None || targetingMode == BoosterTargetingMode.Immediate)
            {
                targetingMode = BoosterTargetingMode.TapCell;
            }

            if (boosterType == BoosterType.RainbowPlacement && createdTileId <= 0)
            {
                createdTileId = 155;
            }

            initialCount = Mathf.Max(0, initialCount);
            coinPrice = Mathf.Max(0, coinPrice);
            unlockLevel = Mathf.Max(1, unlockLevel);
            extraMovesAmount = Mathf.Max(0, extraMovesAmount);
            startingTileId = Mathf.Max(0, startingTileId);
        }
    }
}
