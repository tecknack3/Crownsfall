using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// Poison: weaker, longer damage-over-time (DoT) on player attack. Like Burn, rolls chance to
    /// apply a status that deals skill.value damage at the start of each enemy turn for skill.duration
    /// enemy turns. Typical poison has lower damage per tick but more turns than burn.
    /// Can be equipped on weapon OR mount — BattleManager checks both slots on player attack.
    /// </summary>
    public class PoisonSkill : ISkillEffect
    {
        public SkillType SkillType => SkillType.Poison;

        /// <summary>
        /// Roll Random.value (0–1). If roll &lt;= chance/100, flag poison for BattleManager to apply.
        /// modifiedDamage stays the same because poison is DoT, not bonus hit damage.
        /// </summary>
        public SkillResult ApplyOnPlayerAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            var result = new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null,
                applyPoison = false
            };

            // chance is stored as a percent (e.g. 40 = 40% chance to apply poison).
            var roll = Random.value;
            if (roll > skill.chance / 100f)
            {
                return result;
            }

            result.wasTriggered = true;
            result.message = "POISON APPLIED!";
            result.applyPoison = true;
            result.poisonDamage = Mathf.RoundToInt(skill.value);
            result.poisonDuration = (int)skill.duration;
            return result;
        }

        /// <summary>
        /// Poison does not apply on enemy attacks — return base damage unchanged.
        /// </summary>
        public SkillResult ApplyOnEnemyAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            return new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null,
                applyPoison = false
            };
        }
    }
}