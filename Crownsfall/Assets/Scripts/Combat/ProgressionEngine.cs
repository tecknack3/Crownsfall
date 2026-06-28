using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Builds wave progression data: archetype bands, scaled stats, boss waves, and score rewards.
    /// Call GenerateProgression(waveNumber) from EnemyFactory and BattleManager.
    /// </summary>
    public static class ProgressionEngine
    {
        /// <summary>
        /// Returns a full progression snapshot for the given wave (minimum wave 1).
        /// Boss waves (10, 20, 30…) get boosted stats and +5 score instead of +1.
        /// </summary>
        public static ProgressionProfile GenerateProgression(int waveNumber)
        {
            var wave = Mathf.Max(1, waveNumber);
            var isBoss = wave % 10 == 0;
            var archetype = GetArchetypeForWave(wave);

            // Regular stat formulas — same as the old EnemyFactory scaling.
            var attack = 4 + wave * 2;
            var defense = 2 + Mathf.FloorToInt(wave * 0.75f);
            var speed = 1 + Mathf.FloorToInt(wave * 0.35f);
            var maxHealth = 80 + wave * 25;
            var scoreReward = 1;
            var rewardText = "+1 Score";

            // Boss waves stack extra power on top of regular stats.
            if (isBoss)
            {
                attack += 5;
                defense += 3;
                speed += 1;
                maxHealth *= 2;
                scoreReward = 5;
                rewardText = "Boss defeated! +5 Score";
            }

            return new ProgressionProfile
            {
                waveNumber = wave,
                isBossWave = isBoss,
                enemyArchetype = archetype,
                enemyAttack = attack,
                enemyDefense = defense,
                enemySpeed = speed,
                enemyMaxHealth = maxHealth,
                scoreReward = scoreReward,
                rewardText = rewardText
            };
        }

        /// <summary>
        /// Picks the enemy archetype name based on which wave band the player has reached.
        /// </summary>
        private static string GetArchetypeForWave(int waveNumber)
        {
            if (waveNumber >= 100)
            {
                return "Shadow Emperor";
            }

            if (waveNumber >= 50)
            {
                return "Fire Dragon Rider";
            }

            if (waveNumber >= 20)
            {
                return "Crystal Titan";
            }

            if (waveNumber >= 10)
            {
                return "Bone Knight";
            }

            if (waveNumber >= 5)
            {
                return "Wild Raider";
            }

            return "Rookie Goblin";
        }
    }
}
