using System.Collections.Generic;
using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Creates wave enemies using ProgressionEngine for stats/name and equipment lists for visuals.
    /// BattleManager assigns the equipment lists in the Inspector (drag Head/Body/Weapon/Mount SOs).
    ///
    /// Tip: fill lists from Assets/ScriptableObjects/... or use an Editor "Auto Wire" tool later.
    /// Empty lists are OK — the enemy simply spawns with no gear on that slot.
    /// </summary>
    public class EnemyFactory
    {
        private readonly List<HeadSO> _heads;
        private readonly List<BodySO> _bodies;
        private readonly List<WeaponSO> _weapons;
        private readonly List<MountSO> _mounts;

        /// <summary>
        /// Pass the four equipment lists from BattleManager (or another MonoBehaviour).
        /// Lists can be null or empty; GenerateEnemy still works.
        /// </summary>
        public EnemyFactory(
            List<HeadSO> heads,
            List<BodySO> bodies,
            List<WeaponSO> weapons,
            List<MountSO> mounts)
        {
            _heads = heads;
            _bodies = bodies;
            _weapons = weapons;
            _mounts = mounts;
        }

        /// <summary>
        /// Builds a fresh enemy for the given endless wave number (starts at 1).
        /// Stats and rewards come from ProgressionEngine; gear is picked from the modulo lists.
        /// </summary>
        public EnemyFighter GenerateEnemy(int waveNumber)
        {
            var progression = ProgressionEngine.GenerateProgression(waveNumber);
            var wave = progression.waveNumber;

            var displayName = progression.isBossWave
                ? "BOSS: " + progression.enemyArchetype
                : progression.enemyArchetype;

            var enemy = new EnemyFighter
            {
                enemyName = displayName,
                attack = progression.enemyAttack,
                defense = progression.enemyDefense,
                speed = progression.enemySpeed,
                maxHealth = progression.enemyMaxHealth,
                scoreReward = progression.scoreReward,
                rewardText = progression.rewardText,
                head = PickByWave(_heads, wave),
                body = PickByWave(_bodies, wave),
                weapon = PickByWave(_weapons, wave),
                mount = PickByWave(_mounts, wave),
                isAlive = true
            };

            enemy.currentHealth = enemy.maxHealth;
            return enemy;
        }

        /// <summary>
        /// Picks one item from a list using waveNumber % count for visual variety each wave.
        /// Returns null when the list is missing or empty.
        /// </summary>
        private static T PickByWave<T>(List<T> list, int waveNumber) where T : class
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            var index = waveNumber % list.Count;
            return list[index];
        }
    }
}
