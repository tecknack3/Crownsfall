using Crownsfall.Characters;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// One combat skill implementation. Each SkillType gets its own class (e.g. CriticalStrikeSkill).
    /// </summary>
    public interface ISkillEffect
    {
        /// <summary>Which SkillType this class handles.</summary>
        SkillType SkillType { get; }

        /// <summary>Runs when the player attacks. Returns updated damage and optional log message.</summary>
        SkillResult ApplyOnPlayerAttack(int baseDamage, EquipmentSkill skill, BattleContext context);

        /// <summary>Runs when an enemy attacks. v1 mostly passes damage through unchanged.</summary>
        SkillResult ApplyOnEnemyAttack(int baseDamage, EquipmentSkill skill, BattleContext context);
    }
}
