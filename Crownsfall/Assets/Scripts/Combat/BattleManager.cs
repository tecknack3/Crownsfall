using System.Collections;

using System.Collections.Generic;

using Crownsfall.Characters;

using Crownsfall.Core;

using Crownsfall.Services;

using Crownsfall.UI;

using UnityEngine;



namespace Crownsfall.Combat

{

    /// <summary>

    /// Entry point for the Battle scene. Loads the player's fighter from GameSession,

    /// shows both fighters on UI FighterRigs, fills the BattleHUD overlay, and runs

    /// endless auto-combat waves until the player is defeated.

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



        [Header("Enemy Factory — Equipment Lists")]

        [Tooltip("Enemy gear pools. EnemyFactory picks one item per slot using waveNumber % list.Count.")]

        [SerializeField] private List<HeadSO> enemyHeads = new List<HeadSO>();

        [SerializeField] private List<BodySO> enemyBodies = new List<BodySO>();

        [SerializeField] private List<WeaponSO> enemyWeapons = new List<WeaponSO>();

        [SerializeField] private List<MountSO> enemyMounts = new List<MountSO>();



        [Header("Enemy Display")]

        [Tooltip("Tint applied to enemy UI layers. Stronger red = easier to spot on mobile.")]

        [SerializeField] private Color enemyTint = new Color(1f, 0.32f, 0.32f, 1f);



        [Header("Combat Timing")]

        [Tooltip("Seconds to wait after scene load before the first attack.")]

        [SerializeField] private float battleStartDelay = 1f;



        [Tooltip("Seconds between each attack in the auto-combat loop.")]

        [SerializeField] private float attackInterval = 0.8f;



        [Tooltip("Seconds to wait after an enemy dies before the next wave spawns.")]

        [SerializeField] private float waveTransitionDelay = 1.2f;



        [Header("Debug (Editor Testing Only)")]

        [Tooltip("Editor testing only for boss waves. Should be disabled before release.")]

        public bool useDebugStartWave = false;

        [Tooltip("Starting wave when useDebugStartWave is enabled. Should be disabled before release.")]

        public int debugStartWave = 1;

        // Unity testing only — disable before release.

        [Tooltip("Override player current HP after CalculateStats. Unity testing only; disable before release.")]

        public bool useDebugPlayerHealth = false;

        // Unity testing only — disable before release.

        [Tooltip("Current HP to apply when useDebugPlayerHealth is enabled. Unity testing only; disable before release.")]

        public int debugPlayerHealth = 10;



        private PlayerFighter _playerFighter;

        private EnemyFighter _enemyFighter;

        private EnemyFactory _enemyFactory;



        /// <summary>

        /// Current wave number. Set in Start (debug or wave 1), then increases when an enemy is defeated.

        /// </summary>

        private int _waveNumber;



        /// <summary>

        /// Progression data for the current wave (stats, boss flag, score reward).

        /// </summary>

        private ProgressionProfile _currentProgressionProfile;



        /// <summary>

        /// When false, the combat coroutine stops taking new turns.

        /// </summary>

        private bool _combatRunning;



        /// <summary>

        /// How many wave enemies the player has defeated this run (each kill +1).

        /// </summary>

        private int _enemiesDefeated;



        /// <summary>

        /// Highest wave number reached this run (updates when a wave is cleared or on death).

        /// </summary>

        private int _highestWaveReached;



        /// <summary>

        /// Runs when the battle scene starts. Builds fighters, displays rigs, fills HUD,

        /// then schedules auto-combat after a short delay.

        /// </summary>

        private void Start()

        {

            FindSceneReferencesIfNeeded();



            _enemyFactory = new EnemyFactory(enemyHeads, enemyBodies, enemyWeapons, enemyMounts);



            _playerFighter = BuildPlayerFighter();

            ApplyDebugPlayerHealthOverride();

            InitializeWaveNumber();

            _enemiesDefeated = 0;

            SpawnEnemyForWave(_waveNumber);



            DisplayFighters();

            PopulateBattleUI();

            LogBattleStart();
            LogPlayerEquippedSkills();



            StartCoroutine(BeginCombatAfterDelay());

        }



        /// <summary>

        /// Returns the player fighter built for this battle (read-only for other scripts later).

        /// </summary>

        public PlayerFighter PlayerFighter => _playerFighter;



        /// <summary>

        /// Returns the current wave enemy fighter (read-only for other scripts later).

        /// </summary>

        public EnemyFighter EnemyFighter => _enemyFighter;



        /// <summary>

        /// Waits one second, logs that combat is starting, refreshes HUD bars/score, then runs the loop.

        /// </summary>

        private IEnumerator BeginCombatAfterDelay()

        {

            yield return new WaitForSeconds(battleStartDelay);



            LogCombatMessage("Combat starts!");



            // Refresh health bars and score right before the first swing.

            RefreshCombatHud();



            StartCoroutine(StartBattleLoop());

        }



        /// <summary>

        /// Endless auto-combat: player attacks, pause, enemy attacks (if alive), pause, repeat.

        /// When an enemy dies, score increases and the next wave spawns after a short delay.

        /// Stops only when the player reaches 0 HP.

        /// </summary>

        private IEnumerator StartBattleLoop()

        {

            _combatRunning = true;



            // Outer loop: keep spawning waves until the player is defeated.

            while (_combatRunning && _playerFighter.isAlive)

            {

                // Inner loop: trade blows with the current wave's enemy.

                while (_combatRunning && _playerFighter.isAlive && _enemyFighter.isAlive)

                {

                    // --- Player turn ---

                    ApplyPlayerAttackDamage();



                    if (!_enemyFighter.isAlive)

                    {

                        // Award score, wait, spawn the next enemy, then continue fighting.

                        yield return HandleEnemyDefeatedAndAdvanceWave();

                        break;

                    }



                    yield return new WaitForSeconds(attackInterval);



                    if (!_combatRunning || !_enemyFighter.isAlive)

                    {

                        break;

                    }



                    // --- Enemy turn (only if still alive) ---

                    var enemyDamage = CalculateDamage(_enemyFighter.attack, _playerFighter.defense);

                    _playerFighter.TakeDamage(enemyDamage);



                    UpdatePlayerHealthDisplay();

                    LogCombatMessage($"Enemy dealt {enemyDamage} damage!");



                    if (!_playerFighter.isAlive)

                    {

                        HandlePlayerDefeated();

                        yield break;

                    }



                    yield return new WaitForSeconds(attackInterval);

                }

            }

        }



        /// <summary>

        /// Unity testing only — disable before release.

        /// After BuildPlayerFighter/CloneFighter CalculateStats, optionally overrides current HP.

        /// </summary>

        private void ApplyDebugPlayerHealthOverride()

        {

            if (!useDebugPlayerHealth || _playerFighter == null)

            {

                return;

            }



            _playerFighter.currentHealth = debugPlayerHealth;



            if (battleHUD != null)

            {

                battleHUD.SetPlayerHealth(_playerFighter.currentHealth, _playerFighter.maxHealth);

            }



            Debug.Log($"Debug player health enabled: {debugPlayerHealth}");

        }



        /// <summary>

        /// Sets _waveNumber from debug settings or defaults to wave 1.

        /// </summary>

        private void InitializeWaveNumber()

        {

            _waveNumber = useDebugStartWave ? Mathf.Max(1, debugStartWave) : 1;

            _highestWaveReached = _waveNumber;

            Debug.Log($"Starting battle at wave: {_waveNumber}");

        }



        /// <summary>

        /// Spawns a new wave enemy via EnemyFactory (stats, name tier, and equipment).

        /// </summary>

        private void SpawnEnemyForWave(int wave)

        {

            _currentProgressionProfile = ProgressionEngine.GenerateProgression(wave);

            _enemyFighter = _enemyFactory.GenerateEnemy(wave);

        }



        /// <summary>

        /// Called when the current wave enemy reaches 0 HP.

        /// Adds score, waits, spawns the next wave, and updates HUD + enemy rig.

        /// Combat keeps running — only player death stops the loop.

        /// </summary>

        private IEnumerator HandleEnemyDefeatedAndAdvanceWave()

        {

            var rewardMessage = _enemyFighter != null && !string.IsNullOrEmpty(_enemyFighter.rewardText)

                ? _enemyFighter.rewardText

                : _currentProgressionProfile?.rewardText ?? "+1 Score";

            var scoreReward = _enemyFighter?.scoreReward ?? _currentProgressionProfile?.scoreReward ?? 1;



            // One more enemy down — track it for the game over screen.

            _enemiesDefeated++;



            LogCombatMessage(rewardMessage);



            _playerFighter.AddScore(scoreReward);



            if (battleHUD != null)

            {

                battleHUD.SetScore(_playerFighter.currentScore);

            }



            yield return new WaitForSeconds(waveTransitionDelay);



            _waveNumber++;

            _highestWaveReached = Mathf.Max(_highestWaveReached, _waveNumber);

            SpawnEnemyForWave(_waveNumber);



            if (battleHUD != null)

            {

                battleHUD.SetWave(_waveNumber, _currentProgressionProfile.isBossWave);

            }



            UpdateEnemyHealthDisplay();

            DisplayEnemyFighter();



            LogCombatMessage($"Wave {_waveNumber} begins!");

        }



        /// <summary>

        /// Called when the player reaches 0 HP. Stops combat and shows the final score.

        /// </summary>

        private void HandlePlayerDefeated()

        {

            _combatRunning = false;

            _highestWaveReached = Mathf.Max(_highestWaveReached, _waveNumber);



            var finalScore = _playerFighter.currentScore;

            // Wave the player was fighting when they died (not waves cleared after death).
            var waveReached = _waveNumber;



            LogCombatMessage($"Fighter defeated! Final Score: {finalScore}");



            LocalSaveService.SaveRunResult(_playerFighter, finalScore, waveReached);

            var saveData = LocalSaveService.LoadSaveData();



            if (battleHUD != null)

            {

                battleHUD.ShowGameOver(

                    _playerFighter.fighterName,

                    finalScore,

                    waveReached,

                    _enemiesDefeated,

                    _highestWaveReached,

                    saveData.bestScore,

                    saveData.bestFighterName,

                    saveData.highestWave);

            }

        }



        /// <summary>

        /// Basic damage formula: attack minus defense, with a minimum of 1.

        /// </summary>

        private static int CalculateDamage(int attack, int defense)

        {

            return Mathf.Max(1, attack - defense);

        }



        /// <summary>

        /// Player attack: base damage, optional Critical Strike from weapon skill, then apply to enemy.

        /// </summary>

        private void ApplyPlayerAttackDamage()

        {

            var damage = CalculateDamage(_playerFighter.attack, _enemyFighter.defense);

            TryApplyCriticalStrike(ref damage, out var wasCritical);



            _enemyFighter.TakeDamage(damage);

            UpdateEnemyHealthDisplay();



            if (wasCritical)

            {

                LogCombatMessage("CRITICAL HIT!");

                LogCombatMessage($"Player dealt {damage} damage!");

            }

            else

            {

                LogCombatMessage($"Player dealt {damage} damage.");

            }

        }



        /// <summary>

        /// If the player's weapon has Critical Strike, roll for a crit and multiply damage on success.

        /// v1 only handles Critical Strike — other skill types are ignored for now.

        /// </summary>

        private void TryApplyCriticalStrike(ref int damage, out bool wasCritical)

        {

            wasCritical = false;



            // Skill lives on the weapon ScriptableObject (EquipmentItemSO.skill).

            var skill = _playerFighter.weapon?.skill;

            if (skill == null || skill.skillType != SkillType.CriticalStrike)

            {

                return;

            }



            // Random.value is 0–1. chance is stored as a percent (e.g. 25 = 25% crit chance).

            var roll = Random.value;

            if (roll > skill.chance / 100f)

            {

                return;

            }



            // Crit landed — value is a multiplier (e.g. 2.0 = double damage).

            damage = Mathf.RoundToInt(damage * skill.value);

            wasCritical = true;

        }



        /// <summary>

        /// Syncs player health bar and stat block with current fighter values.

        /// </summary>

        private void UpdatePlayerHealthDisplay()

        {

            if (battleHUD == null)

            {

                return;

            }



            battleHUD.SetPlayerHealth(_playerFighter.currentHealth, _playerFighter.maxHealth);

            battleHUD.SetPlayerStats(_playerFighter);

        }



        /// <summary>

        /// Syncs enemy health bar and stat block with current fighter values.

        /// </summary>

        private void UpdateEnemyHealthDisplay()

        {

            if (battleHUD == null)

            {

                return;

            }



            battleHUD.SetEnemyHealth(_enemyFighter.currentHealth, _enemyFighter.maxHealth);

            battleHUD.SetEnemyStats(_enemyFighter);

        }



        /// <summary>

        /// Refreshes health bars and score at combat start (after the initial delay).

        /// </summary>

        private void RefreshCombatHud()

        {

            if (battleHUD == null)

            {

                return;

            }



            battleHUD.SetPlayerHealth(_playerFighter.currentHealth, _playerFighter.maxHealth);

            battleHUD.SetEnemyHealth(_enemyFighter.currentHealth, _enemyFighter.maxHealth);

            battleHUD.SetScore(_playerFighter.currentScore);

        }



        /// <summary>

        /// Writes a line to the battle log HUD and mirrors it to the Unity Console.

        /// </summary>

        private void LogCombatMessage(string message)

        {

            Debug.Log(message);



            if (battleHUD != null)

            {

                battleHUD.AddLogLine(message);

            }

        }



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

                playerFighterRig.Display(_playerFighter);

                playerFighterRig.SetFacing(true);

                playerFighterRig.SetColorTint(Color.white);

            }



            DisplayEnemyFighter();

        }



        /// <summary>

        /// Refreshes only the enemy FighterRig (called after each new wave spawns).

        /// </summary>

        private void DisplayEnemyFighter()

        {

            if (enemyFighterRig == null || _enemyFighter == null)

            {

                return;

            }



            enemyFighterRig.DisplayEnemy(_enemyFighter);

            enemyFighterRig.SetFacing(false);

            enemyFighterRig.SetColorTint(enemyTint);

        }



        /// <summary>

        /// Fills stat blocks, health bars, wave/score, and the first battle log line.

        /// </summary>

        private void PopulateBattleUI()

        {

            if (battleHUD == null)

            {

                return;

            }



            // Reset score/log only — InitializeForBattle hardcodes wave 1; use _waveNumber instead.

            battleHUD.HideGameOver();

            battleHUD.SetScore(0);

            battleHUD.ClearLog();

            battleHUD.AddLogLine("Battle begins!");

            battleHUD.SetPlayerStats(_playerFighter);

            battleHUD.SetEnemyStats(_enemyFighter);

            battleHUD.SetPlayerHealth(_playerFighter.currentHealth, _playerFighter.maxHealth);

            battleHUD.SetEnemyHealth(_enemyFighter.currentHealth, _enemyFighter.maxHealth);

            battleHUD.SetWave(_waveNumber, _currentProgressionProfile != null && _currentProgressionProfile.isBossWave);

        }



        /// <summary>

        /// Logs a friendly message so you can confirm the scene loaded correctly in the Console.

        /// </summary>

        private void LogBattleStart()

        {

            Debug.Log(

                $"Battle started! Wave {_waveNumber} — Player: {_playerFighter.fighterName} " +

                $"(ATK {_playerFighter.attack}, DEF {_playerFighter.defense}, " +

                $"SPD {_playerFighter.speed}, HP {_playerFighter.maxHealth}) " +

                $"vs Enemy: {_enemyFighter.enemyName} " +

                $"(ATK {_enemyFighter.attack}, DEF {_enemyFighter.defense}, " +

                $"SPD {_enemyFighter.speed}, HP {_enemyFighter.maxHealth})");

        }



        /// <summary>

        /// Logs equipped skill names at battle start for debugging.

        /// v1 applies Critical Strike from the weapon; other skills are data-only for now.

        /// </summary>

        private void LogPlayerEquippedSkills()

        {

            if (_playerFighter == null)

            {

                return;

            }



            Debug.Log(

                "Player Skills\n" +

                FormatEquippedSkillLine("Head", _playerFighter.head) + "\n" +

                FormatEquippedSkillLine("Body", _playerFighter.body) + "\n" +

                FormatEquippedSkillLine("Weapon", _playerFighter.weapon) + "\n" +

                FormatEquippedSkillLine("Mount", _playerFighter.mount));

        }



        private static string FormatEquippedSkillLine(string slotLabel, EquipmentItemSO item)

        {

            var skillName = item != null ? item.GetSkillDisplayName() : "None";

            return $"{slotLabel}: {skillName}";

        }

    }

}


