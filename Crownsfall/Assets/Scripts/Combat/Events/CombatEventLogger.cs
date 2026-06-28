using UnityEngine;

namespace Crownsfall.Combat.Events
{
    /// <summary>
    /// Optional debug listener: attach to any GameObject in the Battle scene to print
    /// every combat event to the Unity Console.
    ///
    /// This is a simple example of the event system — future listeners can play sounds,
    /// update UI, or send analytics without changing BattleManager.
    /// </summary>
    public class CombatEventLogger : MonoBehaviour
    {
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
            if (combatEvent == null)
            {
                return;
            }

            Debug.Log($"[CombatEvent] {combatEvent.eventType}: {combatEvent.message}");
        }
    }
}
