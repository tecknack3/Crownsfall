using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// Burn: damage-over-time (DoT) on player attack. Rolls chance to apply a status that
    /// deals skill.value damage at the start of each enemy turn for skill.duration enemy turns.
    /// Instant attack damage is unchanged — burn ticks separately in BattleManager.
    /// </summary>
    public class BurnSkill : ISkillEffect
    {
        public SkillType SkillType => SkillType.Burn;

        /// <summary>
        /// Roll Random.value (0–1). If roll &lt;= chance/100, flag burn for BattleManager to apply.
        /// modifiedDamage stays the same because burn is DoT, not bonus hit damage.
        /// </summary>
        public SkillResult ApplyOnPlayerAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            var result = new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null,
                applyBurn = false
            };

            // chance is stored as a percent (e.g. 30 = 30% chance to apply burn).
            var roll = Random.value;
            if (roll > skill.chance / 100f)
            {
                return result;
            }

            result.wasTriggered = true;
            result.message = "BURN APPLIED!";
            result.applyBurn = true;
            result.burnDamage = Mathf.RoundToInt(skill.value);
            result.burnDuration = (int)skill.duration;
            return result;
        }

        /// <summary>
        /// Burn does not apply on enemy attacks — return base damage unchanged.
        /// </summary>
        public SkillResult ApplyOnEnemyAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            return new SkillResult
            {
                modifiedDamage = baseDamage,
                wasTriggered = false,
                message = null,
                applyBurn = false
            };
        }
    }
}
