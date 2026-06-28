namespace Crownsfall.Combat
{
    /// <summary>
    /// Plain C# data for one endless wave: enemy stats, archetype, boss flag, and score reward.
    /// Built by ProgressionEngine — EnemyFactory reads stats/name; BattleManager reads rewards.
    /// </summary>
    public class ProgressionProfile
    {
        /// <summary>Which wave this profile belongs to (1, 2, 3…).</summary>
        public int waveNumber;

        /// <summary>True on every 10th wave (10, 20, 30…) — stronger stats and bigger score reward.</summary>
        public bool isBossWave;

        /// <summary>Base enemy type for this wave band (e.g. "Bone Knight"). Boss prefix is added in EnemyFactory.</summary>
        public string enemyArchetype;

        /// <summary>Enemy attack stat for this wave.</summary>
        public int enemyAttack;

        /// <summary>Enemy defense stat for this wave.</summary>
        public int enemyDefense;

        /// <summary>Enemy speed stat for this wave.</summary>
        public int enemySpeed;

        /// <summary>Enemy max HP for this wave.</summary>
        public int enemyMaxHealth;

        /// <summary>Score points awarded when this wave's enemy is defeated.</summary>
        public int scoreReward;

        /// <summary>Battle log line shown when the enemy is defeated (e.g. "+1 Score").</summary>
        public string rewardText;
    }
}
