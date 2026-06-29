using Crownsfall.Combat;
using UnityEngine;

namespace Crownsfall.Progress
{
    /// <summary>
    /// Plain data container for local run records stored in PlayerPrefs.
    /// </summary>
    public class LocalRunResultData
    {
        public int BestScore;
        public int BestWave;
        public int BestGold;
        public int BestXp;
        public string LastFighterName = string.Empty;
        public float LastBattleTimeSeconds;
        public int LastDamageDealt;
        public int LastDamageTaken;
    }

    /// <summary>
    /// PlayerPrefs-based store for personal-best and last-run battle stats.
    /// Written after Wave 3 victory summary; read on the Character Builder screen.
    /// </summary>
    public static class LocalRunResultStore
    {
        private const string KeyHasRecord = "Crownsfall_Run_HasRecord";
        private const string KeyBestScore = "Crownsfall_Run_BestScore";
        private const string KeyBestWave = "Crownsfall_Run_BestWave";
        private const string KeyBestGold = "Crownsfall_Run_BestGold";
        private const string KeyBestXp = "Crownsfall_Run_BestXp";
        private const string KeyLastFighterName = "Crownsfall_Run_LastFighterName";
        private const string KeyLastBattleTimeSeconds = "Crownsfall_Run_LastBattleTime";
        private const string KeyLastDamageDealt = "Crownsfall_Run_LastDamageDealt";
        private const string KeyLastDamageTaken = "Crownsfall_Run_LastDamageTaken";

        /// <summary>
        /// Reads all saved values from PlayerPrefs. Missing keys default to zero / empty string.
        /// </summary>
        public static LocalRunResultData Load()
        {
            var data = new LocalRunResultData
            {
                BestScore = PlayerPrefs.GetInt(KeyBestScore, 0),
                BestWave = PlayerPrefs.GetInt(KeyBestWave, 0),
                BestGold = PlayerPrefs.GetInt(KeyBestGold, 0),
                BestXp = PlayerPrefs.GetInt(KeyBestXp, 0),
                LastFighterName = PlayerPrefs.GetString(KeyLastFighterName, string.Empty),
                LastBattleTimeSeconds = PlayerPrefs.GetFloat(KeyLastBattleTimeSeconds, 0f),
                LastDamageDealt = PlayerPrefs.GetInt(KeyLastDamageDealt, 0),
                LastDamageTaken = PlayerPrefs.GetInt(KeyLastDamageTaken, 0)
            };

            if (ShouldLogProgress())
            {
                Debug.Log(
                    $"LocalRunResultStore.Load: hasRecord={HasRecord()}, " +
                    $"bestScore={data.BestScore}, bestWave={data.BestWave}, " +
                    $"lastFighter={data.LastFighterName}.");
            }

            return data;
        }

        /// <summary>
        /// Writes all fields to PlayerPrefs and flushes to disk.
        /// </summary>
        public static void Save(LocalRunResultData data)
        {
            if (data == null)
            {
                return;
            }

            PlayerPrefs.SetInt(KeyHasRecord, 1);
            PlayerPrefs.SetInt(KeyBestScore, data.BestScore);
            PlayerPrefs.SetInt(KeyBestWave, data.BestWave);
            PlayerPrefs.SetInt(KeyBestGold, data.BestGold);
            PlayerPrefs.SetInt(KeyBestXp, data.BestXp);
            PlayerPrefs.SetString(KeyLastFighterName, data.LastFighterName ?? string.Empty);
            PlayerPrefs.SetFloat(KeyLastBattleTimeSeconds, data.LastBattleTimeSeconds);
            PlayerPrefs.SetInt(KeyLastDamageDealt, data.LastDamageDealt);
            PlayerPrefs.SetInt(KeyLastDamageTaken, data.LastDamageTaken);
            PlayerPrefs.Save();

            if (ShouldLogProgress())
            {
                Debug.Log(
                    $"LocalRunResultStore.Save: bestScore={data.BestScore}, bestWave={data.BestWave}, " +
                    $"bestGold={data.BestGold}, bestXp={data.BestXp}, lastFighter={data.LastFighterName}.");
            }
        }

        /// <summary>
        /// True after at least one run has been recorded.
        /// </summary>
        public static bool HasRecord()
        {
            return PlayerPrefs.GetInt(KeyHasRecord, 0) == 1;
        }

        /// <summary>
        /// Records a completed Wave 3 victory run from the battle summary snapshot.
        /// </summary>
        public static void RecordRun(BattleSummaryData summary)
        {
            if (summary == null)
            {
                return;
            }

            RecordRun(
                summary.PlayerName,
                summary.FinalScore,
                summary.HighestWave,
                summary.GoldEarned,
                summary.XpEarned,
                summary.BattleDurationSeconds,
                summary.DamageDealt,
                summary.DamageTaken);
        }

        /// <summary>
        /// Records a completed run. Best fields update only when the run beats the stored record.
        /// Last-run fields always update.
        /// </summary>
        public static void RecordRun(
            string fighterName,
            int score,
            int wave,
            int gold,
            int xp,
            float battleTimeSeconds,
            int damageDealt,
            int damageTaken)
        {
            var data = Load();
            var isBetterRun = IsRunBetter(score, wave, data.BestScore, data.BestWave);

            if (isBetterRun)
            {
                data.BestScore = score;
                data.BestWave = wave;
                data.BestGold = gold;
                data.BestXp = xp;
            }

            data.LastFighterName = string.IsNullOrEmpty(fighterName) ? "Unknown Fighter" : fighterName;
            data.LastBattleTimeSeconds = Mathf.Max(0f, battleTimeSeconds);
            data.LastDamageDealt = Mathf.Max(0, damageDealt);
            data.LastDamageTaken = Mathf.Max(0, damageTaken);

            Save(data);

            if (ShouldLogProgress())
            {
                Debug.Log(
                    $"LocalRunResultStore.RecordRun: score={score}, wave={wave}, " +
                    $"isBetterRun={isBetterRun}, fighter={data.LastFighterName}.");
            }
        }

        /// <summary>
        /// Removes all run-record keys from PlayerPrefs (useful for testing or reset).
        /// </summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KeyHasRecord);
            PlayerPrefs.DeleteKey(KeyBestScore);
            PlayerPrefs.DeleteKey(KeyBestWave);
            PlayerPrefs.DeleteKey(KeyBestGold);
            PlayerPrefs.DeleteKey(KeyBestXp);
            PlayerPrefs.DeleteKey(KeyLastFighterName);
            PlayerPrefs.DeleteKey(KeyLastBattleTimeSeconds);
            PlayerPrefs.DeleteKey(KeyLastDamageDealt);
            PlayerPrefs.DeleteKey(KeyLastDamageTaken);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// A run is better when score is higher, or score ties and wave is higher.
        /// </summary>
        public static bool IsRunBetter(int newScore, int newWave, int bestScore, int bestWave)
        {
            return newScore > bestScore || (newScore == bestScore && newWave > bestWave);
        }

        private static bool ShouldLogProgress()
        {
            return CombatDebug.TracePresentation || CombatDebug.LocalProgressDebug;
        }
    }
}
