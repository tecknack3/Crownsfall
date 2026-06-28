using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Central combat tuning data. Edit values in the Inspector to rebalance waves, score, and skill templates
    /// without changing combat logic. Assign to BattleManager or create via Tools → Fighter Tools → Create Combat Balance Asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Fighter/Combat Balance", fileName = "CombatBalance")]
    public class CombatBalanceSO : ScriptableObject
    {
        [Header("Enemy Attack Scaling")]
        [Tooltip("Enemy attack on wave 1. Each wave adds Enemy Attack Per Wave.")]
        public int enemyAttackBase = 4;

        [Tooltip("Attack added per wave. Formula: base + wave × perWave (e.g. wave 3 → 4 + 3×2 = 10).")]
        public int enemyAttackPerWave = 2;

        [Header("Enemy Defense Scaling")]
        [Tooltip("Enemy defense on wave 1.")]
        public int enemyDefenseBase = 2;

        [Tooltip("Defense growth per wave (fractional — floored). Formula: base + floor(wave × perWave).")]
        public float enemyDefensePerWave = 0.75f;

        [Header("Enemy Speed Scaling")]
        [Tooltip("Enemy speed on wave 1.")]
        public int enemySpeedBase = 1;

        [Tooltip("Speed added per wave (fractional — floored). Formula: base + floor(wave × perWave).")]
        public float enemySpeedPerWave = 0.35f;

        [Header("Enemy Health Scaling")]
        [Tooltip("Enemy max HP on wave 1.")]
        public int enemyMaxHealthBase = 80;

        [Tooltip("Max HP added per wave. Formula: base + wave × perWave.")]
        public int enemyMaxHealthPerWave = 25;

        [Header("Boss Waves")]
        [Tooltip("Every Nth wave is a boss (e.g. 10 = waves 10, 20, 30…).")]
        public int bossWaveInterval = 10;

        [Tooltip("Extra attack stacked on boss waves.")]
        public int bossAttackBonus = 5;

        [Tooltip("Extra defense stacked on boss waves.")]
        public int bossDefenseBonus = 3;

        [Tooltip("Extra speed stacked on boss waves.")]
        public int bossSpeedBonus = 1;

        [Tooltip("Boss max HP multiplier applied after regular wave scaling (×2 = double HP).")]
        public int bossHealthMultiplier = 2;

        [Header("Score Rewards")]
        [Tooltip("Score points for defeating a normal wave enemy.")]
        public int scorePerWave = 1;

        [Tooltip("Score points for defeating a boss wave enemy.")]
        public int bossScoreReward = 5;

        [Tooltip("Battle log text when a normal enemy is defeated.")]
        public string scoreRewardText = "+1 Score";

        [Tooltip("Battle log text when a boss enemy is defeated.")]
        public string bossScoreRewardText = "Boss defeated! +5 Score";

        // XP is not implemented yet — score is the run progression reward. Add xpPerWave here when XP ships.

        [Header("Skill Template Defaults")]
        [Tooltip("Default crit damage multiplier for new equipment (2 = double damage). Runtime uses each item's EquipmentSkill.value.")]
        public float criticalDamageMultiplier = 2f;

        [Tooltip("Default burn damage per enemy turn for new equipment. Runtime uses each item's EquipmentSkill.value.")]
        public int burnDamage = 5;

        [Tooltip("Default burn duration in enemy turns for new equipment. Runtime uses each item's EquipmentSkill.duration.")]
        public int burnDuration = 3;

        [Tooltip("Default poison damage per enemy turn for new equipment (weaker per tick than burn). Runtime uses EquipmentSkill.value.")]
        public int poisonDamage = 3;

        [Tooltip("Default poison duration in enemy turns for new equipment (often longer than burn). Runtime uses EquipmentSkill.duration.")]
        public int poisonDuration = 5;

        [Tooltip("Default shield block percent for new equipment (30 = block 30% of incoming damage). Runtime uses EquipmentSkill.value.")]
        public float shieldBlockPercent = 30f;

        [Tooltip("Default life steal heal fraction for new equipment (0.25 = heal 25% of damage dealt). Runtime uses EquipmentSkill.value.")]
        public float lifeStealPercent = 0.25f;

        /// <summary>
        /// Creates an in-memory instance with the same defaults as a fresh ScriptableObject asset.
        /// Used when no CombatBalance asset is assigned or found.
        /// </summary>
        public static CombatBalanceSO CreateWithDefaults()
        {
            return CreateInstance<CombatBalanceSO>();
        }
    }
}
