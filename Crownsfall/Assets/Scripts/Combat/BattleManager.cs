using Crownsfall.Characters;
using Crownsfall.Core;
using Crownsfall.UI;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Entry point for the Battle scene. Loads the player's fighter from GameSession,
    /// shows both fighters on UI FighterRigs, and fills the BattleHUD overlay.
    /// Does not run combat yet — that comes in a later step.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("Fighter Rigs (UI Canvas)")]
        [SerializeField] private FighterRig playerFighterRig;
        [SerializeField] private FighterRig enemyFighterRig;

        [Header("HUD (Canvas overlay)")]
        [SerializeField] private BattleHUD battleHUD;

        [Header("Testing (used when no session data)")]
        [Tooltip("Fighter name used when playing Battle scene directly without Character Builder.")]
        [SerializeField] private string testFighterName = "Test Fighter";

        [SerializeField] private HeadSO testHead;
        [SerializeField] private BodySO testBody;
        [SerializeField] private WeaponSO testWeapon;
        [SerializeField] private MountSO testMount;

        [Header("Enemy Placeholder")]
        [Tooltip("Tint applied to enemy UI layers (e.g. red Training Dummy). Stronger red = easier to spot on mobile.")]
        [SerializeField] private Color enemyTint = new Color(1f, 0.32f, 0.32f, 1f);

        [Tooltip("When true, enemy shows the same equipment sprites as the player (still tinted). On by default so the dummy is visible.")]
        [SerializeField] private bool enemyMirrorPlayerAppearance = true;

        private PlayerFighter _playerFighter;
        private PlayerFighter _enemyFighter;

        /// <summary>
        /// Runs when the battle scene starts. Builds fighters, displays rigs, and fills HUD.
        /// </summary>
        private void Start()
        {
            FindSceneReferencesIfNeeded();

            _playerFighter = BuildPlayerFighter();
            _enemyFighter = CreatePlaceholderEnemy();

            DisplayFighters();
            PopulateBattleUI();
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
        /// Finds fighter rigs and HUD by name when Inspector references are missing.
        /// Helpful after running Tools → Fighter Tools → Setup Battle Scene Production UI.
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

            if (battleHUD == null)
            {
                battleHUD = FindObjectOfType<BattleHUD>();
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
        /// Creates a Training Dummy with fixed stats (no equipment).
        /// Stats are set manually because CalculateStats only works from gear.
        /// </summary>
        private static PlayerFighter CreatePlaceholderEnemy()
        {
            return new PlayerFighter
            {
                fighterName = "Training Dummy",
                attack = 5,
                defense = 2,
                speed = 1,
                maxHealth = 100,
                currentHealth = 100
            };
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
        /// Sends each fighter to its FighterRig and sets facing direction.
        /// </summary>
        private void DisplayFighters()
        {
            if (playerFighterRig != null)
            {
                playerFighterRig.DisplayFighter(_playerFighter);
                playerFighterRig.SetFacing(true);
                playerFighterRig.SetColorTint(Color.white);
            }

            if (enemyFighterRig != null)
            {
                // Dummy has no gear by default; optional mirror uses player sprites for a quick visual test.
                var enemyDisplay = enemyMirrorPlayerAppearance
                    ? BuildEnemyDisplayFighter(_playerFighter)
                    : _enemyFighter;

                enemyFighterRig.DisplayFighter(enemyDisplay);
                enemyFighterRig.SetFacing(false);
                enemyFighterRig.SetColorTint(enemyTint);
            }
        }

        /// <summary>
        /// Keeps Training Dummy stats but borrows player equipment icons when mirroring is enabled.
        /// </summary>
        private static PlayerFighter BuildEnemyDisplayFighter(PlayerFighter player)
        {
            var display = new PlayerFighter
            {
                fighterName = "Training Dummy",
                head = player?.head,
                body = player?.body,
                weapon = player?.weapon,
                mount = player?.mount,
                attack = 5,
                defense = 2,
                speed = 1,
                maxHealth = 100,
                currentHealth = 100
            };
            return display;
        }

        /// <summary>
        /// Fills stat blocks, health bars, and the first battle log line.
        /// </summary>
        private void PopulateBattleUI()
        {
            if (battleHUD == null)
            {
                return;
            }

            battleHUD.InitializeForBattle();
            battleHUD.SetPlayerStats(_playerFighter);
            battleHUD.SetEnemyStats(_enemyFighter);
            battleHUD.SetPlayerHealth(_playerFighter.currentHealth, _playerFighter.maxHealth);
            battleHUD.SetEnemyHealth(_enemyFighter.currentHealth, _enemyFighter.maxHealth);
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
