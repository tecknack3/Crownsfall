using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Services
{
    /// <summary>
    /// Saves and loads run results locally with Unity PlayerPrefs.
    /// No cloud or ads — device-only persistence for v1.
    /// </summary>
    public static class LocalSaveService
    {
        private const string KeyBestScore = "Crownsfall_BestScore";
        private const string KeyHighestWave = "Crownsfall_HighestWave";
        private const string KeyBestFighterName = "Crownsfall_BestFighterName";
        private const string KeyLastScore = "Crownsfall_LastScore";
        private const string KeyLastWave = "Crownsfall_LastWave";

        /// <summary>
        /// Records the end of a battle run and updates all-time records when beaten.
        /// Always stores lastScore and lastWave; best fields update only on new records.
        /// </summary>
        public static void SaveRunResult(PlayerFighter fighter, int finalScore, int waveReached)
        {
            var saveData = LoadSaveData();

            // New personal best score — remember who earned it.
            if (finalScore > saveData.bestScore)
            {
                saveData.bestScore = finalScore;
                saveData.bestFighterName = fighter != null && !string.IsNullOrEmpty(fighter.fighterName)
                    ? fighter.fighterName
                    : "Unknown Fighter";
            }

            // New deepest wave reached.
            if (waveReached > saveData.highestWave)
            {
                saveData.highestWave = waveReached;
            }

            // Every run updates the "last run" snapshot.
            saveData.lastScore = finalScore;
            saveData.lastWave = waveReached;

            WriteSaveData(saveData);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reads all saved values from PlayerPrefs. Missing keys default to zero / empty string.
        /// </summary>
        public static LocalSaveData LoadSaveData()
        {
            return new LocalSaveData
            {
                bestScore = PlayerPrefs.GetInt(KeyBestScore, 0),
                highestWave = PlayerPrefs.GetInt(KeyHighestWave, 0),
                bestFighterName = PlayerPrefs.GetString(KeyBestFighterName, string.Empty),
                lastScore = PlayerPrefs.GetInt(KeyLastScore, 0),
                lastWave = PlayerPrefs.GetInt(KeyLastWave, 0)
            };
        }

        /// <summary>
        /// Removes all Crownsfall save keys from PlayerPrefs (useful for testing or reset).
        /// </summary>
        public static void ClearSaveData()
        {
            PlayerPrefs.DeleteKey(KeyBestScore);
            PlayerPrefs.DeleteKey(KeyHighestWave);
            PlayerPrefs.DeleteKey(KeyBestFighterName);
            PlayerPrefs.DeleteKey(KeyLastScore);
            PlayerPrefs.DeleteKey(KeyLastWave);
            PlayerPrefs.Save();
        }

        private static void WriteSaveData(LocalSaveData saveData)
        {
            PlayerPrefs.SetInt(KeyBestScore, saveData.bestScore);
            PlayerPrefs.SetInt(KeyHighestWave, saveData.highestWave);
            PlayerPrefs.SetString(KeyBestFighterName, saveData.bestFighterName ?? string.Empty);
            PlayerPrefs.SetInt(KeyLastScore, saveData.lastScore);
            PlayerPrefs.SetInt(KeyLastWave, saveData.lastWave);
        }
    }
}
