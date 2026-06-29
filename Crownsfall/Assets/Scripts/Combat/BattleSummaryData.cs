using System.Collections.Generic;
using Crownsfall.Characters;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Snapshot of run stats shown on the post-victory Battle Summary screen (Wave 3 only).
    /// </summary>
    public class BattleSummaryData
    {
        public string PlayerName;
        public int WavesCleared;
        public int TotalWaves;
        public int FinalScore;
        public int GoldEarned;
        public int XpEarned;
        public int DamageDealt;
        public int DamageTaken;
        public Dictionary<SkillType, int> SkillActivations = new Dictionary<SkillType, int>();
        public float BattleDurationSeconds;
    }
}
