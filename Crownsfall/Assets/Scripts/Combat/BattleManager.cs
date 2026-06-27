using Crownsfall.Characters;
using Crownsfall.Core;
using Crownsfall.UI;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Entry point for the Battle scene. Loads the player's fighter, shows both fighters
    /// on screen, and logs battle-start info. Does not run combat yet — that comes later.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("Fighter Views")]
        [SerializeField] private BattleFighterView playerFighterView;
        [SerializeField] private BattleFighterView enemyFighterView;

        [Header("Fighter Layout (mobile portrait)")]
        [Tooltip("World position for the player fighter. Tweaked so sprites stay inside the screen.")]
        public Vector3 playerPosition = new Vector3(-0.85f, -1.15f, 0f);

        [Tooltip("World position for the enemy fighter.")]
        public Vector3 enemyPosition = new Vector3(0.85f, -1.15f, 0f);

        [Tooltip("Uniform scale for both fighters. Smaller values fit better on phone screens.")]
        public Vector3 fighterScale = new Vector3(0.34f, 0.34f, 0.34f);

        [Header("Camera")]
        [Tooltip("World position applied to Main Camera at battle start.")]
        public Vector3 cameraPosition = new Vector3(0f, 0f, -10f);

        [Tooltip("Orthographic size for Main Camera at battle start.")]
        public float cameraOrthographicSize = 3.5f;

        [Header("Testing (used when no session data)")]
        [Tooltip("Fighter name used when playing Battle scene directly without Character Builder.")]
        [SerializeField] private string testFighterName = "Test Fighter";

        [SerializeField] private HeadSO testHead;
        [SerializeField] private BodySO testBody;
        [SerializeField] private WeaponSO testWeapon;
        [SerializeField] private MountSO testMount;

        [Header("Enemy Placeholder")]
        [SerializeField] private Color enemyTint = new Color(1f, 0.45f, 0.45f, 1f);

        private PlayerFighter _playerFighter;
        private PlayerFighter _enemyFighter;

        /// <summary>
        /// Runs when the battle scene starts. Builds fighters and displays them.
        /// </summary>
        private void Start()
        {
            ConfigureMainCamera();
            FindFighterViewsIfNeeded();
            PositionFighters();

            _playerFighter = BuildPlayerFighter();
            _enemyFighter = CreatePlaceholderEnemy();

            DisplayFighters();
            LogBattleStart();
        }

        /// <summary>
        /// Returns the player fighter built for this battle (read-only for other scripts later).
        /// </summary>
        public PlayerFighter PlayerFighter => _playerFighter;

        /// <summary>
        /// Returns the placeholder enemy fighter (read-only for other scripts later).
        /// </summary>
        public PlayerFighter EnemyFighter => _enemyFighter;

        /// <summary>
        /// Looks up fighter views by GameObject name when Inspector references are missing.
        /// Helpful after running Tools → Fighter Tools → Setup Battle Scene.
        /// </summary>
        private void FindFighterViewsIfNeeded()
        {
            if (playerFighterView == null)
            {
                var playerObject = GameObject.Find("PlayerFighterView");
                if (playerObject != null)
                {
                    playerFighterView = playerObject.GetComponent<BattleFighterView>();
                }
            }

            if (enemyFighterView == null)
            {
                var enemyObject = GameObject.Find("EnemyFighterView");
                if (enemyObject != null)
                {
                    enemyFighterView = enemyObject.GetComponent<BattleFighterView>();
                }
            }
        }

        /// <summary>
        /// Configures Main Camera for 2D battle view (orthographic, size 3.5, centered at origin).
        /// </summary>
        private void ConfigureMainCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = cameraOrthographicSize;
            mainCamera.transform.position = cameraPosition;
        }

        /// <summary>
        /// Moves and scales both fighters so they fit a portrait orthographic camera.
        /// Main Camera orthographic size 3.5 works well with these defaults; the setup tool matches.
        /// </summary>
        private void PositionFighters()
        {
            if (playerFighterView != null)
            {
                playerFighterView.transform.position = playerPosition;
                playerFighterView.transform.localScale = fighterScale;
            }

            if (enemyFighterView != null)
            {
                enemyFighterView.transform.position = enemyPosition;
                enemyFighterView.transform.localScale = fighterScale;
            }
        }

        /// <summary>
        /// Builds the player fighter from GameSession, or falls back to test values.
        /// Priority: GameSession.CurrentFighter → FighterSessionData → Inspector test equipment.
        /// </summary>
        private PlayerFighter BuildPlayerFighter()
        {
            var sessionFighter = GameSession.Instance.CurrentFighter;
            if (sessionFighter != null)
            {
                return CloneFighter(sessionFighter);
            }

            // Secondary fallback for older static session data (if any).
            if (FighterSessionData.CurrentFighter != null)
            {
                return CloneFighter(FighterSessionData.CurrentFighter);
            }

            if (FighterSessionData.HasSessionData())
            {
                var fighter = new PlayerFighter
                {
                    fighterName = string.IsNullOrEmpty(FighterSessionData.FighterName)
                        ? "Unnamed Fighter"
                        : FighterSessionData.FighterName,
                    head = FighterSessionData.SelectedHead,
                    body = FighterSessionData.SelectedBody,
                    weapon = FighterSessionData.SelectedWeapon,
                    mount = FighterSessionData.SelectedMount
                };
                fighter.CalculateStats();
                return fighter;
            }

            return BuildTestFighter();
        }

        /// <summary>
        /// Creates a simple Training Dummy with no equipment for early battle-scene testing.
        /// CalculateStats with null gear gives attack 0, defense 0, speed 0, maxHealth 100.
        /// </summary>
        private static PlayerFighter CreatePlaceholderEnemy()
        {
            var enemy = new PlayerFighter
            {
                fighterName = "Training Dummy"
            };
            enemy.CalculateStats();
            return enemy;
        }

        /// <summary>
        /// Builds a fighter from the test fields wired in the Inspector.
        /// </summary>
        private PlayerFighter BuildTestFighter()
        {
            var fighter = new PlayerFighter
            {
                fighterName = string.IsNullOrEmpty(testFighterName) ? "Test Fighter" : testFighterName,
                head = testHead,
                body = testBody,
                weapon = testWeapon,
                mount = testMount
            };
            fighter.CalculateStats();
            return fighter;
        }

        /// <summary>
        /// Copies a fighter so the battle scene does not mutate the session original.
        /// </summary>
        private static PlayerFighter CloneFighter(PlayerFighter source)
        {
            var clone = new PlayerFighter
            {
                fighterName = source.fighterName,
                head = source.head,
                body = source.body,
                weapon = source.weapon,
                mount = source.mount
            };
            clone.CalculateStats();
            return clone;
        }

        /// <summary>
        /// Sends each fighter to its BattleFighterView on screen.
        /// </summary>
        private void DisplayFighters()
        {
            if (playerFighterView != null)
            {
                playerFighterView.DisplayFighter(_playerFighter);
                playerFighterView.SetColorTint(Color.white);
            }

            if (enemyFighterView != null)
            {
                enemyFighterView.DisplayFighter(_enemyFighter);
                enemyFighterView.SetColorTint(enemyTint);
            }
        }

        /// <summary>
        /// Logs a friendly message so you can confirm the scene loaded correctly in the Console.
        /// </summary>
        private void LogBattleStart()
        {
            Debug.Log(
                $"Battle started! Player: {_playerFighter.fighterName} " +
                $"(ATK {_playerFighter.attack}, DEF {_playerFighter.defense}, " +
                $"SPD {_playerFighter.speed}, HP {_playerFighter.maxHealth}) " +
                $"vs Enemy: {_enemyFighter.fighterName} " +
                $"(ATK {_enemyFighter.attack}, DEF {_enemyFighter.defense}, " +
                $"SPD {_enemyFighter.speed}, HP {_enemyFighter.maxHealth})");
        }
    }
}
