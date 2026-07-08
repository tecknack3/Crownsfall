using Crownsfall.Combat;
using UnityEngine;

namespace Crownsfall.Progress
{
    /// <summary>
    /// PlayerPrefs-based store for persistent player profile data.
    /// Loaded at startup; saved after every completed battle run.
    /// </summary>
    public static class PlayerProfileManager
    {
        private const string KeyHasProfile = "Crownsfall_Profile_HasProfile";
        private const string KeyProfileJson = "Crownsfall_Profile_Json";

        private static PlayerProfile _cachedProfile;
        private static bool _loaded;

        /// <summary>
        /// The currently loaded profile. Ensures Load has run at least once.
        /// </summary>
        public static PlayerProfile Profile
        {
            get
            {
                EnsureLoaded();
                return _cachedProfile;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            Load();
        }

        /// <summary>
        /// Loads the profile from PlayerPrefs if not already cached.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (!_loaded)
            {
                Load();
            }
        }

        /// <summary>
        /// Reads the profile from PlayerPrefs. Creates and saves a default profile when none exists.
        /// </summary>
        public static PlayerProfile Load()
        {
            _cachedProfile = null;

            if (PlayerPrefs.GetInt(KeyHasProfile, 0) == 1)
            {
                var json = PlayerPrefs.GetString(KeyProfileJson, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    _cachedProfile = JsonUtility.FromJson<PlayerProfile>(json);
                }
            }

            if (_cachedProfile == null)
            {
                _cachedProfile = PlayerProfile.CreateDefault();
                Save(_cachedProfile);
            }
            else
            {
                _cachedProfile.RefreshXpRequirementForCurrentLevel();
            }

            _loaded = true;

            if (ShouldLogProgress())
            {
                Debug.Log(
                    $"PlayerProfileManager.Load: level={_cachedProfile.Level}, " +
                    $"xp={_cachedProfile.CurrentXp}/{_cachedProfile.XpRequiredForNextLevel}, " +
                    $"gold={_cachedProfile.TotalGold}, highestWave={_cachedProfile.HighestWave}.");
            }

            return _cachedProfile;
        }

        /// <summary>
        /// Writes the profile to PlayerPrefs and flushes to disk.
        /// </summary>
        public static void Save(PlayerProfile profile = null)
        {
            profile ??= _cachedProfile ?? PlayerProfile.CreateDefault();
            _cachedProfile = profile;

            PlayerPrefs.SetInt(KeyHasProfile, 1);
            PlayerPrefs.SetString(KeyProfileJson, JsonUtility.ToJson(profile));
            PlayerPrefs.Save();

            if (ShouldLogProgress())
            {
                Debug.Log(
                    $"PlayerProfileManager.Save: level={profile.Level}, " +
                    $"xp={profile.CurrentXp}/{profile.XpRequiredForNextLevel}, gold={profile.TotalGold}.");
            }
        }

        /// <summary>
        /// Applies a completed battle run to the profile, handles level-ups, and saves.
        /// Returns the number of levels gained during this application.
        /// </summary>
        public static int ApplyBattleResult(BattleSummaryData summary, bool playerWon, int punches, int kicks)
        {
            if (summary == null)
            {
                return 0;
            }

            EnsureLoaded();
            var profile = _cachedProfile;
            var levelsGained = 0;

            if (!string.IsNullOrEmpty(summary.PlayerName))
            {
                profile.PlayerName = summary.PlayerName;
            }

            profile.TotalBattles++;
            if (playerWon)
            {
                profile.TotalWins++;
            }

            if (summary.HighestWave > profile.HighestWave)
            {
                profile.HighestWave = summary.HighestWave;
            }

            profile.TotalDamageDealt += Mathf.Max(0, summary.DamageDealt);
            profile.TotalDamageTaken += Mathf.Max(0, summary.DamageTaken);
            profile.LifetimePunches += Mathf.Max(0, punches);
            profile.LifetimeKicks += Mathf.Max(0, kicks);
            profile.LifetimePlayTimeSeconds += Mathf.Max(0f, summary.BattleDurationSeconds);

            if (summary.GoldEarned > 0)
            {
                profile.TotalGold += summary.GoldEarned;

                if (ShouldLogProgress())
                {
                    Debug.Log(
                        $"PlayerProfileManager: Gold Added +{summary.GoldEarned} (total {profile.TotalGold}).");
                }
            }

            if (summary.XpEarned > 0)
            {
                profile.CurrentXp += summary.XpEarned;

                if (ShouldLogProgress())
                {
                    Debug.Log(
                        $"PlayerProfileManager: XP Added +{summary.XpEarned} " +
                        $"(current {profile.CurrentXp}/{profile.XpRequiredForNextLevel}).");
                }
            }

            while (profile.CurrentXp >= profile.XpRequiredForNextLevel)
            {
                profile.CurrentXp -= profile.XpRequiredForNextLevel;
                profile.Level++;
                levelsGained++;
                profile.RefreshXpRequirementForCurrentLevel();

                if (ShouldLogProgress())
                {
                    Debug.Log(
                        $"PlayerProfileManager: Level Up → Level {profile.Level} " +
                        $"(xp toward next: {profile.CurrentXp}/{profile.XpRequiredForNextLevel}).");
                }
            }

            Save(profile);
            return levelsGained;
        }

        /// <summary>
        /// Removes all profile keys from PlayerPrefs (useful for testing or reset).
        /// </summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyHasProfile);
            PlayerPrefs.DeleteKey(KeyProfileJson);
            PlayerPrefs.Save();
            _cachedProfile = PlayerProfile.CreateDefault();
            _loaded = true;
        }

        private static bool ShouldLogProgress()
        {
            return CombatDebug.TracePresentation || CombatDebug.LocalProgressDebug;
        }
    }
}
