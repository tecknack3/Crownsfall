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
    }
}
