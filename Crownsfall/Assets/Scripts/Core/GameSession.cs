using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Core
{
    /// <summary>
    /// Persists game data across scene loads (Character Builder → Battle, etc.).
    /// Lives on a DontDestroyOnLoad GameObject so it survives when scenes change.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        /// <summary>
        /// The fighter the player built and chose to take into battle.
        /// </summary>
        public PlayerFighter CurrentFighter;

        /// <summary>
        /// Singleton access. Finds an existing GameSession in the scene,
        /// or creates a new "GameSession" GameObject if none exists yet.
        /// </summary>
        public static GameSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameSession>();

                    if (_instance == null)
                    {
                        var go = new GameObject("GameSession");
                        _instance = go.AddComponent<GameSession>();
                    }
                }

                return _instance;
            }
            private set => _instance = value;
        }

        private static GameSession _instance;

        private void Awake()
        {
            // Only one GameSession should exist. Destroy duplicates.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Saves the fighter that will be used in the next battle scene.
        /// Calls CalculateStats so attack, defense, speed, and health are ready.
        /// </summary>
        public void SetCurrentFighter(PlayerFighter fighter)
        {
            CurrentFighter = fighter;

            if (fighter != null)
            {
                fighter.CalculateStats();
            }
        }
    }
}
