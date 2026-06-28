using System;

namespace Crownsfall.Combat.Events
{
    /// <summary>
    /// Central hub for combat events. BattleManager calls Raise; other systems subscribe to OnCombatEvent.
    ///
    /// Think of it like a radio station: BattleManager broadcasts, and UI/sound/analytics tune in.
    /// </summary>
    public static class CombatEventBus
    {
        /// <summary>Fired whenever Raise is called with a new CombatEvent.</summary>
        public static event Action<CombatEvent> OnCombatEvent;

        /// <summary>
        /// Broadcasts a combat event to all subscribers.
        /// Sets timestamp automatically if the caller did not set one.
        /// </summary>
        public static void Raise(CombatEvent combatEvent)
        {
            if (combatEvent == null)
            {
                return;
            }

            if (combatEvent.timestamp <= 0f)
            {
                combatEvent.timestamp = UnityEngine.Time.time;
            }

            OnCombatEvent?.Invoke(combatEvent);
        }
    }
}
