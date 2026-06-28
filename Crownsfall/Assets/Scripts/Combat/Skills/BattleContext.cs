using Crownsfall.Characters;

namespace Crownsfall.Combat.Skills
{
    /// <summary>
    /// Shared battle state passed into skill effects so they can read fighters,
    /// wave number, score, and write to the combat log without knowing about BattleManager.
    /// </summary>
    public class BattleContext
    {
        /// <summary>The player's fighter for this battle.</summary>
        public PlayerFighter player;

        /// <summary>The current wave enemy.</summary>
        public EnemyFighter enemy;

        /// <summary>Current wave number (1-based).</summary>
        public int currentWave;

        /// <summary>Player score so far this run.</summary>
        public int currentScore;

        /// <summary>Callback to add a line to the battle log (BattleManager wires LogCombatMessage here).</summary>
        public System.Action<string> logMessage;
    }
}
