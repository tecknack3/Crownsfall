using System;

namespace Crownsfall.Characters
{
    /// <summary>
    /// Represents the player's fighter in battle. Plain C# class — no Unity UI dependency.
    /// Stats come from equipped Head, Body, Weapon, and Mount ScriptableObjects.
    /// </summary>
    public class PlayerFighter
    {
        // --- Identity ---

        public string fighterId;
        public string fighterName;

        // --- Equipment ---

        public HeadSO head;
        public BodySO body;
        public WeaponSO weapon;
        public MountSO mount;

        // --- Calculated stats (updated by CalculateStats) ---

        public int attack;
        public int defense;
        public int speed;
        public int maxHealth;
        public int currentHealth;

        // --- Gameplay ---

        public int currentScore;
        public int highestScore;
        public bool isAlive;

        // Creates a new fighter with a unique ID and default name.
        public PlayerFighter()
        {
            fighterId = Guid.NewGuid().ToString();
            fighterName = string.IsNullOrEmpty(fighterName) ? "Unnamed Fighter" : fighterName;
            isAlive = true;
        }

        // Adds up attack, defense, and speed from all four equipment slots.
        // Null equipment counts as 0. Sets maxHealth and fills currentHealth to full.
        public void CalculateStats()
        {
            attack = (head?.attack ?? 0)
                   + (body?.attack ?? 0)
                   + (weapon?.attack ?? 0)
                   + (mount?.attack ?? 0);

            defense = (head?.defense ?? 0)
                    + (body?.defense ?? 0)
                    + (weapon?.defense ?? 0)
                    + (mount?.defense ?? 0);

            speed = (head?.speed ?? 0)
                  + (body?.speed ?? 0)
                  + (weapon?.speed ?? 0)
                  + (mount?.speed ?? 0);

            maxHealth = 100 + defense * 10;
            currentHealth = maxHealth;
        }

        // Restores currentHealth back to full maxHealth.
        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }

        // Adds points to currentScore and updates highestScore if the new total is higher.
        public void AddScore(int points)
        {
            currentScore += points;

            if (currentScore > highestScore)
            {
                highestScore = currentScore;
            }
        }

        // Reduces currentHealth by damage, clamped between 0 and maxHealth.
        // Calls Die() when health reaches 0.
        public void TakeDamage(int damage)
        {
            currentHealth -= damage;

            if (currentHealth < 0)
            {
                currentHealth = 0;
            }

            if (currentHealth == 0)
            {
                Die();
            }
        }

        // Increases currentHealth by amount, clamped between 0 and maxHealth.
        public void Heal(int amount)
        {
            currentHealth += amount;

            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }

        // Marks the fighter as dead when health reaches 0.
        public void Die()
        {
            isAlive = false;
        }
    }
}
