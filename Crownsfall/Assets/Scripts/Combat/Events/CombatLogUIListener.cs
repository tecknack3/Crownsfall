using Crownsfall.Combat;
using UnityEngine;

namespace Crownsfall.Combat.Events
{
    /// <summary>
    /// Primary source for on-screen battle log lines. Listens to CombatEventBus and appends
    /// formatted text to BattleHUD.
    ///
    /// BattleManager raises combat events only — it does not write duplicate log lines to the HUD.
    /// Debug output for development lives in Debug.Log / CombatEventLogger instead.
    /// </summary>
    public class CombatLogUIListener : MonoBehaviour
    {
        [Tooltip("Battle log panel to append lines to. Auto-found in scene if left empty.")]
        [SerializeField] private BattleHUD battleHUD;

        private void Awake()
        {
            if (battleHUD == null)
            {
                battleHUD = FindObjectOfType<BattleHUD>();
            }
        }

        private void OnEnable()
        {
            CombatEventBus.OnCombatEvent += HandleCombatEvent;
        }

        private void OnDisable()
        {
            CombatEventBus.OnCombatEvent -= HandleCombatEvent;
        }

        private void OnDestroy()
        {
            CombatEventBus.OnCombatEvent -= HandleCombatEvent;
        }

        private void HandleCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null || battleHUD == null)
            {
                return;
            }

            var line = FormatLogLine(combatEvent);

            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            battleHUD.AddLogLine(line);
        }

        /// <summary>
        /// Turns a combat event into one battle-log sentence. Returns null to skip low-value events.
        /// </summary>
        private static string FormatLogLine(CombatEvent combatEvent)
        {
            switch (combatEvent.eventType)
            {
                case CombatEventType.BattleStarted:
                    return "Battle begins!";

                case CombatEventType.WaveStarted:
                    if (!string.IsNullOrEmpty(combatEvent.message))
                    {
                        return combatEvent.message;
                    }

                    return $"Wave {combatEvent.waveNumber} begins!";

                case CombatEventType.BossStarted:
                    return $"BOSS WAVE {combatEvent.waveNumber}!";

                case CombatEventType.SkillTriggered:
                    return combatEvent.message;

                case CombatEventType.DamageDealt:
                    // BattleManager sets message with "!" when a skill also fired on this hit.
                    if (!string.IsNullOrEmpty(combatEvent.message))
                    {
                        return combatEvent.message;
                    }

                    return FormatDamageDealt(combatEvent);

                case CombatEventType.EnemyDefeated:
                    // e.g. "Goblin Scout defeated!"
                    if (!string.IsNullOrEmpty(combatEvent.message))
                    {
                        return combatEvent.message;
                    }

                    return "Enemy defeated!";

                case CombatEventType.ScoreChanged:
                    // Only show when BattleManager attached a reward message (e.g. "+1 Score").
                    return string.IsNullOrEmpty(combatEvent.message) ? null : combatEvent.message;

                case CombatEventType.PlayerDefeated:
                    return "Fighter defeated!";

                case CombatEventType.RunEnded:
                    return $"Final Score: {combatEvent.score}";

                // Attack wind-ups are too noisy for the on-screen log; damage/skill events cover the moment.
                case CombatEventType.PlayerAttack:
                case CombatEventType.EnemyAttack:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Fallback when DamageDealt has no message — infer player vs enemy from the source name.
        /// </summary>
        private static string FormatDamageDealt(CombatEvent combatEvent)
        {
            var isPlayerSource = IsLikelyPlayerSource(combatEvent.sourceName);

            if (isPlayerSource)
            {
                return $"Player dealt {combatEvent.amount} damage.";
            }

            return $"Enemy dealt {combatEvent.amount} damage.";
        }

        /// <summary>
        /// Heuristic: player sources are usually fighter names; wave enemies often contain "Enemy" or "Goblin", etc.
        /// Prefer event.message when BattleManager sets it — this is only a safety net.
        /// </summary>
        private static bool IsLikelyPlayerSource(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName))
            {
                return true;
            }

            var lower = sourceName.ToLowerInvariant();
            return !lower.Contains("enemy") && !lower.Contains("goblin") && !lower.Contains("boss");
        }
    }
}
