using UnityEngine;

namespace Crownsfall.Characters
{
    /// <summary>
    /// Identifies the kind of effect an equipped item can trigger in combat.
    /// v1 is data-only — BattleManager logs skills but does not execute them yet.
    /// </summary>
    public enum SkillType
    {
        None,
        BonusAttack,
        BonusDefense,
        BonusSpeed,
        CriticalStrike,
        LifeSteal,
        Shield,
        Burn,
        Poison,
        Freeze,
        Stun,
        HealOnHit,
        DoubleStrike,
        Rage,
        Execute
    }

    /// <summary>
    /// Skill payload stored on each EquipmentItemSO.
    /// value/chance/duration are reserved for future combat activation.
    /// </summary>
    [System.Serializable]
    public class EquipmentSkill
    {
        public SkillType skillType;
        public string skillName;
        [TextArea] public string description;
        public float value;
        public float chance;
        public float duration;
    }
}
