using Crownsfall.Combat.Events;
using UnityEngine;

namespace Crownsfall.Combat.UI
{
    /// <summary>
    /// Listens to CombatEventBus and spawns floating numbers above fighter rigs.
    /// Wire player/enemy FighterRig references and the FloatingCombatText prefab in the Inspector,
    /// or run Tools → Fighter Tools → Setup Battle Scene Production UI to auto-wire the battle scene.
    /// </summary>
    public class FloatingCombatTextSpawner : MonoBehaviour
    {
        private static readonly Color DamageColor = new Color(1f, 0.267f, 0.267f, 1f);   // #FF4444
        private static readonly Color HealColor = new Color(0.267f, 1f, 0.4f, 1f);       // green
        private static readonly Color CritColor = new Color(1f, 0.69f, 0.125f, 1f);        // orange-yellow

        [Header("Prefab")]
        [SerializeField] private FloatingCombatText floatingCombatTextPrefab;

        [Header("Fighter Rigs")]
        [SerializeField] private FighterRig playerFighterRig;
        [SerializeField] private FighterRig enemyFighterRig;

        private void Awake()
        {
            FindSceneReferencesIfNeeded();
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
            if (combatEvent == null || floatingCombatTextPrefab == null)
            {
                return;
            }

            switch (combatEvent.eventType)
            {
                case CombatEventType.DamageDealt:
                    HandleDamageDealt(combatEvent);
                    break;

                case CombatEventType.HealingReceived:
                    SpawnAtAnchor(
                        GetPlayerDamageAnchor(),
                        $"+{combatEvent.amount} HP",
                        HealColor);
                    break;

                case CombatEventType.SkillTriggered:
                    HandleSkillTriggered(combatEvent);
                    break;
            }
        }

        private void HandleDamageDealt(CombatEvent combatEvent)
        {
            if (combatEvent.amount <= 0)
            {
                return;
            }

            var text = $"-{combatEvent.amount}";

            if (IsDamageToEnemy(combatEvent))
            {
                SpawnAtAnchor(GetEnemyDamageAnchor(), text, DamageColor);
                return;
            }

            if (IsDamageToPlayer(combatEvent))
            {
                SpawnAtAnchor(GetPlayerDamageAnchor(), text, DamageColor);
            }
        }

        /// <summary>
        /// v1 only shows a crit label for Critical Strike ("CRITICAL HIT!").
        /// The actual crit damage number comes from the following DamageDealt event.
        /// </summary>
        private void HandleSkillTriggered(CombatEvent combatEvent)
        {
            if (!IsCriticalHitMessage(combatEvent.message))
            {
                return;
            }

            SpawnAtAnchor(GetEnemyDamageAnchor(), "CRIT!", CritColor);
        }

        private void SpawnAtAnchor(RectTransform anchor, string text, Color color)
        {
            if (anchor == null)
            {
                return;
            }

            var instance = Instantiate(floatingCombatTextPrefab, anchor);
            instance.Initialize(text, color);
        }

        private RectTransform GetPlayerDamageAnchor()
        {
            return playerFighterRig != null ? playerFighterRig.DamageAnchor : null;
        }

        private RectTransform GetEnemyDamageAnchor()
        {
            return enemyFighterRig != null ? enemyFighterRig.DamageAnchor : null;
        }

        /// <summary>
        /// BattleManager always sets message on DamageDealt — use those prefixes first.
        /// </summary>
        private static bool IsDamageToEnemy(CombatEvent combatEvent)
        {
            var message = combatEvent.message;

            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith("Enemy dealt"))
                {
                    return false;
                }

                if (message.Contains("Player dealt")
                    || message.Contains("Burn dealt")
                    || message.Contains("Poison dealt"))
                {
                    return true;
                }
            }

            // Fallback when message is missing: player-side sources are fighter names, not skill/enemy labels.
            return !IsLikelyEnemySource(combatEvent.sourceName);
        }

        private static bool IsDamageToPlayer(CombatEvent combatEvent)
        {
            var message = combatEvent.message;

            if (!string.IsNullOrEmpty(message))
            {
                return message.StartsWith("Enemy dealt");
            }

            return IsLikelyEnemySource(combatEvent.sourceName);
        }

        private static bool IsLikelyEnemySource(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName))
            {
                return false;
            }

            var lower = sourceName.ToLowerInvariant();
            return lower.Contains("enemy") || lower.Contains("goblin") || lower.Contains("boss");
        }

        private static bool IsCriticalHitMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            return message.IndexOf("CRITICAL HIT", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Finds rigs by scene object name when Inspector references are empty.
        /// </summary>
        private void FindSceneReferencesIfNeeded()
        {
            if (playerFighterRig == null)
            {
                var playerObject = GameObject.Find("PlayerFighterRig");
                if (playerObject != null)
                {
                    playerFighterRig = playerObject.GetComponent<FighterRig>();
                }
            }

            if (enemyFighterRig == null)
            {
                var enemyObject = GameObject.Find("EnemyFighterRig");
                if (enemyObject != null)
                {
                    enemyFighterRig = enemyObject.GetComponent<FighterRig>();
                }
            }
        }
    }
}
