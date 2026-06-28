using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// Shield: on enemy attack, reduce incoming damage by skill.value percent (body armor skill).
    /// Only applies on enemy attacks — player attacks are unchanged.
    /// </summary>
    public class ShieldSkill : ISkillEffect
    {
        public SkillType SkillType => SkillType.Shield;

        /// <summary>
        /// Shield does not apply on player attacks — return base damage unchanged.
        /// </summary>
        public SkillResult ApplyOnPlayerAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            return PassThrough(baseDamage);
        }

        /// <summary>
        /// Reduce incoming damage by value percent (e.g. value=30 means 30% less damage).
        /// Final damage is at least 1; logs how much was blocked when shield applies.
        /// </summary>
        public SkillResult ApplyOnEnemyAttack(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            if (skill == null || skill.skillType != SkillType.Shield)
            {
                return PassThrough(baseDamage);
            }

            // value is a percent reduction (e.g. 30 = block 30% of incoming damage).
            var rawModified = baseDamage * (1f - skill.value / 100f);
            var modifiedDamage = Mathf.Max(1, Mathf.RoundToInt(rawModified));
            var blockedAmount = baseDamage - modifiedDamage;

            return new SkillResult
            {
                modifiedDamage = modifiedDamage,
                wasTriggered = true,
                message = $"SHIELD BLOCKED {blockedAmount} damage!"
            };
        }

        /// <summary>
        /// Default when shield does not run: same damage, not triggered, no message.
        /// </summary>
        private static SkillResult PassThrough(int baseDamage)
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
