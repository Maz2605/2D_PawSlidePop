using System;
using System.Collections.Generic;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager
{
    [Serializable]
    public sealed class PlayerEconomySaveData
    {
        public int schemaVersion = 1;
        public int coins;
        public Dictionary<string, int> boosterCounts = new Dictionary<string, int>();
        public int hearts = 5;
        public string lastHeartRegenTime = string.Empty;
        public bool isMatchActive = false;
        public string infiniteHeartsEndUtc = string.Empty;
        public string username = "Player";
        public int avatarIndex = 0;
        public string createdAt = string.Empty;
        public bool useSocialAvatar = true;
        public List<string> claimedStarRewardIds = new List<string>();

        public void Sanitize()
        {
            schemaVersion = Math.Max(1, schemaVersion);
            coins = Math.Max(0, coins);
            boosterCounts ??= new Dictionary<string, int>();
            hearts = Math.Max(0, hearts);
            lastHeartRegenTime ??= string.Empty;
            infiniteHeartsEndUtc ??= string.Empty;
            claimedStarRewardIds ??= new List<string>();

            if (string.IsNullOrEmpty(createdAt))
            {
                createdAt = $"{DateTime.UtcNow.Month}/{DateTime.UtcNow.Year}";
            }

            if (string.IsNullOrWhiteSpace(username) || username == "Player")
            {
                var rand = new System.Random();
                username = $"user{rand.Next(100, 1000):D3}";
            }
            else
            {
                username = username.Trim();
            }

            avatarIndex = Math.Max(0, avatarIndex);

            List<string> keys = new List<string>(boosterCounts.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    boosterCounts.Remove(key);
                    continue;
                }

                boosterCounts[key] = Math.Max(0, boosterCounts[key]);
            }
        }
    }
}
