using System.Collections;

using System.Collections.Generic;

using Crownsfall.Characters;

using Crownsfall.Combat.Events;

using Crownsfall.Combat.Skills;

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
    ///
    /// Battle log UI: BattleManager raises CombatEventBus events only. CombatLogUIListener
    /// turns those events into HUD lines — do not call battleHUD.AddLogLine here for the
    /// same moments (BattleStarted, WaveStarted, skills, damage, defeat, score, etc.).
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



        [Header("Combat Balance")]

        [Tooltip("Wave scaling, score rewards, and skill template defaults. Create via Tools → Fighter Tools → Create Combat Balance Asset.")]

        [SerializeField] private CombatBalanceSO combatBalance;



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
        /// Runs equipped item skills (Critical Strike, Burn, etc.) during combat.
        /// </summary>
        private SkillEngine _skillEngine;

        /// <summary>
        /// Burn DoT state on the current wave enemy. Reset when a new enemy spawns.
        /// Ticks at the start of each enemy turn (before the enemy attacks).
        /// </summary>
        private bool _enemyIsBurning;
        private int _enemyBurnDamage;
        private int _enemyBurnTurnsRemaining;

        /// <summary>
        /// Poison DoT state on the current wave enemy. Reset when a new enemy spawns.
        /// Weaker per-tick than burn but often lasts longer. Ticks after burn each enemy turn.
        /// </summary>
        private bool _enemyIsPoisoned;
        private int _enemyPoisonDamage;
        private int _enemyPoisonTurnsRemaining;
        private EquipmentSkill _enemyPoisonSourceSkill;



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

            if (combatBalance != null)
            {
                CombatBalance.SetActive(combatBalance);
            }



            _enemyFactory = new EnemyFactory(enemyHeads, enemyBodies, enemyWeapons, enemyMounts);

            _skillEngine = new SkillEngine();

            _playerFighter = BuildPlayerFighter();

            ApplyDebugPlayerHealthOverride();

            InitializeWaveNumber();

            _enemiesDefeated = 0;

            SpawnEnemyForWave(_waveNumber);

            NotifyWaveStarted();



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



            // CombatLogUIListener shows "Battle begins!" from this event — no duplicate HUD log here.
            RaiseCombatEvent(
                CombatEventType.BattleStarted,
                "Battle started!",
                sourceName: _playerFighter.fighterName,
                targetName: _enemyFighter.enemyName);



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



                    // --- Enemy turn: DoT ticks first (burn, then poison), then enemy attacks if still alive ---

                    ProcessEnemyBurnAtTurnStart();
                    ProcessEnemyPoisonAtTurnStart();



                    if (!_enemyFighter.isAlive)

                    {

                        yield return HandleEnemyDefeatedAndAdvanceWave();

                        break;

                    }



                    ApplyEnemyAttackDamage();



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

            ResetEnemyBurnState();
            ResetEnemyPoisonState();

        }



        /// <summary>

        /// Called when the current wave enemy reaches 0 HP.

        /// Adds score, waits, spawns the next wave, and updates HUD + enemy rig.

        /// Combat keeps running — only player death stops the loop.

        /// </summary>

        private IEnumerator HandleEnemyDefeatedAndAdvanceWave()

        {

            var balance = CombatBalance.Active;

            var rewardMessage = _enemyFighter != null && !string.IsNullOrEmpty(_enemyFighter.rewardText)

                ? _enemyFighter.rewardText

                : _currentProgressionProfile?.rewardText ?? balance.scoreRewardText;

            var scoreReward = _enemyFighter?.scoreReward ?? _currentProgressionProfile?.scoreReward ?? balance.scorePerWave;



            // One more enemy down — track it for the game over screen.

            _enemiesDefeated++;

            RaiseCombatEvent(
                CombatEventType.EnemyDefeated,
                $"{_enemyFighter.enemyName} defeated!",
                sourceName: _playerFighter.fighterName,
                targetName: _enemyFighter.enemyName);



            // ScoreChanged event carries rewardMessage — CombatLogUIListener shows it in the HUD.
            _playerFighter.AddScore(scoreReward);

            RaiseCombatEvent(
                CombatEventType.ScoreChanged,
                rewardMessage,
                amount: scoreReward,
                score: _playerFighter.currentScore);



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



            // WaveStarted / BossStarted events (via NotifyWaveStarted) drive the battle log UI.
            NotifyWaveStarted();

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



            // PlayerDefeated + RunEnded events — CombatLogUIListener formats the HUD lines.
            RaiseCombatEvent(
                CombatEventType.PlayerDefeated,
                $"Fighter defeated! Final Score: {finalScore}",
                sourceName: _enemyFighter?.enemyName,
                targetName: _playerFighter.fighterName,
                score: finalScore);

            RaiseCombatEvent(
                CombatEventType.RunEnded,
                "Run ended.",
                sourceName: _playerFighter.fighterName,
                score: finalScore);



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

        /// Player attack: base damage, weapon skill via SkillEngine, optional mount skill (poison etc.),

        /// then apply to enemy.

        /// </summary>

        private void ApplyPlayerAttackDamage()

        {

            RaiseCombatEvent(
                CombatEventType.PlayerAttack,
                "Player attacks.",
                sourceName: _playerFighter.fighterName,
                targetName: _enemyFighter.enemyName);



            var baseDamage = CalculateDamage(_playerFighter.attack, _enemyFighter.defense);

            var context = CreateBattleContext();

            var weaponSkill = _playerFighter.weapon?.skill;

            var weaponResult = _skillEngine.ApplyPlayerAttackSkills(baseDamage, weaponSkill, context);

            var anySkillTriggered = ApplyPlayerAttackSkillEffects(weaponResult, weaponSkill);

            var finalDamage = weaponResult.modifiedDamage;

            SkillResult mountResult = null;

            // Poison and Life Steal can live on weapon OR mount — check mount even when weapon already ran.

            var mountSkill = _playerFighter.mount?.skill;

            if (mountSkill != null && mountSkill.skillType != SkillType.None)

            {

                mountResult = _skillEngine.ApplyPlayerAttackSkills(finalDamage, mountSkill, context);

                if (ApplyPlayerAttackSkillEffects(mountResult, mountSkill))

                {

                    anySkillTriggered = true;

                }



                finalDamage = mountResult.modifiedDamage;

            }



            _enemyFighter.TakeDamage(finalDamage);

            UpdateEnemyHealthDisplay();

            var damageSuffix = anySkillTriggered ? "!" : ".";

            RaiseCombatEvent(
                CombatEventType.DamageDealt,
                $"Player dealt {finalDamage} damage{damageSuffix}",
                sourceName: _playerFighter.fighterName,
                targetName: _enemyFighter.enemyName,
                amount: finalDamage);

            // Life Steal heals based on final damage actually dealt (after crit etc.).
            ApplyLifeStealFromResult(weaponResult, weaponSkill, finalDamage);

            if (mountResult != null)

            {

                ApplyLifeStealFromResult(mountResult, mountSkill, finalDamage);

            }

        }



        /// <summary>

        /// Raises skill events and applies DoT flags from one player-attack skill result.

        /// Returns true if a visible skill message was shown.

        /// </summary>

        private bool ApplyPlayerAttackSkillEffects(SkillResult result, EquipmentSkill skill)

        {

            var skillTriggered = result.wasTriggered && !string.IsNullOrEmpty(result.message);



            if (skillTriggered)

            {

                RaiseCombatEvent(
                    CombatEventType.SkillTriggered,
                    result.message,
                    sourceName: GetSkillDisplayName(skill),
                    targetName: _enemyFighter.enemyName,
                    amount: result.modifiedDamage);

            }



            if (result.applyBurn)

            {

                ApplyBurnToEnemy(result.burnDamage, result.burnDuration);

            }



            if (result.applyPoison)

            {

                ApplyPoisonToEnemy(result.poisonDamage, result.poisonDuration, skill);

            }



            return skillTriggered;

        }

        /// <summary>
        /// After player damage is applied, heals the player if Life Steal triggered on this attack.
        /// Uses final damage dealt so crits and other modifiers are included in the heal amount.
        /// </summary>
        private void ApplyLifeStealFromResult(SkillResult result, EquipmentSkill skill, int damageDealt)
        {
            if (result == null || !result.applyLifeSteal || result.lifeStealPercent <= 0f)
            {
                return;
            }

            var healAmount = Mathf.RoundToInt(damageDealt * result.lifeStealPercent);
            if (healAmount <= 0)
            {
                return;
            }

            _playerFighter.Heal(healAmount);
            UpdatePlayerHealthDisplay();

            RaiseCombatEvent(
                CombatEventType.HealingReceived,
                $"Player healed {healAmount} HP!",
                sourceName: GetSkillDisplayName(skill),
                targetName: _playerFighter.fighterName,
                amount: healAmount);
        }


        /// <summary>
        /// Clears poison DoT when a new wave enemy spawns.
        /// </summary>
        private void ResetEnemyPoisonState()
        {
            _enemyIsPoisoned = false;
            _enemyPoisonDamage = 0;
            _enemyPoisonTurnsRemaining = 0;
            _enemyPoisonSourceSkill = null;
        }

        /// <summary>
        /// Starts or refreshes poison on the current enemy (weapon or mount poison skill).
        /// </summary>
        private void ApplyPoisonToEnemy(int damage, int turnsRemaining, EquipmentSkill sourceSkill)
        {
            _enemyIsPoisoned = true;
            _enemyPoisonDamage = damage;
            _enemyPoisonTurnsRemaining = turnsRemaining;
            _enemyPoisonSourceSkill = sourceSkill;
        }

        /// <summary>
        /// DoT tick at the start of the enemy turn — runs after burn each enemy turn.
        /// Deals poison damage, logs it, then counts down duration. When turns hit 0, poison ends.
        /// </summary>
        private void ProcessEnemyPoisonAtTurnStart()
        {
            if (!_enemyIsPoisoned || _enemyPoisonTurnsRemaining <= 0)
            {
                return;
            }

            _enemyFighter.TakeDamage(_enemyPoisonDamage);
            UpdateEnemyHealthDisplay();

            RaiseCombatEvent(
                CombatEventType.DamageDealt,
                $"Poison dealt {_enemyPoisonDamage} damage!",
                sourceName: GetSkillDisplayName(_enemyPoisonSourceSkill),
                targetName: _enemyFighter.enemyName,
                amount: _enemyPoisonDamage);

            _enemyPoisonTurnsRemaining--;

            if (_enemyPoisonTurnsRemaining <= 0)
            {
                _enemyIsPoisoned = false;
                RaiseCombatEvent(
                    CombatEventType.SkillTriggered,
                    "Poison faded.",
                    sourceName: GetSkillDisplayName(_enemyPoisonSourceSkill),
                    targetName: _enemyFighter.enemyName);
            }
        }

        /// <summary>
        /// Clears burn DoT when a new wave enemy spawns.
        /// </summary>
        private void ResetEnemyBurnState()
        {
            _enemyIsBurning = false;
            _enemyBurnDamage = 0;
            _enemyBurnTurnsRemaining = 0;
        }

        /// <summary>
        /// Starts or refreshes burn on the current enemy (called when Burn skill triggers on player attack).
        /// </summary>
        private void ApplyBurnToEnemy(int damage, int turnsRemaining)
        {
            _enemyIsBurning = true;
            _enemyBurnDamage = damage;
            _enemyBurnTurnsRemaining = turnsRemaining;
        }

        /// <summary>
        /// DoT tick at the start of the enemy turn — before the enemy can attack.
        /// Deals burn damage, logs it, then counts down duration. When turns hit 0, burn ends.
        /// </summary>
        private void ProcessEnemyBurnAtTurnStart()
        {
            if (!_enemyIsBurning || _enemyBurnTurnsRemaining <= 0)
            {
                return;
            }

            _enemyFighter.TakeDamage(_enemyBurnDamage);
            UpdateEnemyHealthDisplay();

            RaiseCombatEvent(
                CombatEventType.DamageDealt,
                $"Burn dealt {_enemyBurnDamage} damage!",
                sourceName: GetSkillDisplayName(_playerFighter.weapon?.skill),
                targetName: _enemyFighter.enemyName,
                amount: _enemyBurnDamage);

            _enemyBurnTurnsRemaining--;

            if (_enemyBurnTurnsRemaining <= 0)
            {
                _enemyIsBurning = false;
                RaiseCombatEvent(
                    CombatEventType.SkillTriggered,
                    "Burn faded.",
                    sourceName: GetSkillDisplayName(_playerFighter.weapon?.skill),
                    targetName: _enemyFighter.enemyName);
            }
        }

        /// <summary>

        /// Enemy attack: base damage, body skill via SkillEngine, then apply to player.

        /// </summary>

        private void ApplyEnemyAttackDamage()

        {

            RaiseCombatEvent(
                CombatEventType.EnemyAttack,
                "Enemy attacks.",
                sourceName: _enemyFighter.enemyName,
                targetName: _playerFighter.fighterName);



            var baseDamage = CalculateDamage(_enemyFighter.attack, _playerFighter.defense);

            var context = CreateBattleContext();

            var skill = _playerFighter.body?.skill;

            var result = _skillEngine.ApplyEnemyAttackSkills(baseDamage, skill, context);



            if (result.wasTriggered && !string.IsNullOrEmpty(result.message))

            {

                RaiseCombatEvent(
                    CombatEventType.SkillTriggered,
                    result.message,
                    sourceName: GetSkillDisplayName(skill),
                    targetName: _playerFighter.fighterName,
                    amount: result.modifiedDamage);

            }



            _playerFighter.TakeDamage(result.modifiedDamage);

            UpdatePlayerHealthDisplay();

            var skillTriggered = result.wasTriggered && !string.IsNullOrEmpty(result.message);
            var damageSuffix = skillTriggered ? "!" : ".";
            RaiseCombatEvent(
                CombatEventType.DamageDealt,
                $"Enemy dealt {result.modifiedDamage} damage{damageSuffix}",
                sourceName: _enemyFighter.enemyName,
                targetName: _playerFighter.fighterName,
                amount: result.modifiedDamage);

        }



        /// <summary>

        /// Builds the context object skill effects use to read fighters and write log lines.

        /// </summary>

        private BattleContext CreateBattleContext()

        {

            return new BattleContext

            {

                player = _playerFighter,

                enemy = _enemyFighter,

                currentWave = _waveNumber,

                currentScore = _playerFighter.currentScore,

                // Skills may log debug lines here; HUD text comes from SkillTriggered / DamageDealt events.
                logMessage = message => Debug.Log(message)

            };

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

        /// Fills stat blocks, health bars, wave/score. Battle log lines come from CombatLogUIListener

        /// when CombatEventBus events fire — do not seed duplicate lines here.

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

        /// v1 applies Critical Strike and Burn from the weapon, Poison and Life Steal from weapon or mount;
        /// other skills are data-only for now.

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



        /// <summary>

        /// Raises a combat event on the shared bus with common battle fields filled in.

        /// </summary>

        private void RaiseCombatEvent(

            CombatEventType type,

            string message,

            string sourceName = null,

            string targetName = null,

            int amount = 0,

            int? waveNumber = null,

            int? score = null,

            bool? isBoss = null)

        {

            CombatEventBus.Raise(new CombatEvent

            {

                eventType = type,

                message = message,

                sourceName = sourceName ?? string.Empty,

                targetName = targetName ?? string.Empty,

                amount = amount,

                waveNumber = waveNumber ?? _waveNumber,

                score = score ?? (_playerFighter?.currentScore ?? 0),

                isBoss = isBoss ?? (_currentProgressionProfile?.isBossWave ?? false)

            });

        }



        /// <summary>

        /// Fires WaveStarted and BossStarted (when applicable) after a wave enemy is spawned.

        /// </summary>

        private void NotifyWaveStarted()

        {

            var isBoss = _currentProgressionProfile != null && _currentProgressionProfile.isBossWave;



            RaiseCombatEvent(

                CombatEventType.WaveStarted,

                $"Wave {_waveNumber} begins!",

                isBoss: isBoss);



            if (isBoss)

            {

                RaiseCombatEvent(

                    CombatEventType.BossStarted,

                    $"Boss wave {_waveNumber}!",

                    isBoss: true);

            }

        }



        private static string GetSkillDisplayName(EquipmentSkill skill)

        {

            if (skill == null)

            {

                return "Skill";

            }



            return string.IsNullOrEmpty(skill.skillName) ? skill.skillType.ToString() : skill.skillName;

        }

    }

}


