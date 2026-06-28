using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Builds wave progression data: archetype bands, scaled stats, boss waves, and score rewards.
    /// Scaling constants come from CombatBalance — edit CombatBalanceSO in the Inspector to rebalance.
    /// Call GenerateProgression(waveNumber) from EnemyFactory and BattleManager.
    /// </summary>
    public static class ProgressionEngine
    {
        /// <summary>
        /// Returns a full progression snapshot for the given wave (minimum wave 1).
        /// Boss waves get boosted stats and a larger score reward from CombatBalance.
        /// </summary>
        public static ProgressionProfile GenerateProgression(int waveNumber)
        {
            var balance = CombatBalance.Active;
            var wave = Mathf.Max(1, waveNumber);
            var isBoss = balance.bossWaveInterval > 0 && wave % balance.bossWaveInterval == 0;
            var archetype = GetArchetypeForWave(wave);

            var attack = balance.enemyAttackBase + wave * balance.enemyAttackPerWave;
            var defense = balance.enemyDefenseBase + Mathf.FloorToInt(wave * balance.enemyDefensePerWave);
            var speed = balance.enemySpeedBase + Mathf.FloorToInt(wave * balance.enemySpeedPerWave);
            var maxHealth = balance.enemyMaxHealthBase + wave * balance.enemyMaxHealthPerWave;
            var scoreReward = balance.scorePerWave;
            var rewardText = balance.scoreRewardText;

            if (isBoss)
            {
                attack += balance.bossAttackBonus;
                defense += balance.bossDefenseBonus;
                speed += balance.bossSpeedBonus;
                maxHealth *= balance.bossHealthMultiplier;
                scoreReward = balance.bossScoreReward;
                rewardText = balance.bossScoreRewardText;
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
