namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// What a skill effect did on one attack. BattleManager reads this to apply damage and log messages.
    /// </summary>
    public class SkillResult
    {
        /// <summary>Final damage after the skill runs (may equal base damage if nothing triggered).</summary>
        public int modifiedDamage;

        /// <summary>True when the skill actually fired (e.g. a crit landed).</summary>
        public bool wasTriggered;

        /// <summary>Optional log line when triggered (e.g. "CRITICAL HIT!"). Can be empty.</summary>
        public string message;

        /// <summary>When true, BattleManager applies burn DoT using burnDamage and burnDuration.</summary>
        public bool applyBurn;

        /// <summary>Damage dealt to the enemy at the start of each enemy turn while burning.</summary>
        public int burnDamage;

        /// <summary>How many enemy turns the burn lasts (one tick per enemy turn).</summary>
        public int burnDuration;

        /// <summary>When true, BattleManager applies poison DoT using poisonDamage and poisonDuration.</summary>
        public bool applyPoison;

        /// <summary>Damage dealt to the enemy at the start of each enemy turn while poisoned.</summary>
        public int poisonDamage;

        /// <summary>How many enemy turns the poison lasts (one tick per enemy turn).</summary>
        public int poisonDuration;
    }
}
