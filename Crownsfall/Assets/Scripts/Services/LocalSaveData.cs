namespace Crownsfall.Services
{
    /// <summary>
    /// Plain data container for local PlayerPrefs save values.
    /// Loaded by LocalSaveService and shown on the game over screen.
    /// </summary>
    public class LocalSaveData
    {
        /// <summary>All-time highest score across every run.</summary>
        public int bestScore;

        /// <summary>All-time deepest wave reached across every run.</summary>
        public int highestWave;

        /// <summary>Fighter name tied to the current best score.</summary>
        public string bestFighterName;

        /// <summary>Score from the most recently completed run.</summary>
        public int lastScore;

        /// <summary>Wave reached in the most recently completed run.</summary>
        public int lastWave;
    }
}
