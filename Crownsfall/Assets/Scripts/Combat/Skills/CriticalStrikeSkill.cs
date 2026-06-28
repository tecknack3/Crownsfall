using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// Critical Strike: on player attack, roll chance to multiply damage by skill.value.
    /// Only applies on player attacks — enemy attacks are unchanged.
    /// </summary>
    public class CriticalStrikeSkill : ISkillEffect
    {
        public SkillType SkillType => SkillType.CriticalStrike;

        /// <summary>
        /// Roll Random.value (0–1). If roll &lt;= chance/100, multiply damage and mark triggered.
        /// </summary>
        public SkillResult ApplyOnPlayerAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            // Start with normal damage — no crit yet.
            var result = new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null
            };

            // Random.value is 0–1. chance is stored as a percent (e.g. 25 = 25% crit chance).
            var roll = Random.value;
            if (roll > skill.chance / 100f)
            {
                return result;
            }

            // Crit landed — value is a multiplier (e.g. 2.0 = double damage).
            result.modifiedDamage = Mathf.RoundToInt(baseDamage * skill.value);
            result.wasTriggered = true;
            result.message = "CRITICAL HIT!";
            return result;
        }

        /// <summary>
        /// Critical Strike does not apply on enemy attacks — return base damage unchanged.
        /// </summary>
        public SkillResult ApplyOnEnemyAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            return new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null
            };
        }
    }
}
