using System;
using UnityEngine;

namespace Crownsfall.Progress
{
    /// <summary>
    /// Persistent player progression and lifetime stats stored in PlayerPrefs.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerName = string.Empty;
        public int Level = 1;
        public int CurrentXp;
        public int XpRequiredForNextLevel = 100;
        public int TotalGold;
        public int TotalBattles;
        public int TotalWins;
        public int HighestWave;
        public int TotalDamageDealt;
        public int TotalDamageTaken;
        public int LifetimePunches;
        public int LifetimeKicks;
        public float LifetimePlayTimeSeconds;

        /// <summary>
        /// Creates a fresh level-1 profile with default XP requirement.
        /// </summary>
        public static PlayerProfile CreateDefault()
        {
            var profile = new PlayerProfile
            {
                Level = 1,
                CurrentXp = 0
            };
            profile.RefreshXpRequirementForCurrentLevel();
            return profile;
        }

        /// <summary>
        /// XP needed to advance from the given level to the next.
        /// Level 1 requires 100 XP; each subsequent level multiplies the previous requirement by 1.25.
        /// </summary>
        public static int CalculateXpRequiredForLevel(int level)
        {
            level = Mathf.Max(1, level);
            var requirement = 100f * Mathf.Pow(1.25f, level - 1);
            return Mathf.Max(1, Mathf.RoundToInt(requirement));
        }

        /// <summary>
        /// Syncs <see cref="XpRequiredForNextLevel"/> with the current <see cref="Level"/>.
        /// </summary>
        public void RefreshXpRequirementForCurrentLevel()
        {
            XpRequiredForNextLevel = CalculateXpRequiredForLevel(Level);
        }
    }
}
