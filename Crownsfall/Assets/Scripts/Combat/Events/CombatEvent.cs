namespace Crownsfall.Combat.Events
{
    /// <summary>
    /// Every kind of moment that can happen during a battle run.
    /// Other systems (UI, sound, analytics) listen for these instead of hard-coding battle logic.
    /// </summary>
    public enum CombatEventType
    {
        BattleStarted,
        WaveStarted,
        PlayerAttack,
        EnemyAttack,
        SkillTriggered,
        DamageDealt,
        HealingReceived,
        EnemyDefeated,
        PlayerDefeated,
        ScoreChanged,
        BossStarted,
        BattleWon,
        RunEnded
    }

    /// <summary>
    /// One battle moment packaged as data so listeners can react without touching BattleManager.
    ///
    /// Event systems are useful because they decouple combat from everything else:
    /// BattleManager only raises events; UI, sound, particles, analytics, and replay
    /// can subscribe later without changing combat code.
    /// </summary>
    [System.Serializable]
    public class CombatEvent
    {
        /// <summary>What happened (attack, defeat, score change, etc.).</summary>
        public CombatEventType eventType;

        /// <summary>Human-readable line for logs or UI (e.g. "Player dealt 5 damage.").</summary>
        public string message;

        /// <summary>Who caused the event (attacker name, "BattleManager", etc.).</summary>
        public string sourceName;

        /// <summary>Who was affected (defender name, enemy name, etc.).</summary>
        public string targetName;

        /// <summary>Damage dealt, score gained, or other numeric value when relevant.</summary>
        public int amount;

        /// <summary>Wave number when the event occurred (1-based).</summary>
        public int waveNumber;

        /// <summary>Player score after the event when relevant.</summary>
        public int score;

        /// <summary>True when this event belongs to a boss wave.</summary>
        public bool isBoss;

        /// <summary>True when player attack damage was a critical hit (used for floating text styling).</summary>
        public bool isCritical;

        /// <summary>Time.time when the event was raised (seconds since scene load).</summary>
        public float timestamp;
    }
}
