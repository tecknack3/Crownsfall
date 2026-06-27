using System.Collections.Generic;
using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Creates wave enemies with scaled stats, tier names, and equipment picked from lists.
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
        /// </summary>
        public EnemyFighter GenerateEnemy(int waveNumber)
        {
            var wave = Mathf.Max(1, waveNumber);

            var enemy = new EnemyFighter
            {
                enemyName = GetEnemyNameForWave(wave),
                attack = 4 + wave * 2,
                defense = 2 + Mathf.FloorToInt(wave * 0.75f),
                speed = 1 + Mathf.FloorToInt(wave * 0.35f),
                maxHealth = 80 + wave * 25,
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
        /// Tier name by wave band (endless mode gets harder names as waves climb).
        /// </summary>
        public static string GetEnemyNameForWave(int waveNumber)
        {
            if (waveNumber >= 100)
            {
                return "Shadow Emperor";
            }

            if (waveNumber >= 50)
            {
                return "Fire Dragon Rider";
            }

            if (waveNumber >= 20)
            {
                return "Crystal Titan";
            }

            if (waveNumber >= 10)
            {
                return "Bone Knight";
            }

            if (waveNumber >= 5)
            {
                return "Wild Raider";
            }

            return "Rookie Goblin";
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
