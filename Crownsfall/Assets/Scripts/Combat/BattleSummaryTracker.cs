using System;
using System.Collections.Generic;
using Crownsfall.Characters;
using Crownsfall.Combat.Events;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Accumulates battle stats during a run via CombatEventBus and wave reward hooks.
    /// Presentation-only — does not affect combat math or rewards.
    /// </summary>
    public class BattleSummaryTracker : IDisposable
    {
        private static readonly SkillType[] TrackedSkills =
        {
            SkillType.LifeSteal,
            SkillType.Shield,
            SkillType.CriticalStrike
        };

        private readonly string _playerName;
        private readonly float _battleStartTime;
        private int _goldEarned;
        private int _xpEarned;
        private int _damageDealt;
        private int _damageTaken;
        private readonly Dictionary<SkillType, int> _skillActivations = new Dictionary<SkillType, int>();
        private bool _disposed;

        public BattleSummaryTracker(string playerName)
        {
            _playerName = playerName ?? string.Empty;
            _battleStartTime = Time.time;

            foreach (var skillType in TrackedSkills)
            {
                _skillActivations[skillType] = 0;
            }

            CombatEventBus.OnCombatEvent += HandleCombatEvent;
        }

        /// <summary>
        /// Records gold/XP at the same moment BattleManager shows the wave reward screen.
        /// </summary>
        public void RecordWaveRewards(int gold, int xp)
        {
            _goldEarned += gold;
            _xpEarned += xp;

            if (CombatDebug.TracePresentation)
            {
                Debug.Log($"BattleSummaryTracker: wave rewards +{gold} gold, +{xp} xp (totals {_goldEarned}/{_xpEarned}).");
            }
        }

        /// <summary>
        /// Builds the summary snapshot for the victory screen.
        /// </summary>
        public BattleSummaryData BuildSummary(int wavesCleared, int totalWaves, int finalScore)
        {
            var data = new BattleSummaryData
            {
                PlayerName = _playerName,
                WavesCleared = wavesCleared,
                TotalWaves = totalWaves,
                HighestWave = wavesCleared,
                FinalScore = finalScore,
                GoldEarned = _goldEarned,
                XpEarned = _xpEarned,
                DamageDealt = _damageDealt,
                DamageTaken = _damageTaken,
                BattleDurationSeconds = Mathf.Max(0f, Time.time - _battleStartTime)
            };

            foreach (var pair in _skillActivations)
            {
                if (pair.Value > 0)
                {
                    data.SkillActivations[pair.Key] = pair.Value;
                }
            }

            return data;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CombatEventBus.OnCombatEvent -= HandleCombatEvent;
        }

        private void HandleCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null)
            {
                return;
            }

            switch (combatEvent.eventType)
            {
                case CombatEventType.DamageDealt:
                    HandleDamageDealt(combatEvent);
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

            if (IsDamageToEnemy(combatEvent))
            {
                _damageDealt += combatEvent.amount;
                return;
            }

            if (IsDamageToPlayer(combatEvent))
            {
                _damageTaken += combatEvent.amount;
            }
        }

        private void HandleSkillTriggered(CombatEvent combatEvent)
        {
            if (!TryMapSkillMessage(combatEvent.message, out var skillType))
            {
                return;
            }

            if (!_skillActivations.ContainsKey(skillType))
            {
                _skillActivations[skillType] = 0;
            }

            _skillActivations[skillType]++;
        }

        private static bool TryMapSkillMessage(string message, out SkillType skillType)
        {
            skillType = SkillType.None;

            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            if (message == "CRITICAL HIT!")
            {
                skillType = SkillType.CriticalStrike;
                return true;
            }

            if (message == "LIFE STEAL!")
            {
                skillType = SkillType.LifeSteal;
                return true;
            }

            if (message.StartsWith("SHIELD", StringComparison.Ordinal))
            {
                skillType = SkillType.Shield;
                return true;
            }

            return false;
        }

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

            return sourceName.StartsWith("Enemy", StringComparison.OrdinalIgnoreCase)
                || sourceName.Contains("Wave", StringComparison.OrdinalIgnoreCase);
        }
    }
}
