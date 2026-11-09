using System;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager
{
    public class HeartManager : Singleton<HeartManager>
    {
        public const int MaxHearts = 5;
        public const int RegenTimeSeconds = 1800; // 30 minutes

        private PlayerEconomyRepository Repository => PlayerEconomyRepository.Instance;

        public event Action<int, int> OnHeartsChanged;

        private int _lastHearts;

        protected override void Awake()
        {
            base.Awake();
            PlayerEconomyRepository.OnDataChanged += HandleRepositoryDataChanged;
        }

        private void Start()
        {
            _lastHearts = Hearts;
        }

        protected override void OnDestroy()
        {
            PlayerEconomyRepository.OnDataChanged -= HandleRepositoryDataChanged;
            base.OnDestroy();
        }

        private void HandleRepositoryDataChanged()
        {
            int previous = _lastHearts;
            UpdateHeartRegeneration();
            int current = Hearts;
            if (previous != current)
            {
                _lastHearts = current;
                OnHeartsChanged?.Invoke(previous, current);
            }
        }

        public bool IsInfiniteHeartsActive => !string.IsNullOrEmpty(Repository.Data.infiniteHeartsEndUtc)
            && DateTime.TryParse(Repository.Data.infiniteHeartsEndUtc, out DateTime endUtc)
            && DateTime.UtcNow < endUtc.ToUniversalTime();

        public double RemainingInfiniteHeartsSeconds
        {
            get
            {
                if (!string.IsNullOrEmpty(Repository.Data.infiniteHeartsEndUtc)
                    && DateTime.TryParse(Repository.Data.infiniteHeartsEndUtc, out DateTime endUtc))
                {
                    double remaining = (endUtc.ToUniversalTime() - DateTime.UtcNow).TotalSeconds;
                    return Mathf.Max(0f, (float)remaining);
                }
                return 0;
            }
        }

        public int Hearts
        {
            get
            {
                if (IsInfiniteHeartsActive)
                {
                    return MaxHearts;
                }
                UpdateHeartRegeneration();
                return Repository.Data.hearts;
            }
        }

        public double SecondsUntilNextHeart
        {
            get
            {
                if (IsInfiniteHeartsActive)
                {
                    return 0;
                }
                int currentHearts = Hearts; // This triggers regeneration calculation first
                if (currentHearts >= MaxHearts)
                {
                    return 0;
                }

                DateTime lastRegen = GetLastHeartRegenTime();
                double elapsedSeconds = (DateTime.UtcNow - lastRegen).TotalSeconds;
                if (elapsedSeconds < 0)
                {
                    // Protection against clock going backwards
                    return RegenTimeSeconds;
                }

                double secondsSinceLastRegen = elapsedSeconds % RegenTimeSeconds;
                return RegenTimeSeconds - secondsSinceLastRegen;
            }
        }

        public bool IsMatchActive => Repository.Data.isMatchActive;

        public void InitializeHeartSystem()
        {
            UpdateHeartRegeneration();

            if (Repository.Data.isMatchActive)
            {
                Debug.LogWarning("[HeartManager] Active match detected at initialization. Player must have quit or crashed. Deducting 1 heart.");
                Repository.Data.isMatchActive = false;
                
                int previous = Repository.Data.hearts;
                if (previous > 0)
                {
                    if (previous == MaxHearts)
                    {
                        Repository.Data.lastHeartRegenTime = DateTime.UtcNow.ToString("o");
                    }
                    Repository.Data.hearts--;
                    _lastHearts = Repository.Data.hearts;
                    OnHeartsChanged?.Invoke(previous, Repository.Data.hearts);
                }
                Repository.Save();
            }
            else
            {
                _lastHearts = Hearts;
            }
        }

        public bool CanSpendHeart()
        {
            return Hearts > 0;
        }

        public bool TrySpendHeart()
        {
            if (IsInfiniteHeartsActive)
            {
                return true;
            }

            int currentHearts = Hearts;
            if (currentHearts <= 0)
            {
                return false;
            }

            int previous = currentHearts;
            if (previous == MaxHearts)
            {
                Repository.Data.lastHeartRegenTime = DateTime.UtcNow.ToString("o");
            }

            Repository.Data.hearts = Mathf.Max(0, previous - 1);
            _lastHearts = Repository.Data.hearts;
            Repository.Save();

            OnHeartsChanged?.Invoke(previous, Repository.Data.hearts);
            return true;
        }

        public void AddInfiniteHearts(int durationSeconds)
        {
            if (durationSeconds <= 0)
            {
                return;
            }

            DateTime baseTime = DateTime.UtcNow;
            if (IsInfiniteHeartsActive && DateTime.TryParse(Repository.Data.infiniteHeartsEndUtc, out DateTime existingEnd))
            {
                baseTime = existingEnd.ToUniversalTime();
            }

            DateTime newEnd = baseTime.AddSeconds(durationSeconds);
            Repository.Data.infiniteHeartsEndUtc = newEnd.ToString("o");
            _lastHearts = Hearts;
            Repository.Save();

            OnHeartsChanged?.Invoke(Hearts, Hearts);
        }

        public void ClearInfiniteHearts()
        {
            Repository.Data.infiniteHeartsEndUtc = string.Empty;
            _lastHearts = Hearts;
            Repository.Save();
            OnHeartsChanged?.Invoke(Hearts, Hearts);
        }

        public void AddHearts(int amount, bool allowOverfill = false)
        {
            if (amount <= 0)
            {
                return;
            }

            int previous = Hearts;
            int target = previous + amount;
            if (!allowOverfill)
            {
                target = Mathf.Min(MaxHearts, target);
            }

            Repository.Data.hearts = target;
            if (target >= MaxHearts)
            {
                Repository.Data.lastHeartRegenTime = string.Empty;
            }
            _lastHearts = Repository.Data.hearts;

            Repository.Save();
            OnHeartsChanged?.Invoke(previous, Repository.Data.hearts);
        }

        public void SetMatchStarted()
        {
            Repository.Data.isMatchActive = true;
            Repository.Save();
        }

        public void SetMatchFinished(bool won)
        {
            if (!Repository.Data.isMatchActive)
            {
                return;
            }

            Repository.Data.isMatchActive = false;

            if (!won)
            {
                TrySpendHeart();
            }
            else
            {
                Repository.Save();
            }
        }

        private void UpdateHeartRegeneration()
        {
            if (Repository.Data.hearts >= MaxHearts)
            {
                if (!string.IsNullOrEmpty(Repository.Data.lastHeartRegenTime))
                {
                    Repository.Data.lastHeartRegenTime = string.Empty;
                    Repository.Save();
                }
                return;
            }

            DateTime now = DateTime.UtcNow;
            DateTime lastRegen = GetLastHeartRegenTime();
            double elapsedSeconds = (now - lastRegen).TotalSeconds;

            if (elapsedSeconds >= RegenTimeSeconds)
            {
                int heartsToAdd = (int)(elapsedSeconds / RegenTimeSeconds);
                int previous = Repository.Data.hearts;
                int newHeartsCount = Mathf.Min(MaxHearts, previous + heartsToAdd);

                Repository.Data.hearts = newHeartsCount;

                if (newHeartsCount >= MaxHearts)
                {
                    Repository.Data.lastHeartRegenTime = string.Empty;
                }
                else
                {
                    DateTime nextRegenBase = lastRegen.AddSeconds(heartsToAdd * RegenTimeSeconds);
                    Repository.Data.lastHeartRegenTime = nextRegenBase.ToString("o");
                }
                _lastHearts = Repository.Data.hearts;

                Repository.Save();
                OnHeartsChanged?.Invoke(previous, newHeartsCount);
            }
        }

        private DateTime GetLastHeartRegenTime()
        {
            string rawTime = Repository.Data.lastHeartRegenTime;
            if (!string.IsNullOrEmpty(rawTime) && DateTime.TryParse(rawTime, out DateTime parsed))
            {
                return parsed.ToUniversalTime();
            }

            // Fallback: If hearts < MaxHearts but last regen time is missing or invalid, start regen timer now.
            DateTime fallback = DateTime.UtcNow;
            Repository.Data.lastHeartRegenTime = fallback.ToString("o");
            Repository.Save();
            return fallback;
        }
    }
}
