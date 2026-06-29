using System.Collections;

using System.Collections.Generic;

using Crownsfall.Characters;

using Crownsfall.Combat.Events;

using Crownsfall.Combat.Skills;

using Crownsfall.Combat.UI;

using Crownsfall.Core;

using Crownsfall.Services;

using Crownsfall.UI;

using UnityEngine;

using UnityEngine.UI;



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



        [Header("Wave Banner")]

        [Tooltip("Centered banner shown before each wave's combat begins. Auto-found if left empty.")]

        public WaveBannerUI waveBannerUI;



        [Header("Reward Screen")]

        [Tooltip("Post-wave reward overlay shown after each enemy defeat. Auto-found if left empty.")]

        public RewardScreenUI rewardScreenUI;





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

        [Tooltip("When true, enemy rig mirrors the player's equipment sprites (still tinted). On by default so wave 1 is visible before enemy gear lists are wired.")]

        [SerializeField] private bool enemyMirrorPlayerAppearance = true;



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



        [Tooltip("Seconds to wait after enemy entrance before combat resumes.")]

        public float waveTransitionPostEntranceDelay = 0.35f;



        [Header("Combat Animation")]

        [Tooltip("How long the attacker slides toward the opponent before holding at the strike point.")]

        public float attackMoveDuration = 0.25f;



        [Tooltip("Brief pause at the end of the lunge before the attacker returns home.")]

        public float attackHoldDuration = 0.08f;



        [Tooltip("How long the attacker takes to slide back to their starting position.")]

        public float attackReturnDuration = 0.25f;



        [Tooltip("How far toward the target the attacker moves (0.35 = 35% of the distance between home positions).")]

        public float attackDistancePercent = 0.35f;



        [Tooltip("How far the player moves during the wind-up before the punch (fraction of gap between home positions).")]

        public float playerPunchApproachPercent = 0.10f;



        [Tooltip("How far toward the enemy the player root steps before the punch/kick (fraction of gap between home positions in shared battle space).")]

        public float playerAttackStepForwardPercent = 0.15f;



        [Tooltip("Seconds for the player root to step forward toward the enemy before the punch/kick.")]

        public float playerAttackStepForwardDuration = 0.12f;



        [Tooltip("Seconds for the player root to step back to home after damage is applied.")]

        public float playerAttackStepBackDuration = 0.25f;



        [Tooltip("Fraction of attackMoveDuration spent on the fast jab snap; remainder is the slow approach wind-up.")]

        [Range(0.05f, 0.25f)]

        public float playerPunchSnapDurationRatio = 0.12f;



        [Tooltip("Peak uniform scale on the player rig during the jab snap (1 = no scale bump).")]

        public float playerPunchScalePeak = 1.08f;



        [Tooltip("Brief Z rotation in degrees during the jab snap (0 = none). Sign follows facing.")]

        public float playerPunchRotationDegrees = 0f;



        [Tooltip("How far the weapon anchor extends forward (local +X px) during the jab snap.")]

        public float playerPunchWeaponExtendPixels = 100f;



        [Tooltip("How far the leg anchor extends forward (local +X px) during the jab snap.")]

        public float playerPunchLegExtendPixels = 85f;



        [Tooltip("Fraction of attackMoveDuration used for the weapon jab before the leg kick begins.")]

        [Range(0.35f, 0.65f)]

        public float playerPunchWeaponMoveFraction = 0.55f;



        [Tooltip("Fraction of attackHoldDuration to hold the weapon extended after the jab snap.")]

        [Range(0.1f, 0.6f)]

        public float playerPunchWeaponHoldFraction = 0.35f;



        [Tooltip("Fraction of attackHoldDuration to pause (weapon retracts) before the leg wind-up.")]

        [Range(0f, 0.4f)]

        public float playerPunchLegDelayAfterWeaponSnap = 0.15f;



        [Tooltip("Scales how far the rig root moves during the player punch (body stays relatively stable).")]

        [Range(0.1f, 1f)]

        public float playerPunchRootDistanceMultiplier = 0.35f;



        [Tooltip("Total time the defender shakes left/right after taking attack damage.")]

        public float hitShakeDuration = 0.18f;



        [Tooltip("Horizontal offset (pixels) applied each shake step on the defender rig.")]

        public float hitShakeStrength = 8f;



        [Tooltip("Number of left/right wobble steps during a hit reaction.")]

        public int hitShakeSteps = 6;



        [Tooltip("How long the defeated fighter rig shrinks toward zero scale on death.")]

        public float deathShrinkDuration = 0.35f;



        [Tooltip("How long the defeated fighter rig fades out (CanvasGroup alpha) on death.")]

        public float deathFadeDuration = 0.35f;



        [Tooltip("Z tilt in degrees during the brief defeat pose before shrink/fade.")]

        public float deathTiltDegrees = 12f;



        [Tooltip("Seconds for the defeat tilt/fall pose before shrink/fade begins.")]

        public float deathTiltDuration = 0.2f;



        [Tooltip("Probability (0–1) that an attack uses a kick instead of a punch when LegAnchor exists.")]

        [Range(0f, 1f)]

        public float attackKickProbability = 0.3f;



        [Tooltip("How far the defender recoils backward (shared battle space pixels) when hit.")]

        public float hitRecoilDistance = 14f;



        [Tooltip("Seconds for the defender hit recoil and return.")]

        public float hitRecoilDuration = 0.14f;



        [Tooltip("Optional Z rotation in degrees during hit recoil (0 = none).")]

        public float hitRecoilRotationDegrees = 4f;



        [Tooltip("Peak uniform scale multiplier during hit recoil bounce (1 = none).")]

        public float hitRecoilScalePeak = 1.04f;



        [Tooltip("Y-scale multiplier at hit recoil peak for subtle squash (1 = no squash).")]

        public float hitRecoilScaleYSquash = 0.97f;



        [Tooltip("Brief hold at the stepped position before the strike snap (anticipation).")]

        public float attackAnticipationDuration = 0.05f;



        [Tooltip("Real-time impact pause after strike lands — visual only; does not freeze UI or Time.timeScale.")]

        public float hitStopDuration = 0.05f;



        [Tooltip("Small backward knockback distance before defeat tilt (UI pixels).")]

        public float deathPreRecoilDistance = 6f;



        [Tooltip("Duration of the pre-defeat knockback before fall/tilt.")]

        public float deathPreRecoilDuration = 0.08f;



        [Header("Enemy Entrance Animation")]

        [Tooltip("Duration of the normal enemy slide-in entrance (off-screen right → home).")]

        public float normalEntranceDuration = 0.45f;



        [Tooltip("Duration of the boss slide-in entrance (farther offset, heavier landing).")]

        public float bossEntranceDuration = 0.8f;



        [Tooltip("How far off-screen (pixels right of home) the normal enemy starts.")]

        public float normalEntranceOffsetX = 250f;



        [Tooltip("How far off-screen (pixels right of home) the boss starts.")]

        public float bossEntranceOffsetX = 400f;



        [Tooltip("Optional backdrop Image for the boss red flash. Auto-finds EnemyBackdrop under the enemy slot if empty.")]

        [SerializeField] private Image bossEntranceFlashBackdrop;



        [Header("Victory (optional)")]

        [Tooltip("Wave number that ends the battle in victory. 0 = endless (no victory). Example: 10 = win after wave 10 is cleared.")]

        public int victoryWaveNumber = 0;



        [Tooltip("How far the player rig moves upward during the victory pose (UI pixels).")]

        public float victoryMoveUpAmount = 30f;



        [Tooltip("Peak scale multiplier during the victory pose (1.08 = 8% larger than rest size).")]

        public float victoryScaleMultiplier = 1.08f;



        [Tooltip("Seconds to hold the victory pose at peak before returning to idle.")]

        public float victoryHoldDuration = 0.5f;



        [Tooltip("Seconds to lerp in and out of the victory pose.")]

        public float victoryAnimDuration = 0.3f;



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

        /// When true, the battle loop yields without attacking (reward screen + wave transition).

        /// </summary>

        private bool _combatPaused;



        /// <summary>

        /// Reference to the active auto-combat loop so it can be tracked from BeginCombatAfterDelay.

        /// </summary>

        private Coroutine _battleLoopCoroutine;



        /// <summary>

        /// How many wave enemies the player has defeated this run (each kill +1).

        /// </summary>

        private int _enemiesDefeated;



        /// <summary>

        /// Observes combat events and wave rewards for the post-victory summary screen.

        /// </summary>




        /// <summary>

        /// Highest wave number reached this run (updates when a wave is cleared or on death).

        /// </summary>

        private int _highestWaveReached;



        /// <summary>

        /// UI anchoredPosition of the player rig when combat begins (rest position after each attack).

        /// </summary>

        private Vector3 playerHomePosition;



        /// <summary>

        /// UI anchoredPosition of the enemy rig when combat begins (rest position after each attack).

        /// </summary>

        private Vector3 enemyHomePosition;



        /// <summary>

        /// UI localScale of the player rig at battle start (includes facing flip from SetFacing).

        /// </summary>

        private Vector3 playerOriginalScale = Vector3.one;



        /// <summary>

        /// UI localScale of the enemy rig at battle start (includes facing flip from SetFacing).

        /// </summary>

        private Vector3 enemyOriginalScale = Vector3.one;



        /// <summary>

        /// Ensures the victory pose animation runs at most once per battle.

        /// </summary>

        private bool _victoryAnimationPlayed;



        /// <summary>
        /// Punch or kick — one strike type per attack (never both).
        /// </summary>
        private enum AttackStrikeType
        {
            Punch,
            Kick
        }



        /// <summary>
        /// Guards against overlapping attack presentation coroutines corrupting rig transforms.
        /// </summary>
        private bool _attackPresentationRunning;



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

            CaptureFighterHomePositions();

            CaptureFighterOriginalScales();

            PrepareEnemySpawnStartState();

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



            // Wave 1: same serialized intro as post-reward wave advances (NotifyWaveStarted already fired in Start).

            yield return RunWavePresentationSequence(notifyWaveStarted: false, logWaveTransition: false);



            _battleLoopCoroutine = StartCoroutine(StartBattleLoop());

            yield return _battleLoopCoroutine;

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

                    while (_combatPaused)

                    {

                        yield return null;

                    }


                    // --- Player turn: punch toward enemy, then apply damage ---

                    yield return PerformPlayerAttackWithAnimation();


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



                    yield return PerformEnemyAttackWithAnimation();



                    if (!_playerFighter.isAlive)

                    {

                        yield return HandlePlayerDefeated();

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

        /// <remarks>
        /// Each wave gets a fresh <see cref="_enemyFighter"/> data instance from the factory.
        /// The scene <see cref="enemyFighterRig"/> GameObject is NOT destroyed or re-instantiated —
        /// the same UI rig is reused: death animation shrinks/fades the old look, then
        /// <see cref="DisplayEnemyFighter"/> refreshes visuals for the new enemy and
        /// <see cref="PrepareEnemySpawnStartState"/> hides the rig until the entrance animation runs.
        /// </remarks>

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

            _combatPaused = true;

            StopFighterIdleBreath();



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



            // Shrink + fade the defeated enemy rig. No Destroy — enemyFighterRig is reused for the next wave.

            yield return AnimateFighterDeath(

                GetRigTransform(enemyFighterRig),

                GetOrAddCanvasGroup(enemyFighterRig != null ? enemyFighterRig.gameObject : null),

                enemyOriginalScale);

            yield return ShowRewardScreenForClearedWave();



            // Final wave cleared — play victory once, then stop combat (no next wave).

            if (IsBattleWon())

            {

                yield return HandleBattleWon();

                _combatPaused = false;

                yield break;

            }



            if (rewardScreenUI == null)

            {

                yield return new WaitForSeconds(waveTransitionDelay);

            }



            _waveNumber++;

            _highestWaveReached = Mathf.Max(_highestWaveReached, _waveNumber);

            SpawnEnemyForWave(_waveNumber);



            if (battleHUD != null)

            {

                battleHUD.SetWave(_waveNumber, _currentProgressionProfile.isBossWave);

            }



            UpdateEnemyHealthDisplay();

            DisplayEnemyFighter();

            CaptureEnemyHomePosition();

            PrepareEnemySpawnStartState();

            // Serialized: WaveStarted → banner → entrance → pause → combat loop resumes when this returns.

            yield return RunWavePresentationSequence(notifyWaveStarted: true, logWaveTransition: true);

        }



        /// <summary>

        /// True when victoryWaveNumber is set and the current wave (just cleared) meets or exceeds it.

        /// </summary>

        private bool IsBattleWon()

        {

            return victoryWaveNumber > 0 && _waveNumber >= victoryWaveNumber;

        }



        /// <summary>

        /// Called when the final wave enemy is defeated. Plays the victory pose once, raises BattleWon, stops combat.

        /// </summary>

        private IEnumerator HandleBattleWon()

        {

            if (!_victoryAnimationPlayed)

            {

                _victoryAnimationPlayed = true;

                yield return PlayVictoryAnimation();

            }



            _combatRunning = false;

            _highestWaveReached = Mathf.Max(_highestWaveReached, _waveNumber);



            RaiseCombatEvent(

                CombatEventType.BattleWon,

                $"Victory! Wave {_waveNumber} cleared!",

                sourceName: _playerFighter.fighterName,

                targetName: _enemyFighter?.enemyName,

                score: _playerFighter.currentScore);

        }



        /// <summary>

        /// Victory pose: player rig moves up slightly, scales up, holds, then returns to home position and scale.

        /// Uses anchoredPosition (like attack lunge) and localScale (like death animation).

        /// </summary>

        private IEnumerator PlayVictoryAnimation()

        {




            StopFighterIdleBreath();



            var playerRect = GetRigRectTransform(playerFighterRig);

            var playerTransform = GetRigTransform(playerFighterRig);

            if (playerRect == null || playerTransform == null)

            {

                yield break;

            }



            var homePos = playerHomePosition;

            var peakPos = homePos + new Vector3(0f, victoryMoveUpAmount, 0f);

            var restScale = playerOriginalScale;

            var peakScale = restScale * victoryScaleMultiplier;



            // Step 1: lerp up and scale up.

            yield return LerpVictoryPose(

                playerRect,

                playerTransform,

                homePos,

                peakPos,

                restScale,

                peakScale,

                victoryAnimDuration);



            // Step 2: hold the victory pose.

            yield return new WaitForSeconds(victoryHoldDuration);



            // Step 3: lerp back to idle position and original scale.

            yield return LerpVictoryPose(

                playerRect,

                playerTransform,

                peakPos,

                homePos,

                peakScale,

                restScale,

                victoryAnimDuration);



            // Guarantee exact rest pose (avoids float drift from lerp).

            SetRectAnchoredPosition(playerRect, playerHomePosition);

            playerTransform.localScale = playerOriginalScale;

        }



        /// <summary>

        /// Lerps player rig anchoredPosition and localScale together for the victory pose.

        /// </summary>

        private static IEnumerator LerpVictoryPose(

            RectTransform rect,

            Transform rigTransform,

            Vector3 startPos,

            Vector3 endPos,

            Vector3 startScale,

            Vector3 endScale,

            float duration)

        {

            if (rect == null || rigTransform == null)

            {

                yield break;

            }



            if (duration <= 0f)

            {

                SetRectAnchoredPosition(rect, endPos);

                rigTransform.localScale = endScale;

                yield break;

            }



            var elapsed = 0f;

            while (elapsed < duration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);

                SetRectAnchoredPosition(rect, Vector3.Lerp(startPos, endPos, t));

                rigTransform.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;

            }



            SetRectAnchoredPosition(rect, endPos);

            rigTransform.localScale = endScale;

        }



        /// <summary>

        /// Called when the player reaches 0 HP. Plays death animation, then stops combat and shows game over.

        /// </summary>

        private IEnumerator HandlePlayerDefeated()

        {

            // Shrink + fade the player rig before the game over panel covers the battlefield.

            yield return AnimateFighterDeath(

                GetRigTransform(playerFighterRig),

                GetOrAddCanvasGroup(playerFighterRig != null ? playerFighterRig.gameObject : null),

                playerOriginalScale);



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

        /// Saves each fighter rig's current UI scale as the rest size for death/respawn animations.

        /// Called after rigs are displayed at battle start (SetFacing has already run).

        /// </summary>

        private void CaptureFighterOriginalScales()

        {

            CapturePlayerOriginalScale();

            CaptureEnemyOriginalScale();

        }



        /// <summary>

        /// Stores the player rig's localScale so death animation can shrink from here and reset later.

        /// </summary>

        private void CapturePlayerOriginalScale()

        {

            var playerTransform = GetRigTransform(playerFighterRig);

            if (playerTransform == null)

            {

                return;

            }



            playerOriginalScale = playerTransform.localScale;

        }



        /// <summary>

        /// Stores the enemy rig's localScale so death animation can shrink from here and reset on respawn.

        /// </summary>

        private void CaptureEnemyOriginalScale()

        {

            var enemyTransform = GetRigTransform(enemyFighterRig);

            if (enemyTransform == null)

            {

                return;

            }



            enemyOriginalScale = enemyTransform.localScale;

        }



        /// <summary>

        /// Saves each fighter rig's current UI position as its home/rest spot for attack animations.

        /// Called after rigs are displayed at battle start and when a new wave enemy appears.

        /// </summary>

        private void CaptureFighterHomePositions()

        {

            CapturePlayerHomePosition();

            CaptureEnemyHomePosition();

        }



        /// <summary>

        /// Stores the player rig's anchoredPosition so attacks can lunge out and return here.

        /// </summary>

        private void CapturePlayerHomePosition()

        {

            var playerRect = GetRigRectTransform(playerFighterRig);

            if (playerRect == null)

            {

                return;

            }



            playerHomePosition = RectAnchoredToVector3(playerRect);

        }



        /// <summary>

        /// Stores the enemy rig's anchoredPosition so attacks can lunge out and return here.

        /// </summary>

        private void CaptureEnemyHomePosition()

        {

            var enemyRect = GetRigRectTransform(enemyFighterRig);

            if (enemyRect == null)

            {

                return;

            }



            enemyHomePosition = RectAnchoredToVector3(enemyRect);

        }



        /// <summary>

        /// Player turn wrapper: compute crit before animation, step forward and strike, apply damage,

        /// defender reacts, then step back to idle.

        /// </summary>

        private IEnumerator PerformPlayerAttackWithAnimation()

        {



            if (_combatPaused || _attackPresentationRunning)

            {

                yield break;

            }



            _attackPresentationRunning = true;

            StopFighterIdleBreath();



            var attackWave = _waveNumber;



            // Skills run before the punch so crit can boost punch reach for that swing only.

            var attackOutcome = ComputePlayerAttackOutcome();



            const float critPunchReachMultiplier = 1.2f;

            const float critRecoilMultiplier = 1.5f;

            var punchReachMultiplier = attackOutcome.isCritical ? critPunchReachMultiplier : 1f;



            var stepPosition = ComputeAttackStepPosition(

                playerFighterRig,

                enemyFighterRig,

                playerHomePosition,

                enemyHomePosition,

                playerAttackStepForwardPercent,

                punchReachMultiplier);



            var strikeType = RollAttackStrikeType(playerFighterRig);



            // Idle → step forward → punch OR kick (root holds at step position).

            yield return AnimateAttackStrikeSequence(

                playerFighterRig,

                playerHomePosition,

                stepPosition,

                playerOriginalScale,

                punchReachMultiplier,

                strikeType);



            if (_combatPaused || !_combatRunning || attackWave != _waveNumber || _enemyFighter == null || !_enemyFighter.isAlive)

            {

                _attackPresentationRunning = false;

                yield break;

            }





            ApplyPlayerAttackOutcome(attackOutcome);





            if (_combatPaused || !_combatRunning || attackWave != _waveNumber || _enemyFighter == null || !_enemyFighter.isAlive)

            {

                _attackPresentationRunning = false;

                yield break;

            }



            yield return AnimateHitRecoil(

                enemyFighterRig,

                enemyHomePosition,

                playerHomePosition,

                playerFighterRig,

                enemyOriginalScale,

                attackOutcome.isCritical ? critRecoilMultiplier : 1f);



            if (_combatPaused || !_combatRunning || attackWave != _waveNumber)

            {

                _attackPresentationRunning = false;

                yield break;

            }



            if (enemyFighterRig != null)

            {

                if (attackOutcome.isCritical)

                {

                    yield return enemyFighterRig.FlashDamageCritical();

                    yield return enemyFighterRig.ScalePunch();

                }

                else

                {

                    enemyFighterRig.FlashDamage();

                }

            }



            yield return AnimateAttackStrikeRecover(

                playerFighterRig,

                stepPosition,

                playerOriginalScale,

                punchReachMultiplier,

                strikeType);



            // Step back → idle at home.

            yield return AnimateAttackStepBack(

                playerFighterRig,

                playerHomePosition,

                stepPosition,

                playerOriginalScale,

                playerAttackStepBackDuration);



            _attackPresentationRunning = false;

            StartFighterIdleBreath();

        }



        /// <summary>

        /// Enemy turn wrapper: play the lunge animation first, then run existing damage logic.

        /// </summary>

        private IEnumerator PerformEnemyAttackWithAnimation()

        {

            if (_combatPaused || _attackPresentationRunning)

            {

                yield break;

            }



            _attackPresentationRunning = true;

            StopFighterIdleBreath();



            var attackWave = _waveNumber;



            var stepPosition = ComputeAttackStepPosition(

                enemyFighterRig,

                playerFighterRig,

                enemyHomePosition,

                playerHomePosition,

                playerAttackStepForwardPercent,

                1f);



            var strikeType = RollAttackStrikeType(enemyFighterRig);





            yield return AnimateAttackStrikeSequence(

                enemyFighterRig,

                enemyHomePosition,

                stepPosition,

                enemyOriginalScale,

                1f,

                strikeType);



            if (_combatPaused || !_combatRunning || attackWave != _waveNumber || _enemyFighter == null || !_enemyFighter.isAlive || _playerFighter == null || !_playerFighter.isAlive)

            {

                _attackPresentationRunning = false;

                yield break;

            }



            ApplyEnemyAttackDamage();



            yield return AnimateHitRecoil(

                playerFighterRig,

                playerHomePosition,

                enemyHomePosition,

                enemyFighterRig,

                playerOriginalScale,

                1f);



            if (_combatPaused || !_combatRunning || attackWave != _waveNumber)

            {

                _attackPresentationRunning = false;

                yield break;

            }



            if (playerFighterRig != null)

            {

                playerFighterRig.FlashDamage();

            }



            yield return AnimateAttackStrikeRecover(

                enemyFighterRig,

                stepPosition,

                enemyOriginalScale,

                1f,

                strikeType);



            yield return AnimateAttackStepBack(

                enemyFighterRig,

                enemyHomePosition,

                stepPosition,

                enemyOriginalScale,

                playerAttackStepBackDuration);



            _attackPresentationRunning = false;

            StartFighterIdleBreath();

        }



        /// <summary>

        /// Slides one UI fighter rig toward the opponent, holds briefly, then returns home.

        /// Uses Vector3.Lerp on RectTransform.anchoredPosition (x/y only). Skips instantly if rig is missing.

        /// </summary>

        private IEnumerator AnimateAttackLunge(

            FighterRig attackerRig,

            Vector3 home,

            Vector3 targetHome,

            float distanceMultiplier = 1f)

        {

            var attackerRect = GetRigRectTransform(attackerRig);

            if (attackerRect == null)

            {

                yield break;

            }



            // Strike point = partway between attacker home and opponent home (e.g. 35% of the gap).

            // distanceMultiplier > 1 pushes crit lunges farther toward the target.

            var effectiveDistance = attackDistancePercent * distanceMultiplier;

            var attackPos = Vector3.Lerp(home, targetHome, effectiveDistance);



            // Step 1: lunge forward.

            yield return LerpRectAnchoredPosition(attackerRect, home, attackPos, attackMoveDuration);



            // Step 2: hold at the strike point so the hit reads clearly on screen.

            yield return new WaitForSeconds(attackHoldDuration);



            // Step 3: slide back to the stored home position.

            yield return LerpRectAnchoredPosition(attackerRect, attackPos, home, attackReturnDuration);



            // Guarantee exact rest position (avoids float drift from lerp).

            SetRectAnchoredPosition(attackerRect, home);

        }



        /// <summary>

        /// Attack presentation: step forward, then a single punch OR kick at the stepped position.

        /// Root stays at the step point until <see cref="AnimateAttackStepBack"/> runs after damage.

        /// </summary>

        private IEnumerator AnimateAttackStrikeSequence(

            FighterRig attackerRig,

            Vector3 home,

            Vector3 stepPosition,

            Vector3 originalScale,

            float distanceMultiplier,

            AttackStrikeType strikeType)

        {



            var attackerRect = GetRigRectTransform(attackerRig);

            if (attackerRect == null)

            {

                yield break;

            }



            var attackerTransform = GetRigTransform(attackerRig);

            var weaponRect = attackerRig != null ? attackerRig.WeaponAnchor : null;

            var legRect = attackerRig != null ? attackerRig.LegAnchor : null;



            if (strikeType == AttackStrikeType.Kick && legRect == null)

            {

                strikeType = AttackStrikeType.Punch;

            }



            SetRectAnchoredPosition(attackerRect, home);



            yield return LerpPlayerPunchPhase(

                attackerRect,

                attackerTransform,

                home,

                stepPosition,

                originalScale,

                originalScale,

                GetRigUprightRotation(attackerTransform),

                GetRigUprightRotation(attackerTransform),

                playerAttackStepForwardDuration,

                EaseOutQuad,

                null);



            if (attackAnticipationDuration > 0f)

            {

                SetRectAnchoredPosition(attackerRect, stepPosition);





                yield return new WaitForSecondsRealtime(attackAnticipationDuration);

            }



            var restScale = originalScale;

            var peakScale = restScale * playerPunchScalePeak;

            var restRotation = GetRigUprightRotation(attackerTransform);

            var facingSign = restScale.x >= 0f ? 1f : -1f;

            var strikeRotation = restRotation + new Vector3(0f, 0f, -playerPunchRotationDegrees * facingSign);



            var strikeMoveDuration = attackMoveDuration;

            var snapDuration = strikeMoveDuration * playerPunchSnapDurationRatio;

            var approachDuration = strikeMoveDuration - snapDuration;



            if (strikeType == AttackStrikeType.Punch && weaponRect != null)

            {

                var weaponRestPos = weaponRect.anchoredPosition;

                var weaponExtendedPos = weaponRestPos + new Vector2(playerPunchWeaponExtendPixels * distanceMultiplier, 0f);



                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    restScale,

                    restScale,

                    restRotation,

                    restRotation,

                    approachDuration,

                    EaseInQuad,

                    weaponRect,

                    weaponRestPos,

                    weaponRestPos,

                    null);



                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    restScale,

                    peakScale,

                    restRotation,

                    strikeRotation,

                    snapDuration,

                    EaseOutQuad,

                    weaponRect,

                    weaponRestPos,

                    weaponExtendedPos,

                    null);



                yield return HoldAttackImpactPose(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    peakScale,

                    strikeRotation,

                    weaponRect,

                    weaponExtendedPos,

                    null,

                    default);

            }

            else if (strikeType == AttackStrikeType.Kick && legRect != null)

            {

                var legRestPos = legRect.anchoredPosition;

                var legExtendedPos = legRestPos + new Vector2(playerPunchLegExtendPixels * distanceMultiplier, 0f);



                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    restScale,

                    restScale,

                    restRotation,

                    restRotation,

                    approachDuration,

                    EaseInQuad,

                    null,

                    default,

                    default,

                    legRect,

                    legRestPos,

                    legRestPos);



                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    restScale,

                    peakScale,

                    restRotation,

                    strikeRotation,

                    snapDuration,

                    EaseOutQuad,

                    null,

                    default,

                    default,

                    legRect,

                    legRestPos,

                    legExtendedPos);



                yield return HoldAttackImpactPose(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    peakScale,

                    strikeRotation,

                    null,

                    default,

                    legRect,

                    legExtendedPos);

            }

            else

            {

                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    restScale,

                    peakScale,

                    restRotation,

                    strikeRotation,

                    snapDuration,

                    EaseOutQuad,

                    null);



                yield return HoldAttackImpactPose(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    peakScale,

                    strikeRotation,

                    null,

                    default,

                    null,

                    default);

            }



        }



        /// <summary>

        /// Real-time impact pause — freezes presentation pose only; UI and other coroutines keep running.

        /// </summary>

        private IEnumerator HoldAttackImpactPose(

            RectTransform attackerRect,

            Transform attackerTransform,

            Vector3 stepPosition,

            Vector3 peakScale,

            Vector3 strikeRotation,

            RectTransform weaponRect,

            Vector2 weaponExtendedPos,

            RectTransform legRect,

            Vector2 legExtendedPos)

        {

            if (attackerRect != null)

            {

                SetRectAnchoredPosition(attackerRect, stepPosition);

            }



            if (attackerTransform != null)

            {

                attackerTransform.localScale = peakScale;

                attackerTransform.localEulerAngles = strikeRotation;

            }



            if (weaponRect != null)

            {

                weaponRect.anchoredPosition = weaponExtendedPos;

            }



            if (legRect != null)

            {

                legRect.anchoredPosition = legExtendedPos;

            }



            if (hitStopDuration <= 0f)

            {

                yield break;

            }





            yield return new WaitForSecondsRealtime(hitStopDuration);

        }



        /// <summary>

        /// Retracts the strike limb and settles attacker scale/rotation after impact and defender recoil.

        /// </summary>

        private IEnumerator AnimateAttackStrikeRecover(

            FighterRig attackerRig,

            Vector3 stepPosition,

            Vector3 originalScale,

            float distanceMultiplier,

            AttackStrikeType strikeType)

        {

            var attackerRect = GetRigRectTransform(attackerRig);

            if (attackerRect == null)

            {

                yield break;

            }



            var attackerTransform = GetRigTransform(attackerRig);

            var weaponRect = attackerRig != null ? attackerRig.WeaponAnchor : null;

            var legRect = attackerRig != null ? attackerRig.LegAnchor : null;



            if (strikeType == AttackStrikeType.Kick && legRect == null)

            {

                strikeType = AttackStrikeType.Punch;

            }



            var restScale = originalScale;

            var peakScale = restScale * playerPunchScalePeak;

            var restRotation = GetRigUprightRotation(attackerTransform);

            var facingSign = restScale.x >= 0f ? 1f : -1f;

            var strikeRotation = restRotation + new Vector3(0f, 0f, -playerPunchRotationDegrees * facingSign);

            var strikeMoveDuration = attackMoveDuration;

            var snapDuration = strikeMoveDuration * playerPunchSnapDurationRatio;

            var settleDuration = Mathf.Max(snapDuration, attackHoldDuration * 0.35f);



            if (strikeType == AttackStrikeType.Punch && weaponRect != null)

            {

                var weaponExtendedPos = weaponRect.anchoredPosition;

                var weaponRestPos = weaponExtendedPos - new Vector2(playerPunchWeaponExtendPixels * distanceMultiplier, 0f);



                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    peakScale,

                    restScale,

                    strikeRotation,

                    restRotation,

                    settleDuration,

                    EaseInOutQuad,

                    weaponRect,

                    weaponExtendedPos,

                    weaponRestPos,

                    null);

            }

            else if (strikeType == AttackStrikeType.Kick && legRect != null)

            {

                var legExtendedPos = legRect.anchoredPosition;

                var legRestPos = legExtendedPos - new Vector2(playerPunchLegExtendPixels * distanceMultiplier, 0f);



                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    peakScale,

                    restScale,

                    strikeRotation,

                    restRotation,

                    settleDuration,

                    EaseInOutQuad,

                    null,

                    default,

                    default,

                    legRect,

                    legExtendedPos,

                    legRestPos);

            }

            else if (attackerTransform != null)

            {

                yield return LerpPlayerPunchPhase(

                    attackerRect,

                    attackerTransform,

                    stepPosition,

                    stepPosition,

                    peakScale,

                    restScale,

                    strikeRotation,

                    restRotation,

                    settleDuration,

                    EaseInOutQuad,

                    null);

            }



            SetRectAnchoredPosition(attackerRect, stepPosition);



            if (attackerTransform != null)

            {

                attackerTransform.localScale = restScale;

                attackerTransform.localEulerAngles = restRotation;

            }



            attackerRig?.ApplyOffsets();



        }



        /// <summary>

        /// Slides the attacker root from the strike step point back to home and settles the rest pose.

        /// </summary>

        private IEnumerator AnimateAttackStepBack(

            FighterRig attackerRig,

            Vector3 home,

            Vector3 stepPosition,

            Vector3 originalScale,

            float stepBackDuration)

        {




            var attackerRect = GetRigRectTransform(attackerRig);

            if (attackerRect == null)

            {

                yield break;

            }



            var attackerTransform = GetRigTransform(attackerRig);



            yield return LerpPlayerPunchPhase(

                attackerRect,

                attackerTransform,

                stepPosition,

                home,

                originalScale,

                originalScale,

                GetRigUprightRotation(attackerTransform),

                GetRigUprightRotation(attackerTransform),

                stepBackDuration,

                EaseInOutQuad,

                null);



            SetRectAnchoredPosition(attackerRect, home);

            if (attackerTransform != null)

            {

                attackerTransform.localScale = originalScale;

                ResetRigUprightRotation(attackerTransform);

            }



        }



        /// <summary>

        /// Computes how far an attacker root steps toward a target in shared battle space.

        /// </summary>

        private Vector3 ComputeAttackStepPosition(

            FighterRig attackerRig,

            FighterRig targetRig,

            Vector3 attackerHome,

            Vector3 targetHome,

            float stepPercent,

            float distanceMultiplier = 1f)

        {

            var attackerRect = GetRigRectTransform(attackerRig);

            var targetRect = GetRigRectTransform(targetRig);

            if (attackerRect == null)

            {

                return attackerHome;

            }



            var effectivePercent = Mathf.Clamp01(stepPercent * distanceMultiplier);



            if (targetRect == null || effectivePercent <= 0f)

            {

                return attackerHome;

            }



            var sharedParent = GetSharedRectTransformParent(attackerRect, targetRect);

            if (sharedParent == null)

            {

                return attackerHome;

            }



            SetRectAnchoredPosition(attackerRect, attackerHome);

            SetRectAnchoredPosition(targetRect, targetHome);



            var startShared = SharedLocalFromWorld(sharedParent, attackerRect.position);

            var endShared = SharedLocalFromWorld(sharedParent, targetRect.position);

            var stepShared = new Vector3(

                Mathf.Lerp(startShared.x, endShared.x, effectivePercent),

                startShared.y,

                0f);



            var stepWorld = sharedParent.TransformPoint(stepShared);

            var stepAnchored = RigAnchoredFromWorldPosition(attackerRect, stepWorld);



            SetRectAnchoredPosition(attackerRect, attackerHome);



            return stepAnchored;

        }



        /// <summary>

        /// Picks punch (default) or kick (alternate) for one strike per attack.

        /// </summary>

        private AttackStrikeType RollAttackStrikeType(FighterRig rig)

        {

            if (rig == null || rig.LegAnchor == null)

            {

                return AttackStrikeType.Punch;

            }



            return Random.value < attackKickProbability ? AttackStrikeType.Kick : AttackStrikeType.Punch;

        }



        /// <summary>

        /// Stops idle breath on both rigs before presentation animations that own transforms.

        /// </summary>

        private void StopFighterIdleBreath()

        {

            playerFighterRig?.StopIdleBreath();

            enemyFighterRig?.StopIdleBreath();

        }



        /// <summary>

        /// Refreshes idle breath rest poses from stored home positions and starts both rigs.

        /// </summary>

        private void StartFighterIdleBreath()

        {

            if (_combatPaused || !_combatRunning)

            {

                return;

            }



            SyncIdleBreathRestFromHomes();

            playerFighterRig?.StartIdleBreath();

            if (_enemyFighter != null && _enemyFighter.isAlive)

            {

                enemyFighterRig?.StartIdleBreath();

            }

        }



        /// <summary>

        /// Pushes current home anchored positions and scales into each rig's idle breath cache.

        /// </summary>

        private void SyncIdleBreathRestFromHomes()

        {

            if (playerFighterRig != null)

            {

                playerFighterRig.SetIdleBreathRestPose(

                    new Vector2(playerHomePosition.x, playerHomePosition.y),

                    playerOriginalScale);

            }



            if (enemyFighterRig != null)

            {

                enemyFighterRig.SetIdleBreathRestPose(

                    new Vector2(enemyHomePosition.x, enemyHomePosition.y),

                    enemyOriginalScale);

            }

        }



        /// <summary>

        /// Brief backward recoil on the defender in shared battle space, away from the attacker.

        /// </summary>

        private IEnumerator AnimateHitRecoil(

            FighterRig defenderRig,

            Vector3 defenderHome,

            Vector3 attackerHome,

            FighterRig attackerRig,

            Vector3 defenderOriginalScale,

            float strengthMultiplier = 1f)

        {

            var defenderRect = GetRigRectTransform(defenderRig);

            var attackerRect = GetRigRectTransform(attackerRig);

            var defenderTransform = GetRigTransform(defenderRig);



            if (defenderRect == null || defenderTransform == null || hitRecoilDuration <= 0f)

            {

                yield break;

            }



            StopFighterIdleBreath();



            var sharedParent = attackerRect != null

                ? GetSharedRectTransformParent(defenderRect, attackerRect)

                : null;



            Vector3 recoilAnchored = defenderHome;



            if (sharedParent != null && attackerRect != null)

            {

                SetRectAnchoredPosition(defenderRect, defenderHome);

                SetRectAnchoredPosition(attackerRect, attackerHome);



                var defenderShared = SharedLocalFromWorld(sharedParent, defenderRect.position);

                var attackerShared = SharedLocalFromWorld(sharedParent, attackerRect.position);

                var awayDir = defenderShared - attackerShared;

                if (awayDir.sqrMagnitude > 0.0001f)

                {

                    awayDir.Normalize();

                }

                else

                {

                    awayDir = Vector3.right;

                }



                var recoilShared = defenderShared + awayDir * (hitRecoilDistance * strengthMultiplier);

                var recoilWorld = sharedParent.TransformPoint(recoilShared);

                recoilAnchored = RigAnchoredFromWorldPosition(defenderRect, recoilWorld);

                SetRectAnchoredPosition(defenderRect, defenderHome);

            }

            else

            {

                var awayX = Mathf.Sign(defenderHome.x - attackerHome.x);

                if (Mathf.Approximately(awayX, 0f))

                {

                    awayX = 1f;

                }



                recoilAnchored = defenderHome + new Vector3(awayX * hitRecoilDistance * strengthMultiplier, 0f, 0f);

            }



            var restRotation = GetRigUprightRotation(defenderTransform);

            var facingSign = defenderOriginalScale.x >= 0f ? 1f : -1f;

            var recoilRotation = restRotation + new Vector3(0f, 0f, hitRecoilRotationDegrees * facingSign);

            var peakScale = new Vector3(

                defenderOriginalScale.x * hitRecoilScalePeak,

                defenderOriginalScale.y * hitRecoilScaleYSquash,

                defenderOriginalScale.z);

            var halfDuration = hitRecoilDuration * 0.5f;





            yield return LerpPlayerPunchPhase(

                defenderRect,

                defenderTransform,

                defenderHome,

                recoilAnchored,

                defenderOriginalScale,

                peakScale,

                restRotation,

                recoilRotation,

                halfDuration,

                EaseOutQuad,

                null);



            yield return LerpPlayerPunchPhase(

                defenderRect,

                defenderTransform,

                recoilAnchored,

                defenderHome,

                peakScale,

                defenderOriginalScale,

                recoilRotation,

                restRotation,

                halfDuration,

                EaseInOutQuad,

                null);



            SetRectAnchoredPosition(defenderRect, defenderHome);

            defenderTransform.localScale = defenderOriginalScale;

            defenderTransform.localEulerAngles = restRotation;

        }



        /// <summary>

        /// Computes how far the player root steps toward the enemy (legacy wrapper).

        /// </summary>

        private Vector3 ComputePlayerAttackStepPosition(

            FighterRig playerRig,

            Vector3 playerHome,

            Vector3 enemyHome,

            float distanceMultiplier = 1f)

        {

            return ComputeAttackStepPosition(

                playerRig,

                enemyFighterRig,

                playerHome,

                enemyHome,

                playerAttackStepForwardPercent,

                distanceMultiplier);

        }



        /// <summary>

        /// Finds the lowest common RectTransform ancestor of two UI rigs (typically BattleArea).

        /// </summary>

        private static RectTransform GetSharedRectTransformParent(RectTransform a, RectTransform b)

        {

            if (a == null || b == null)

            {

                return null;

            }



            var ancestors = new System.Collections.Generic.HashSet<Transform>();

            for (Transform t = a; t != null; t = t.parent)

            {

                ancestors.Add(t);

            }



            for (Transform t = b; t != null; t = t.parent)

            {

                if (ancestors.Contains(t) && t is RectTransform sharedRect)

                {

                    return sharedRect;

                }

            }



            return null;

        }



        /// <summary>

        /// Converts a world-space pivot into local x/y within a shared battle parent.

        /// </summary>

        private static Vector3 SharedLocalFromWorld(RectTransform sharedParent, Vector3 worldPos)

        {

            var local = sharedParent.InverseTransformPoint(worldPos);

            return new Vector3(local.x, local.y, 0f);

        }



        /// <summary>

        /// Resolves the rig anchoredPosition that places its pivot at a world-space point.

        /// </summary>

        private static Vector3 RigAnchoredFromWorldPosition(RectTransform rig, Vector3 worldPos)

        {

            rig.position = worldPos;

            return RectAnchoredToVector3(rig);

        }



        /// <summary>

        /// Lerps player punch phase: rig anchoredPosition plus optional scale/rotation on the root,

        /// and optional weapon- and leg-anchor local extension toward the target.

        /// Pass null for weaponRect or legRect to skip animating that limb for the phase.

        /// </summary>

        private static IEnumerator LerpPlayerPunchPhase(

            RectTransform rect,

            Transform rigTransform,

            Vector3 startPos,

            Vector3 endPos,

            Vector3 startScale,

            Vector3 endScale,

            Vector3 startRotation,

            Vector3 endRotation,

            float duration,

            System.Func<float, float> ease,

            RectTransform weaponRect = null,

            Vector2 weaponStartPos = default,

            Vector2 weaponEndPos = default,

            RectTransform legRect = null,

            Vector2 legStartPos = default,

            Vector2 legEndPos = default)

        {

            if (rect == null)

            {

                yield break;

            }



            if (duration <= 0f)

            {

                SetRectAnchoredPosition(rect, endPos);



                if (rigTransform != null)

                {

                    rigTransform.localScale = endScale;

                    rigTransform.localEulerAngles = endRotation;

                }



                if (weaponRect != null)

                {

                    weaponRect.anchoredPosition = weaponEndPos;

                }



                if (legRect != null)

                {

                    legRect.anchoredPosition = legEndPos;

                }



                yield break;

            }



            var elapsed = 0f;



            while (elapsed < duration)

            {

                elapsed += Time.deltaTime;

                var t = ease(Mathf.Clamp01(elapsed / duration));



                SetRectAnchoredPosition(rect, Vector3.Lerp(startPos, endPos, t));



                if (rigTransform != null)

                {

                    rigTransform.localScale = Vector3.Lerp(startScale, endScale, t);

                    rigTransform.localEulerAngles = Vector3.Lerp(startRotation, endRotation, t);

                }



                if (weaponRect != null)

                {

                    weaponRect.anchoredPosition = Vector2.Lerp(weaponStartPos, weaponEndPos, t);

                }



                if (legRect != null)

                {

                    legRect.anchoredPosition = Vector2.Lerp(legStartPos, legEndPos, t);

                }



                yield return null;

            }



            SetRectAnchoredPosition(rect, endPos);



            if (rigTransform != null)

            {

                rigTransform.localScale = endScale;

                rigTransform.localEulerAngles = endRotation;

            }



            if (weaponRect != null)

            {

                weaponRect.anchoredPosition = weaponEndPos;

            }



            if (legRect != null)

            {

                legRect.anchoredPosition = legEndPos;

            }

        }



        private static float EaseInQuad(float t) => t * t;



        private static float EaseOutQuad(float t)

        {

            var inv = 1f - t;

            return 1f - inv * inv;

        }



        private static float EaseInOutQuad(float t)

        {

            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

        }



        /// <summary>

        /// Brief left/right wobble on the defender after attack damage.

        /// Uses localPosition (not anchoredPosition) so the shake does not fight the lunge home positions.

        /// DoT ticks (burn/poison) do not call this — only direct attack hits.

        /// </summary>

        private IEnumerator ShakeHitReaction(Transform target, float strengthMultiplier = 1f)

        {

            if (target == null || hitShakeSteps <= 0 || hitShakeDuration <= 0f)

            {

                yield break;

            }



            // Remember rest spot so we can snap back when the shake finishes.

            var originalLocal = target.localPosition;

            var stepDuration = hitShakeDuration / hitShakeSteps;

            var shakeStrength = hitShakeStrength * strengthMultiplier;



            for (var i = 0; i < hitShakeSteps; i++)

            {

                // Alternate left (+X) and right (-X) for a quick hit-recoil feel.

                var xOffset = (i % 2 == 0) ? shakeStrength : -shakeStrength;

                target.localPosition = originalLocal + new Vector3(xOffset, 0f, 0f);

                yield return new WaitForSeconds(stepDuration);

            }



            target.localPosition = originalLocal;

        }



        /// <summary>

        /// Ensures a CanvasGroup exists on the rig root so alpha fade affects all child Images.

        /// Adds one at runtime if the prefab does not already have it.

        /// </summary>

        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)

        {

            if (go == null)

            {

                return null;

            }



            var canvasGroup = go.GetComponent<CanvasGroup>();

            if (canvasGroup == null)

            {

                canvasGroup = go.AddComponent<CanvasGroup>();

            }



            return canvasGroup;

        }



        /// <summary>

        /// Death animation: shrink localScale toward 0.1 and fade CanvasGroup alpha to 0.

        /// Shrink and fade run in parallel (each uses its own duration from Inspector settings).

        /// </summary>

        private IEnumerator AnimateFighterDeath(Transform rig, CanvasGroup canvasGroup, Vector3 originalScale)

        {

            if (rig == null)

            {

                yield break;

            }



            StopFighterIdleBreath();



            var rigRect = rig as RectTransform;

            var homeAnchored = rigRect != null ? RectAnchoredToVector3(rigRect) : Vector3.zero;

            var restRotation = GetRigUprightRotation(rig);

            var facingSign = originalScale.x >= 0f ? 1f : -1f;



            if (rigRect != null && deathPreRecoilDuration > 0f && deathPreRecoilDistance > 0f)

            {

                var recoilAnchored = homeAnchored + new Vector3(-deathPreRecoilDistance * facingSign, -3f, 0f);

                var recoilSquash = new Vector3(originalScale.x * 1.02f, originalScale.y * 0.96f, originalScale.z);





                yield return LerpPlayerPunchPhase(

                    rigRect,

                    rig,

                    homeAnchored,

                    recoilAnchored,

                    originalScale,

                    recoilSquash,

                    restRotation,

                    restRotation,

                    deathPreRecoilDuration,

                    EaseOutQuad,

                    null);



                homeAnchored = recoilAnchored;

            }



            if (rigRect != null && deathTiltDuration > 0f)

            {

                var tiltRotation = restRotation + new Vector3(0f, 0f, deathTiltDegrees * facingSign);

                var tiltAnchored = homeAnchored + new Vector3(0f, -8f, 0f);

                var tiltStartScale = rig.localScale;





                yield return LerpPlayerPunchPhase(

                    rigRect,

                    rig,

                    homeAnchored,

                    tiltAnchored,

                    tiltStartScale,

                    tiltStartScale * 0.95f,

                    restRotation,

                    tiltRotation,

                    deathTiltDuration,

                    EaseOutQuad,

                    null);



                ResetRigUprightRotation(rig);

            }



            var targetScale = Vector3.one * 0.1f;

            var totalDuration = Mathf.Max(deathShrinkDuration, deathFadeDuration);



            if (totalDuration <= 0f)

            {

                rig.localScale = targetScale;

                if (canvasGroup != null)

                {

                    canvasGroup.alpha = 0f;

                }

                ResetRigUprightRotation(rig);

                yield break;

            }



            var startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            var elapsed = 0f;



            while (elapsed < totalDuration)

            {

                elapsed += Time.deltaTime;



                if (deathShrinkDuration > 0f)

                {

                    var shrinkT = Mathf.Clamp01(elapsed / deathShrinkDuration);

                    rig.localScale = Vector3.Lerp(originalScale, targetScale, shrinkT);

                }

                else

                {

                    rig.localScale = targetScale;

                }



                if (canvasGroup != null)

                {

                    if (deathFadeDuration > 0f)

                    {

                        var fadeT = Mathf.Clamp01(elapsed / deathFadeDuration);

                        canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, fadeT);

                    }

                    else

                    {

                        canvasGroup.alpha = 0f;

                    }

                }



                yield return null;

            }



            rig.localScale = targetScale;

            if (canvasGroup != null)

            {

                canvasGroup.alpha = 0f;

            }

            ResetRigUprightRotation(rig);

        }



        /// <summary>

        /// Sets the enemy rig to entrance-animation start state (off-screen right, alpha 0, scaled down/up).

        /// Called at battle start and after each wave so the enemy stays hidden until the entrance runs.

        /// </summary>

        private void PrepareEnemySpawnStartState()

        {

            var enemyTransform = GetRigTransform(enemyFighterRig);

            var enemyRect = GetRigRectTransform(enemyFighterRig);

            var canvasGroup = GetOrAddCanvasGroup(enemyFighterRig != null ? enemyFighterRig.gameObject : null);

            if (enemyTransform == null)

            {

                return;

            }



            var isBoss = IsCurrentWaveBoss();

            var offsetX = isBoss ? bossEntranceOffsetX : normalEntranceOffsetX;

            var startScaleMultiplier = isBoss ? 1.2f : 0.85f;



            enemyTransform.localScale = enemyOriginalScale * startScaleMultiplier;

            ResetRigUprightRotation(enemyTransform);

            if (canvasGroup != null)

            {

                canvasGroup.alpha = 0f;

            }



            if (enemyRect != null)

            {

                var startPosition = enemyHomePosition + new Vector3(offsetX, 0f, 0f);

                SetRectAnchoredPosition(enemyRect, startPosition);

            }

        }



        /// <summary>

        /// True when the active wave profile marks this wave as a boss wave.

        /// </summary>

        private bool IsCurrentWaveBoss()

        {

            return _currentProgressionProfile != null && _currentProgressionProfile.isBossWave;

        }



        /// <summary>

        /// Normal or boss entrance: slide from off-screen right, fade in, scale to home size, landing bounce.

        /// Boss waves use a farther offset, slower timing, heavier bounce, and optional red backdrop flash.

        /// </summary>

        private IEnumerator PlayEnemyEntranceAnimation()

        {

            var isBoss = IsCurrentWaveBoss();




            StopFighterIdleBreath();



            var enemyRect = GetRigRectTransform(enemyFighterRig);

            var enemyTransform = GetRigTransform(enemyFighterRig);

            var canvasGroup = GetOrAddCanvasGroup(enemyFighterRig != null ? enemyFighterRig.gameObject : null);



            if (enemyTransform == null)

            {


                yield break;

            }



            var duration = isBoss ? bossEntranceDuration : normalEntranceDuration;

            var offsetX = isBoss ? bossEntranceOffsetX : normalEntranceOffsetX;

            var startScaleMultiplier = isBoss ? 1.2f : 0.85f;

            var bounceOvershoot = isBoss ? 1.15f : 1.05f;



            var startScale = enemyOriginalScale * startScaleMultiplier;

            var endScale = enemyOriginalScale;

            var startPosition = enemyHomePosition + new Vector3(offsetX, 0f, 0f);

            var endPosition = enemyHomePosition;



            // Always start from entrance pose — not from death shrink, tilt, or previous alpha.

            ResetRigUprightRotation(enemyTransform);

            enemyTransform.localScale = startScale;

            if (canvasGroup != null)

            {

                canvasGroup.alpha = 0f;

            }



            if (enemyRect != null)

            {

                SetRectAnchoredPosition(enemyRect, startPosition);

            }



            if (duration <= 0f)

            {

                if (enemyRect != null)

                {

                    SetRectAnchoredPosition(enemyRect, endPosition);

                }



                enemyTransform.localScale = endScale;

                if (canvasGroup != null)

                {

                    canvasGroup.alpha = 1f;

                }

                ResetRigUprightRotation(enemyTransform);


                yield break;

            }



            var elapsed = 0f;

            while (elapsed < duration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);

                // Ease out: fast slide-in, gentle settle at home.

                var eased = 1f - (1f - t) * (1f - t);



                if (enemyRect != null)

                {

                    SetRectAnchoredPosition(enemyRect, Vector3.Lerp(startPosition, endPosition, eased));

                }



                enemyTransform.localScale = Vector3.Lerp(startScale, endScale, eased);

                if (canvasGroup != null)

                {

                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

                }



                yield return null;

            }



            if (enemyRect != null)

            {

                SetRectAnchoredPosition(enemyRect, endPosition);

            }



            enemyTransform.localScale = endScale;

            if (canvasGroup != null)

            {

                canvasGroup.alpha = 1f;

            }



            // Landing bounce: quick overshoot then settle (heavier for bosses).

            yield return AnimateEntranceLandingBounce(enemyTransform, endScale, bounceOvershoot, 0.08f);

            ResetRigUprightRotation(enemyTransform);

            if (isBoss)

            {

                yield return FlashBossEntranceBackdrop();

            }




        }



        /// <summary>

        /// Quick scale punch at the end of an entrance (e.g. 1.0 → 1.05 → 1.0 over ~0.08s).

        /// </summary>

        private static IEnumerator AnimateEntranceLandingBounce(

            Transform rig,

            Vector3 baseScale,

            float overshootMultiplier,

            float duration)

        {

            if (rig == null || duration <= 0f)

            {

                yield break;

            }



            var punchScale = baseScale * overshootMultiplier;

            var halfDuration = duration * 0.5f;



            var elapsed = 0f;

            while (elapsed < halfDuration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / halfDuration);

                rig.localScale = Vector3.Lerp(baseScale, punchScale, t);

                yield return null;

            }



            elapsed = 0f;

            while (elapsed < halfDuration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / halfDuration);

                // Sin ease settles the overshoot smoothly back to rest scale.

                var eased = Mathf.Sin(t * Mathf.PI * 0.5f);

                rig.localScale = Vector3.Lerp(punchScale, baseScale, eased);

                yield return null;

            }



            rig.localScale = baseScale;

        }



        /// <summary>

        /// Brief red flash on the enemy slot backdrop when a boss lands. Skips gracefully if no Image is found.

        /// </summary>

        private IEnumerator FlashBossEntranceBackdrop(float flashDuration = 0.15f)

        {

            var backdrop = ResolveBossEntranceFlashBackdrop();

            if (backdrop == null)

            {

                // No EnemyBackdrop in scene — boss entrance still plays without the flash.

                yield break;

            }



            var originalColor = backdrop.color;

            var flashColor = new Color(0.85f, 0.12f, 0.12f, Mathf.Max(originalColor.a, 0.75f));

            backdrop.color = flashColor;



            var elapsed = 0f;

            while (elapsed < flashDuration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / flashDuration);

                backdrop.color = Color.Lerp(flashColor, originalColor, t);

                yield return null;

            }



            backdrop.color = originalColor;

        }



        /// <summary>

        /// Uses the serialized boss flash Image, or finds EnemyBackdrop under the enemy rig's parent slot.

        /// </summary>

        private Image ResolveBossEntranceFlashBackdrop()

        {

            if (bossEntranceFlashBackdrop != null)

            {

                return bossEntranceFlashBackdrop;

            }



            if (enemyFighterRig == null)

            {

                return null;

            }



            var enemySlot = enemyFighterRig.transform.parent;

            if (enemySlot == null)

            {

                return null;

            }



            var backdropTransform = enemySlot.Find("EnemyBackdrop");

            return backdropTransform != null ? backdropTransform.GetComponent<Image>() : null;

        }



        /// <summary>

        /// Returns the root Transform on a FighterRig (used for hit-reaction localPosition shake).

        /// </summary>

        private static Transform GetRigTransform(FighterRig rig)

        {

            return rig != null ? rig.transform : null;

        }



        /// <summary>

        /// Canonical upright local rotation for fighter rigs (facing is handled via localScale.x flip).

        /// </summary>

        private static Vector3 GetRigUprightRotation(Transform rig)

        {

            return Vector3.zero;

        }



        /// <summary>

        /// Snaps a rig root back to upright after presentation overlays (death tilt, punch, recoil).

        /// </summary>

        private static void ResetRigUprightRotation(Transform rig)

        {

            if (rig == null)

            {

                return;

            }



            rig.localEulerAngles = GetRigUprightRotation(rig);

        }



        /// <summary>

        /// Smoothly moves a UI rig's anchoredPosition from start to end over duration seconds.

        /// </summary>

        private static IEnumerator LerpRectAnchoredPosition(

            RectTransform rect,

            Vector3 start,

            Vector3 end,

            float duration)

        {

            if (rect == null)

            {

                yield break;

            }



            if (duration <= 0f)

            {

                SetRectAnchoredPosition(rect, end);

                yield break;

            }



            var elapsed = 0f;

            while (elapsed < duration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);

                SetRectAnchoredPosition(rect, Vector3.Lerp(start, end, t));

                yield return null;

            }



            SetRectAnchoredPosition(rect, end);

        }



        /// <summary>

        /// Returns the root RectTransform on a FighterRig (UI canvas rigs use anchoredPosition).

        /// </summary>

        private static RectTransform GetRigRectTransform(FighterRig rig)

        {

            return rig != null ? rig.transform as RectTransform : null;

        }



        /// <summary>

        /// Reads anchoredPosition as Vector3 (z is always 0 for UI rigs).

        /// </summary>

        private static Vector3 RectAnchoredToVector3(RectTransform rect)

        {

            var anchored = rect.anchoredPosition;

            return new Vector3(anchored.x, anchored.y, 0f);

        }



        /// <summary>

        /// Writes x/y from a Vector3 into RectTransform.anchoredPosition.

        /// </summary>

        private static void SetRectAnchoredPosition(RectTransform rect, Vector3 position)

        {

            rect.anchoredPosition = new Vector2(position.x, position.y);

        }



        /// <summary>

        /// Basic damage formula: attack minus defense, with a minimum of 1.

        /// </summary>

        private static int CalculateDamage(int attack, int defense)

        {

            return Mathf.Max(1, attack - defense);

        }



        /// <summary>

        /// Pre-computed player attack data (skills rolled before the lunge animation).

        /// </summary>

        private struct PlayerAttackOutcome

        {

            public int finalDamage;

            public bool isCritical;

            public SkillResult weaponResult;

            public SkillResult mountResult;

            public EquipmentSkill weaponSkill;

            public EquipmentSkill mountSkill;

        }



        /// <summary>

        /// Rolls weapon/mount skills and final damage without applying HP or raising damage events.

        /// Called before the attack lunge so crit can change lunge distance.

        /// </summary>

        private PlayerAttackOutcome ComputePlayerAttackOutcome()

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



            var outcome = new PlayerAttackOutcome

            {

                weaponResult = weaponResult,

                weaponSkill = weaponSkill,

                finalDamage = weaponResult.modifiedDamage,

                isCritical = weaponResult.isCritical

            };



            var mountSkill = _playerFighter.mount?.skill;

            if (mountSkill != null && mountSkill.skillType != SkillType.None)

            {

                var mountResult = _skillEngine.ApplyPlayerAttackSkills(outcome.finalDamage, mountSkill, context);

                outcome.mountResult = mountResult;

                outcome.mountSkill = mountSkill;

                outcome.finalDamage = mountResult.modifiedDamage;

            }



            return outcome;

        }



        /// <summary>

        /// Applies a pre-computed player attack: skill events, damage, and life steal.

        /// </summary>

        private void ApplyPlayerAttackOutcome(PlayerAttackOutcome outcome)

        {

            var anySkillTriggered = false;



            if (ApplyPlayerAttackSkillEffects(outcome.weaponResult, outcome.weaponSkill))

            {

                anySkillTriggered = true;

            }



            if (outcome.mountResult != null)

            {

                if (ApplyPlayerAttackSkillEffects(outcome.mountResult, outcome.mountSkill))

                {

                    anySkillTriggered = true;

                }

            }



            _enemyFighter.TakeDamage(outcome.finalDamage);

            UpdateEnemyHealthDisplay();



            var damageSuffix = anySkillTriggered ? "!" : ".";

            RaiseCombatEvent(

                CombatEventType.DamageDealt,

                $"Player dealt {outcome.finalDamage} damage{damageSuffix}",

                sourceName: _playerFighter.fighterName,

                targetName: _enemyFighter.enemyName,

                amount: outcome.finalDamage,

                isCritical: outcome.isCritical);



            ApplyLifeStealFromResult(outcome.weaponResult, outcome.weaponSkill, outcome.finalDamage);



            if (outcome.mountResult != null)

            {

                ApplyLifeStealFromResult(outcome.mountResult, outcome.mountSkill, outcome.finalDamage);

            }

        }



        /// <summary>

        /// Player attack: base damage, weapon skill via SkillEngine, optional mount skill (poison etc.),

        /// then apply to enemy.

        /// </summary>

        private void ApplyPlayerAttackDamage()

        {

            ApplyPlayerAttackOutcome(ComputePlayerAttackOutcome());

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
            if (_combatPaused || !_enemyIsPoisoned || _enemyPoisonTurnsRemaining <= 0)
            {
                return;
            }

            _enemyFighter.TakeDamage(_enemyPoisonDamage);
            UpdateEnemyHealthDisplay();

            if (enemyFighterRig != null)
            {
                enemyFighterRig.FlashDamage();
            }

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
            if (_combatPaused || !_enemyIsBurning || _enemyBurnTurnsRemaining <= 0)
            {
                return;
            }

            _enemyFighter.TakeDamage(_enemyBurnDamage);
            UpdateEnemyHealthDisplay();

            if (enemyFighterRig != null)
            {
                enemyFighterRig.FlashDamage();
            }

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



            if (waveBannerUI == null)

            {

                var bannerObject = GameObject.Find("WaveBannerPanel");

                if (bannerObject != null)

                {

                    waveBannerUI = bannerObject.GetComponent<WaveBannerUI>();

                }

            }



            if (waveBannerUI == null)

            {

                waveBannerUI = FindObjectOfType<WaveBannerUI>();

            }



            if (waveBannerUI != null && waveBannerUI.gameObject.scene != gameObject.scene)

            {

                waveBannerUI = null;

            }



            if (waveBannerUI == null)

            {

                Debug.LogWarning("WaveBannerUI not found in BattleScene.");

            }



            if (rewardScreenUI == null)

            {

                rewardScreenUI = FindObjectOfType<RewardScreenUI>();

            }



            if (rewardScreenUI != null && rewardScreenUI.gameObject.scene != gameObject.scene)

            {

                rewardScreenUI = null;

            }



            if (rewardScreenUI == null)

            {

                Debug.LogWarning("RewardScreenUI not found in BattleScene.");

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



            enemyFighterRig.Display(ResolveEnemyDisplayFighter());

            enemyFighterRig.SetFacing(false);

            ResetRigUprightRotation(GetRigTransform(enemyFighterRig));

            enemyFighterRig.SetColorTint(enemyTint);

        }



        /// <summary>

        /// Builds the PlayerFighter snapshot used only for enemy rig visuals.

        /// Mirrors player gear when enabled, or when factory lists left the enemy with no icons.

        /// </summary>

        private PlayerFighter ResolveEnemyDisplayFighter()

        {

            var display = _enemyFighter.ToDisplayFighter();

            if (_playerFighter == null)

            {

                return display;

            }



            if (enemyMirrorPlayerAppearance || !EnemyHasVisibleEquipment(display))

            {

                display.head = _playerFighter.head ?? display.head;

                display.body = _playerFighter.body ?? display.body;

                display.weapon = _playerFighter.weapon ?? display.weapon;

                display.mount = _playerFighter.mount ?? display.mount;

            }



            return display;

        }



        private static bool EnemyHasVisibleEquipment(PlayerFighter display)

        {

            if (display == null)

            {

                return false;

            }



            return display.head?.icon != null

                   || display.body?.icon != null

                   || display.weapon?.icon != null

                   || display.mount?.icon != null;

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

            bool? isBoss = null,

            bool isCritical = false)

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

                isBoss = isBoss ?? (_currentProgressionProfile?.isBossWave ?? false),

                isCritical = isCritical

            });

        }



        /// <summary>

        /// v1 display-only rewards: Gold = 25 + wave * 5, XP = 12 + wave * 3.

        /// </summary>

        private static int CalculateWaveGoldReward(int waveNumber)

        {

            return 25 + waveNumber * 5;

        }



        /// <summary>

        /// v1 display-only rewards: Gold = 25 + wave * 5, XP = 12 + wave * 3.

        /// </summary>

        private static int CalculateWaveXpReward(int waveNumber)

        {

            return 12 + waveNumber * 3;

        }



        /// <summary>

        /// Shows the post-wave reward overlay and waits for Continue. No-op when RewardScreenUI is missing.

        /// </summary>

        private IEnumerator ShowRewardScreenForClearedWave()

        {

            StopFighterIdleBreath();



            var goldAmount = CalculateWaveGoldReward(_waveNumber);

            var xpAmount = CalculateWaveXpReward(_waveNumber);



            if (rewardScreenUI == null)

            {

                yield break;

            }



            rewardScreenUI.ShowReward(_waveNumber, goldAmount, xpAmount);

            yield return rewardScreenUI.WaitForContinue();

            rewardScreenUI.Hide();

        }



        /// <summary>

        /// Serialized wave intro: optional WaveStarted notify, banner wait, entrance wait, post-entrance pause.

        /// Combat must not resume until this coroutine completes.

        /// </summary>

        private IEnumerator RunWavePresentationSequence(bool notifyWaveStarted, bool logWaveTransition)

        {

            _combatPaused = true;

            StopFighterIdleBreath();

            if (logWaveTransition)

            {

                Debug.Log("Wave Transition Started");

            }



            if (notifyWaveStarted)

            {

                NotifyWaveStarted();

            }



            if (waveBannerUI != null && _enemyFighter != null)

            {

                var isBoss = _currentProgressionProfile != null && _currentProgressionProfile.isBossWave;

                yield return waveBannerUI.ShowWaveBanner(_waveNumber, _enemyFighter.enemyName, isBoss);

            }

            else

            {

                if (waveBannerUI == null)

                {

                    Debug.LogWarning("WaveBannerUI missing — waiting default banner duration.");

                }

                yield return new WaitForSeconds(1.25f);

            }



            yield return PlayEnemyEntranceAnimation();

            yield return new WaitForSeconds(waveTransitionPostEntranceDelay);



            if (logWaveTransition)

            {

                Debug.Log("Wave Transition Complete - Combat Resuming");

            }



            _combatPaused = false;

            StartFighterIdleBreath();

        }



        /// <summary>

        /// <summary>

        /// Fires WaveStarted and BossStarted (when applicable) before the wave intro banner.

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


