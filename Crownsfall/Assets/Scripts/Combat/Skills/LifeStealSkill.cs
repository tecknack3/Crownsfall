using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// Life Steal: on player attack, roll chance to heal the player for a fraction of
    /// damage dealt. Heal amount is calculated in BattleManager AFTER final damage is
    /// applied (so crits and other damage modifiers count). Can be equipped on weapon OR mount.
    /// </summary>
    public class LifeStealSkill : ISkillEffect
    {
        public SkillType SkillType => SkillType.LifeSteal;

        /// <summary>
        /// Roll Random.value (0–1). If roll &lt;= chance/100, flag life steal for BattleManager.
        /// modifiedDamage stays the same — healing happens after the enemy takes damage.
        /// </summary>
        public SkillResult ApplyOnPlayerAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            var result = new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null,
                applyLifeSteal = false
            };

            // chance is stored as a percent (e.g. 30 = 30% chance to life steal).
            var roll = Random.value;
            if (roll > skill.chance / 100f)
            {
                return result;
            }

            result.wasTriggered = true;
            result.message = "LIFE STEAL!";
            result.applyLifeSteal = true;
            // value is the heal fraction (e.g. 0.25 = heal 25% of damage actually dealt).
            result.lifeStealPercent = skill.value;
            return result;
        }

        /// <summary>
        /// Life Steal does not apply on enemy attacks — return base damage unchanged.
        /// </summary>
        public SkillResult ApplyOnEnemyAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            return new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null,
                applyLifeSteal = false
            };
        }
    }
}
