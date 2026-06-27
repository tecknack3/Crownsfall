using UnityEngine;

namespace Crownsfall.Characters
{
    /// <summary>
    /// Represents one wave enemy in battle. Plain C# class — no Unity UI dependency.
    /// Stats are set by EnemyFactory from wave number; equipment is picked from enemy gear lists.
    /// </summary>
    public class EnemyFighter
    {
        // --- Identity ---

        /// <summary>Display name shown in HUD and battle log (e.g. "Rookie Goblin").</summary>
        public string enemyName;

        // --- Equipment (visual variety from EnemyFactory lists) ---

        public HeadSO head;
        public BodySO body;
        public WeaponSO weapon;
        public MountSO mount;

        // --- Combat stats (scaled by wave in EnemyFactory) ---

        public int attack;
        public int defense;
        public int speed;
        public int maxHealth;
        public int currentHealth;

        /// <summary>False when currentHealth reaches 0.</summary>
        public bool isAlive = true;

        /// <summary>
        /// Reduces currentHealth by damage, clamped to 0. Marks the enemy dead at 0 HP.
        /// Same idea as PlayerFighter.TakeDamage.
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHealth -= damage;

            if (currentHealth < 0)
            {
                currentHealth = 0;
            }

            if (currentHealth == 0)
            {
                isAlive = false;
            }
        }

        /// <summary>
        /// Builds a temporary PlayerFighter so FighterRig.Display() can show enemy gear.
        /// Use this for visuals only — keep using EnemyFighter for combat logic.
        /// </summary>
        public PlayerFighter ToDisplayFighter()
        {
            return new PlayerFighter
            {
                fighterName = enemyName,
                head = head,
                body = body,
                weapon = weapon,
                mount = mount,
                attack = attack,
                defense = defense,
                speed = speed,
                maxHealth = maxHealth,
                currentHealth = currentHealth,
                isAlive = isAlive
            };
        }
    }
}
