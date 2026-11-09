using System;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager
{
    public class EconomyManager : Singleton<EconomyManager>
    {
        private PlayerEconomyRepository Repository => PlayerEconomyRepository.Instance;

        public int Coins => Repository.Data.coins;

        public event Action<int, int, string> OnCoinsChanged;

        private int _lastCoins;

        protected override void Awake()
        {
            base.Awake();
            PlayerEconomyRepository.OnDataChanged += HandleRepositoryDataChanged;
        }

        private void Start()
        {
            _lastCoins = Coins;
        }

        protected override void OnDestroy()
        {
            PlayerEconomyRepository.OnDataChanged -= HandleRepositoryDataChanged;
            base.OnDestroy();
        }

        private void HandleRepositoryDataChanged()
        {
            int previous = _lastCoins;
            int current = Coins;
            if (previous != current)
            {
                _lastCoins = current;
                OnCoinsChanged?.Invoke(previous, current, "repository_sync");
            }
        }

        public bool CanSpendCoins(int amount)
        {
            return amount >= 0 && Coins >= amount;
        }

        public void AddCoins(int amount, string reason = null)
        {
            if (amount <= 0)
            {
                return;
            }

            int previous = Coins;
            Repository.Data.coins = Mathf.Max(0, previous + amount);
            _lastCoins = Repository.Data.coins;
            Repository.Save();
            OnCoinsChanged?.Invoke(previous, Repository.Data.coins, reason);
        }

        public bool TrySpendCoins(int amount, string reason = null)
        {
            if (amount < 0 || !CanSpendCoins(amount))
            {
                return false;
            }

            int previous = Coins;
            Repository.Data.coins = Mathf.Max(0, previous - amount);
            _lastCoins = Repository.Data.coins;
            Repository.Save();
            OnCoinsChanged?.Invoke(previous, Repository.Data.coins, reason);
            return true;
        }

        public void Reload()
        {
            int previous = Coins;
            Repository.Reload();
            _lastCoins = Coins;
            if (previous != Coins)
            {
                OnCoinsChanged?.Invoke(previous, Coins, "reload");
            }
        }
    }
}
