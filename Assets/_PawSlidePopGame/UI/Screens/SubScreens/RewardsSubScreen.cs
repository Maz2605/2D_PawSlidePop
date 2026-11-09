using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    /// <summary>
    /// Màn hình con Star Rewards – hiển thị danh sách phần thưởng theo mốc sao và tiến độ section đã vượt qua.
    /// </summary>
    public class RewardsSubScreen : BaseSubScreen
    {
        [Header("Cấu Hình Section & Tiến Độ")]
        [SerializeField] private MapWorldConfigSO mapConfig;
        [SerializeField] private TMP_Text mapCountText;     // Phần hiển thị núi (completed/total sections)
        [SerializeField] private TMP_Text starsCountText;   // Phần hiển thị sao (current/max stars)

        [Header("Cấu Hình UI Danh Sách")]
        [SerializeField] private RewardItemView rewardItemPrefab;
        [SerializeField] private Transform contentRoot;

        private readonly List<StarRewardSO> _rewards = new List<StarRewardSO>();
        private readonly List<RewardItemView> _activeViews = new List<RewardItemView>();

        // ───── Lifecycle ─────

        public override void Init()
        {
            if (isInitialized)
                return;

            base.Init();
        }

        public override void Show()
        {
            base.Show();
            RefreshUI();
        }

        // ───── Setup & Refresh ─────

        private void RefreshUI()
        {
            // Dọn dẹp danh sách cũ
            foreach (var view in _activeViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _activeViews.Clear();
            _rewards.Clear();

            // Tải danh sách StarRewardSO từ Resources/Reward/StarRewards
            StarRewardSO[] loadedRewards = Resources.LoadAll<StarRewardSO>("Reward/StarRewards");
            if (loadedRewards != null)
            {
                _rewards.AddRange(loadedRewards);
                // Sắp xếp các phần thưởng theo số sao yêu cầu tăng dần
                _rewards.Sort((a, b) => a.RequiredStars.CompareTo(b.RequiredStars));
            }

            if (_rewards.Count == 0)
            {
                Debug.LogWarning("[RewardsSubScreen] Không tìm thấy file StarRewardSO nào trong thư mục Resources/Reward/StarRewards.", this);
            }

            // Tính toán tiến độ
            int playerTotalStars = GetPlayerTotalStars();
            int totalStarsPossible = GetTotalStarsPossible();
            GetSectionProgress(out int completedSections, out int totalSections);

            // Cập nhật các text Header
            if (starsCountText != null)
            {
                starsCountText.SetText("{0}/{1}", playerTotalStars, totalStarsPossible);
            }

            if (mapCountText != null)
            {
                mapCountText.SetText("{0}/{1}", completedSections, totalSections);
            }

            // Sinh động danh sách dòng phần thưởng
            var economy = PlayerEconomyRepository.Instance;
            HashSet<string> claimedIds = economy != null && economy.Data != null && economy.Data.claimedStarRewardIds != null
                ? new HashSet<string>(economy.Data.claimedStarRewardIds)
                : new HashSet<string>();

            foreach (StarRewardSO reward in _rewards)
            {
                if (reward == null)
                    continue;

                if (rewardItemPrefab == null || contentRoot == null)
                {
                    Debug.LogWarning("[RewardsSubScreen] Thiếu prefab hoặc content root để sinh phần thưởng.", this);
                    continue;
                }

                RewardItemView view = Instantiate(rewardItemPrefab, contentRoot);
                bool isClaimed = claimedIds.Contains(reward.RewardId);

                view.Bind(reward, HandleClaimClicked, playerTotalStars, isClaimed);
                _activeViews.Add(view);
            }
        }

        // ───── Claim Handler ─────

        private void HandleClaimClicked(StarRewardSO reward)
        {
            if (reward == null)
                return;

            var economy = PlayerEconomyRepository.Instance;
            if (economy == null || economy.Data == null)
                return;

            if (economy.Data.claimedStarRewardIds.Contains(reward.RewardId))
            {
                UIManager.Instance?.ShowToast("Reward already claimed!");
                return;
            }

            int playerTotalStars = GetPlayerTotalStars();
            if (playerTotalStars < reward.RequiredStars)
            {
                UIManager.Instance?.ShowToast("Not enough Stars to claim this reward!");
                return;
            }

            // Thực hiện trao quà
            if (RewardGrantService.TryGrantMultiple(reward.Rewards, "star_reward"))
            {
                economy.Data.claimedStarRewardIds.Add(reward.RewardId);
                economy.Save();

                UIManager.Instance?.ShowToast($"Successfully claimed: {reward.DisplayName}!");
                RefreshUI();
            }
            else
            {
                UIManager.Instance?.ShowToast("Claim failed. Please try again.");
            }
        }

        // ───── Progress Helpers ─────

        private int GetPlayerTotalStars()
        {
            int total = 0;
            var progressRepo = LevelProgressRepository.Instance;
            if (progressRepo != null && progressRepo.Data != null && progressRepo.Data.levels != null)
            {
                foreach (var lvl in progressRepo.Data.levels)
                {
                    if (lvl != null)
                    {
                        total += lvl.bestStars;
                    }
                }
            }
            return total;
        }

        private int GetTotalStarsPossible()
        {
            var provider = new _PawSlidePopGame._Scripts.Data.LevelProvider.ResourcesLevelCatalogProvider();
            IReadOnlyList<string> levelIds = provider.GetLevelIds();
            return levelIds != null ? levelIds.Count * 3 : 0;
        }

        private void GetSectionProgress(out int completedSections, out int totalSections)
        {
            completedSections = 0;
            totalSections = 0;

            if (mapConfig == null)
                return;

            var provider = new _PawSlidePopGame._Scripts.Data.LevelProvider.ResourcesLevelCatalogProvider();
            IReadOnlyList<string> levelIds = provider.GetLevelIds();
            if (levelIds == null || levelIds.Count == 0)
                return;

            var progressRepo = LevelProgressRepository.Instance;
            if (progressRepo == null)
                return;

            string fallbackCurrentLevelId = "Level_001";
            progressRepo.EnsureInitializedProgress(fallbackCurrentLevelId);
            string currentLevelId = progressRepo.GetCurrentLevelId(fallbackCurrentLevelId);

            int resolvedCurrentLevelNumber = 1;
            MapManager.TryParseLevelNumber(currentLevelId, out int parsedCurrentLevelNumber);
            resolvedCurrentLevelNumber = Mathf.Max(1, parsedCurrentLevelNumber);

            int resolvedHighestUnlockedLevelNumber = Mathf.Max(resolvedCurrentLevelNumber, progressRepo.GetHighestUnlockedLevelNumber());

            var mapManager = new MapManager();
            var layout = mapManager.BuildLayout(
                levelIds,
                mapConfig,
                resolvedHighestUnlockedLevelNumber,
                resolvedCurrentLevelNumber,
                progressRepo,
                skipSectionZero: true);

            if (layout == null || layout.Sections == null)
                return;

            foreach (var section in layout.Sections)
            {
                if (section.EntryCount == 0)
                    continue; // Bỏ qua section rỗng / overflow

                totalSections++;

                int lastEntryIndex = section.StartEntryIndex + section.EntryCount - 1;
                if (lastEntryIndex < layout.Entries.Count)
                {
                    var lastEntry = layout.Entries[lastEntryIndex];
                    if (resolvedHighestUnlockedLevelNumber > lastEntry.DisplayLevelNumber)
                    {
                        completedSections++;
                    }
                }
            }
        }
    }
}