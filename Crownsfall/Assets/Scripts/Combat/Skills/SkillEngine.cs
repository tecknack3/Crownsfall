using System.Collections.Generic;
using Crownsfall.Characters;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// Looks up skill effects by SkillType and runs them during combat.
    /// v1 registers Critical Strike, Shield, Burn, Poison, and Life Steal; more skills can be added later.
    /// </summary>
    public class SkillEngine
    {
        // Maps each SkillType to its handler class (e.g. CriticalStrike -> CriticalStrikeSkill).
        private readonly Dictionary<SkillType, ISkillEffect> _effects = new Dictionary<SkillType, ISkillEffect>();

        /// <summary>
        /// Registers built-in skill effects. Call once when battle starts.
        /// </summary>
        public SkillEngine()
        {
            RegisterEffect(new CriticalStrikeSkill());
            RegisterEffect(new ShieldSkill());
            RegisterEffect(new BurnSkill());
            RegisterEffect(new PoisonSkill());
            RegisterEffect(new LifeStealSkill());
        }

        /// <summary>
        /// Adds one skill effect to the lookup table.
        /// </summary>
        private void RegisterEffect(ISkillEffect effect)
        {
            _effects[effect.SkillType] = effect;
        }

        /// <summary>
        /// Runs the weapon skill (or any single EquipmentSkill) on a player attack.
        /// If skill is null, None, or not registered, returns base damage unchanged.
        /// </summary>
        public SkillResult ApplyPlayerAttackSkills(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            return ApplySkill(baseDamage, skill, context, isPlayerAttack: true);
        }

        /// <summary>
        /// Runs a skill on an enemy attack. v1 passes damage through for unregistered types.
        /// </summary>
        public SkillResult ApplyEnemyAttackSkills(int baseDamage, EquipmentSkill skill, BattleContext context)
        {
            return ApplySkill(baseDamage, skill, context, isPlayerAttack: false);
        }

        /// <summary>
        /// Shared path: find the effect for skill.skillType and call the right Apply method.
        /// </summary>
        private SkillResult ApplySkill(int baseDamage, EquipmentSkill skill, BattleContext context, bool isPlayerAttack)
        {
            // No skill data — nothing to do.
            if (skill == null || skill.skillType == SkillType.None)
            {
                return PassThrough(baseDamage);
            }

            // Skill type exists in data but we have no handler yet (Burn, LifeSteal, etc.).
            if (!_effects.TryGetValue(skill.skillType, out var effect))
            {
                return PassThrough(baseDamage);
            }

            return isPlayerAttack
                ? effect.ApplyOnPlayerAttack(baseDamage, skill, context)
                : effect.ApplyOnEnemyAttack(baseDamage, skill, context);
        }

        /// <summary>
        /// Default result when no skill runs: same damage, not triggered, no message.
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
