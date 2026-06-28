using System.IO;
using Crownsfall.Combat;
using Crownsfall.Combat.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crownsfall.Editor
{
    /// <summary>
    /// Editor menus that create the Battle scene hierarchy for beginners.
    /// v1: fighters + camera only. v2: adds mobile portrait HUD overlay (Canvas + stat cards + log).
    /// </summary>
    public static class BattleSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/BattleScene/BattleScene.unity";

        // Match BattleManager defaults so the scene looks correct before Play mode too.
        private static readonly Vector3 PlayerPosition = new Vector3(-0.85f, -1.15f, 0f);
        private static readonly Vector3 EnemyPosition = new Vector3(0.85f, -1.15f, 0f);
        private static readonly Vector3 FighterScale = new Vector3(0.34f, 0.34f, 0.34f);
        private static readonly Vector3 CameraPosition = new Vector3(0f, 0f, -10f);
        private const float CameraOrthographicSize = 3.5f;

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        // Dark blue-gray background (#1a1a2e).
        private static readonly Color BackgroundColor = new Color(0.102f, 0.102f, 0.180f, 1f);

        // Production UI layout tuned for mobile portrait (e.g. Samsung Galaxy S10e @ 1080×2280).
        // CanvasScaler reference is 1080×1920; anchors keep content inside safe areas.
        // Fighter rig + layers are ~25% larger than the original 320×420 layout.
        private static readonly Vector2 FighterRigSize = new Vector2(400f, 525f); // 320×420 × 1.25
        private const float LayerMountBodySize = 350f;
        private const float LayerWeaponSize = 250f;
        private const float LayerHeadSize = 275f;
        private const float LayerCrownSize = 200f;
        private const float BattleLogFontSize = 30f;
        private const float StatsFontSize = 26f;
        private const float HealthBarLabelFontSize = 22f;
        private const float HealthBarWidth = 300f;
        private const float HealthBarHeight = 32f;
        private const float BottomBarHeight = 240f;
        private const float BottomBarPadding = 32f;

        [MenuItem("Tools/Fighter Tools/Setup Battle Scene")]
        public static void SetupSceneFromMenu()
        {
            SetupScene(includeUi: false);
            EditorUtility.DisplayDialog(
                "Battle Scene",
                "Battle scene (v1) created at:\n" + ScenePath,
                "OK");
        }

        [MenuItem("Tools/Fighter Tools/Setup Battle Scene v2")]
        public static void SetupSceneV2FromMenu()
        {
            SetupScene(includeUi: true);
            EditorUtility.DisplayDialog(
                "Battle Scene v2",
                "Battle scene v2 created at:\n" + ScenePath +
                "\n\nIncludes world-space fighters + HUD Canvas.\nPress Play to test.",
                "OK");
        }

        [MenuItem("Tools/Fighter Tools/Setup Battle Scene Production UI")]
        public static void SetupSceneProductionUiFromMenu()
        {
            SetupSceneProductionUi();
            EditorUtility.DisplayDialog(
                "Battle Scene Production UI",
                "Production battle scene saved at:\n" + ScenePath +
                "\n\nUI-only fighters on Canvas + BattleHUD.\nPress Play to test.",
                "OK");
        }

        /// <summary>
        /// Updates layout on an already-open Battle scene without rebuilding from scratch.
        /// Run this after tweaking layout constants, or use Setup Battle Scene Production UI for a full rebuild.
        /// </summary>
        [MenuItem("Tools/Fighter Tools/Polish Battle Scene UI")]
        public static void PolishBattleSceneUiFromMenu()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Polish Battle Scene UI",
                    "Battle scene not found at:\n" + ScenePath +
                    "\n\nRun Setup Battle Scene Production UI first.",
                    "OK");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var updated = PolishExistingProductionUi();

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Polish Battle Scene UI",
                updated
                    ? "Battle scene UI polished and saved.\nPress Play to preview on mobile portrait."
                    : "Could not find production UI objects in the scene.\nRun Setup Battle Scene Production UI instead.",
                "OK");
        }

        /// <summary>
        /// Creates the scene, camera, BattleManager, fighter views, and optional v2 UI.
        /// v2 / includeUi now delegates to the production UI layout.
        /// </summary>
        public static void SetupScene(bool includeUi = false)
        {
            if (includeUi)
            {
                SetupSceneProductionUi();
                return;
            }

            EnsureSceneFolderExists();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            var battleManagerObject = CreateBattleManagerObject();
            var battleManager = battleManagerObject.GetComponent<BattleManager>();

            var playerView = CreateFighterView("PlayerFighterView", PlayerPosition);
            var enemyView = CreateFighterView("EnemyFighterView", EnemyPosition);

            BattleUIController uiController = null;
            if (includeUi)
            {
                uiController = CreateBattleUiCanvas();
            }

            WireBattleManager(battleManager, playerView, enemyView, uiController);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(includeUi
                ? $"Battle scene v2 saved to {ScenePath}"
                : $"Battle scene saved to {ScenePath}");
        }

        private static void EnsureSceneFolderExists()
        {
            var folder = Path.GetDirectoryName(ScenePath);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "BattleScene");
            }
        }

        /// <summary>
        /// Orthographic camera for 2D world-space fighters. Size 3.5 ≈ 7 units tall on screen.
        /// </summary>
        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            camera.transform.position = CameraPosition;

            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateBattleManagerObject()
        {
            return new GameObject("BattleManager", typeof(BattleManager));
        }

        /// <summary>
        /// Creates a fighter view with four named sprite layers (MountLayer → HeadLayer).
        /// </summary>
        private static BattleFighterView CreateFighterView(string objectName, Vector3 position)
        {
            var root = new GameObject(objectName);
            root.transform.position = position;
            root.transform.localScale = FighterScale;

            var view = root.AddComponent<BattleFighterView>();

            var mountLayer = CreateSpriteLayer(root.transform, "MountLayer", 0);
            var bodyLayer = CreateSpriteLayer(root.transform, "BodyLayer", 1);
            var weaponLayer = CreateSpriteLayer(root.transform, "WeaponLayer", 2);
            var headLayer = CreateSpriteLayer(root.transform, "HeadLayer", 3);

            WireFighterView(view, mountLayer, bodyLayer, weaponLayer, headLayer);

            return view;
        }

        /// <summary>
        /// One child GameObject with a SpriteRenderer for a single equipment layer.
        /// </summary>
        private static Transform CreateSpriteLayer(Transform parent, string layerName, int sortingOrder)
        {
            var layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.localPosition = Vector3.zero;

            var renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return layerObject.transform;
        }

        private static void WireFighterView(
            BattleFighterView view,
            Transform mountLayer,
            Transform bodyLayer,
            Transform weaponLayer,
            Transform headLayer)
        {
            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("mountLayer").objectReferenceValue = mountLayer;
            serializedView.FindProperty("bodyLayer").objectReferenceValue = bodyLayer;
            serializedView.FindProperty("weaponLayer").objectReferenceValue = weaponLayer;
            serializedView.FindProperty("headLayer").objectReferenceValue = headLayer;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        /// <summary>
        /// Builds Screen Space Overlay Canvas with stat cards, VS label, and battle log.
        /// </summary>
        private static BattleUIController CreateBattleUiCanvas()
        {
            var canvasObject = new GameObject("BattleCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasObject.GetComponent<RectTransform>();

            CreateBackground(canvasRect);
            var vsText = CreateVsText(canvasRect);
            var playerCard = CreateStatCard(canvasRect, "PlayerStatCard", true, out var playerTexts);
            var enemyCard = CreateStatCard(canvasRect, "EnemyStatCard", false, out var enemyTexts);
            var logText = CreateBattleLogPanel(canvasRect);

            var uiController = canvasObject.AddComponent<BattleUIController>();
            WireBattleUiController(
                uiController,
                vsText,
                playerTexts,
                enemyTexts,
                logText);

            return uiController;
        }

        private static void CreateBackground(RectTransform canvasRect)
        {
            var background = CreateRect("Background", canvasRect);
            StretchToParent(background);

            var image = background.gameObject.AddComponent<Image>();
            image.color = BackgroundColor;
            image.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateVsText(RectTransform canvasRect)
        {
            var vsRect = CreateRect("VsText", canvasRect);
            vsRect.anchorMin = new Vector2(0.5f, 0.5f);
            vsRect.anchorMax = new Vector2(0.5f, 0.5f);
            vsRect.pivot = new Vector2(0.5f, 0.5f);
            vsRect.anchoredPosition = new Vector2(0f, 120f);
            vsRect.sizeDelta = new Vector2(320f, 160f);

            var vsText = CreateTmpText(vsRect, "VS", 96f, FontStyles.Bold);
            vsText.alignment = TextAlignmentOptions.Center;
            vsText.color = new Color(0.85f, 0.85f, 0.92f, 0.35f);
            return vsText;
        }

        /// <summary>
        /// Stat card panel anchored to bottom-left (player) or bottom-right (enemy).
        /// </summary>
        private static RectTransform CreateStatCard(
            RectTransform canvasRect,
            string cardName,
            bool isPlayer,
            out StatCardTexts texts)
        {
            var card = CreateRect(cardName, canvasRect);
            card.sizeDelta = new Vector2(420f, 300f);
            card.pivot = new Vector2(isPlayer ? 0f : 1f, 0f);

            if (isPlayer)
            {
                card.anchorMin = new Vector2(0f, 0f);
                card.anchorMax = new Vector2(0f, 0f);
                card.anchoredPosition = new Vector2(36f, 36f);
            }
            else
            {
                card.anchorMin = new Vector2(1f, 0f);
                card.anchorMax = new Vector2(1f, 0f);
                card.anchoredPosition = new Vector2(-36f, 36f);
            }

            var panelImage = card.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.14f, 0.92f);
            panelImage.raycastTarget = false;

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            texts = new StatCardTexts
            {
                NameText = CreateStatLine(card, "NameText", "Fighter", 36f, FontStyles.Bold),
                AttackText = CreateStatLine(card, "AttackText", "ATK 0", 28f, FontStyles.Normal),
                DefenseText = CreateStatLine(card, "DefenseText", "DEF 0", 28f, FontStyles.Normal),
                SpeedText = CreateStatLine(card, "SpeedText", "SPD 0", 28f, FontStyles.Normal),
                HealthText = CreateStatLine(card, "HealthText", "HP 0", 28f, FontStyles.Normal)
            };

            return card;
        }

        private static TextMeshProUGUI CreateStatLine(
            RectTransform parent,
            string name,
            string defaultText,
            float fontSize,
            FontStyles style)
        {
            var line = CreateTmpText(parent, defaultText, fontSize, style);
            line.gameObject.name = name;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = fontSize + 12f;
            return line;
        }

        private static TextMeshProUGUI CreateBattleLogPanel(RectTransform canvasRect)
        {
            var panel = CreateRect("BattleLogPanel", canvasRect);
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 360f);
            panel.sizeDelta = new Vector2(900f, 280f);

            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.07f, 0.11f, 0.88f);
            panelImage.raycastTarget = false;

            var logTextRect = CreateRect("BattleLogText", panel);
            StretchToParent(logTextRect, new Vector2(20f, 16f), new Vector2(-20f, -16f));

            var logText = CreateTmpText(logTextRect, string.Empty, 26f, FontStyles.Normal);
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.enableWordWrapping = true;
            logText.overflowMode = TextOverflowModes.Truncate;
            logText.color = new Color(0.82f, 0.84f, 0.9f, 1f);
            logText.gameObject.name = "BattleLogText";

            return logText;
        }

        private static void WireBattleUiController(
            BattleUIController controller,
            TextMeshProUGUI vsText,
            StatCardTexts playerTexts,
            StatCardTexts enemyTexts,
            TextMeshProUGUI logText)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("playerNameText").objectReferenceValue = playerTexts.NameText;
            serialized.FindProperty("playerAttackText").objectReferenceValue = playerTexts.AttackText;
            serialized.FindProperty("playerDefenseText").objectReferenceValue = playerTexts.DefenseText;
            serialized.FindProperty("playerSpeedText").objectReferenceValue = playerTexts.SpeedText;
            serialized.FindProperty("playerHealthText").objectReferenceValue = playerTexts.HealthText;

            serialized.FindProperty("enemyNameText").objectReferenceValue = enemyTexts.NameText;
            serialized.FindProperty("enemyAttackText").objectReferenceValue = enemyTexts.AttackText;
            serialized.FindProperty("enemyDefenseText").objectReferenceValue = enemyTexts.DefenseText;
            serialized.FindProperty("enemySpeedText").objectReferenceValue = enemyTexts.SpeedText;
            serialized.FindProperty("enemyHealthText").objectReferenceValue = enemyTexts.HealthText;

            serialized.FindProperty("vsText").objectReferenceValue = vsText;
            serialized.FindProperty("battleLogText").objectReferenceValue = logText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        /// <summary>
        /// Legacy v1 wiring — BattleManager now uses FighterRig + BattleHUD (production UI).
        /// </summary>
        private static void WireBattleManager(
            BattleManager manager,
            BattleFighterView playerView,
            BattleFighterView enemyView,
            BattleUIController uiController)
        {
            EditorUtility.SetDirty(manager);
            Debug.LogWarning(
                "Battle scene v1 uses legacy SpriteRenderer views. " +
                "Use Tools → Fighter Tools → Setup Battle Scene Production UI for the current BattleManager.");
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI CreateTmpText(
            Transform parent,
            string text,
            float fontSize,
            FontStyles fontStyle)
        {
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = GetDefaultTmpFont();
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static TMP_FontAsset GetDefaultTmpFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            if (font == null)
            {
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            return font;
        }

        private static void StretchToParent(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Helper holder for the five TMP fields on one stat card.
        /// </summary>
        private struct StatCardTexts
        {
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI AttackText;
            public TextMeshProUGUI DefenseText;
            public TextMeshProUGUI SpeedText;
            public TextMeshProUGUI HealthText;
        }

        /// <summary>
        /// Serialized references for the game over overlay wired into BattleHUD.
        /// </summary>
        private struct GameOverPanelRefs
        {
            public GameObject Panel;
            public TextMeshProUGUI TitleText;
            public TextMeshProUGUI FighterNameText;
            public TextMeshProUGUI FinalScoreText;
            public TextMeshProUGUI WaveReachedText;
            public TextMeshProUGUI EnemiesDefeatedText;
            public TextMeshProUGUI HighestWaveText;
            public TextMeshProUGUI BestScoreText;
            public TextMeshProUGUI AllTimeHighestWaveText;
            public Button PlayAgainButton;
            public Button CharacterBuilderButton;
        }

        // --- Production UI (Canvas + FighterRig + BattleHUD) ---

        /// <summary>
        /// Creates the production mobile battle scene: Canvas hierarchy, UI fighter rigs, BattleHUD.
        /// No SpriteRenderer fighters — everything is UI Image based.
        /// </summary>
        public static void SetupSceneProductionUi()
        {
            EnsureSceneFolderExists();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            var battleManagerObject = CreateBattleManagerObject();
            var battleManager = battleManagerObject.GetComponent<BattleManager>();

            var battleHud = CreateProductionBattleCanvas(out var playerRig, out var enemyRig);

            WireBattleManagerProduction(battleManager, playerRig, enemyRig, battleHud);
            EnsureFloatingCombatTextSpawner(battleManagerObject, playerRig, enemyRig);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Production battle scene saved to {ScenePath}");
        }

        /// <summary>
        /// Builds the full Canvas → BattleHUD hierarchy for the production mobile layout.
        /// </summary>
        private static BattleHUD CreateProductionBattleCanvas(out FighterRig playerRig, out FighterRig enemyRig)
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            CreateBackground(canvasRect);

            var hudRoot = CreateRect("BattleHUD", canvasRect);
            StretchToParent(hudRoot);

            var battleHud = hudRoot.gameObject.AddComponent<BattleHUD>();

            CreateTopBar(hudRoot, out var titleText, out var waveText, out var scoreText);
            CreateBattleArea(hudRoot, out playerRig, out enemyRig, out _,
                out var playerHealthFill, out var enemyHealthFill);
            CreateProductionBattleLogPanel(hudRoot, out var battleLogText);
            CreateBottomStatsBar(hudRoot, out var playerStatsText, out var enemyStatsText);
            var gameOverRefs = CreateGameOverPanel(hudRoot);

            WireBattleHud(
                battleHud,
                titleText,
                waveText,
                scoreText,
                battleLogText,
                playerStatsText,
                enemyStatsText,
                playerHealthFill,
                enemyHealthFill,
                gameOverRefs);

            return battleHud;
        }

        private static RectTransform CreateTopBar(
            RectTransform parent,
            out TextMeshProUGUI titleText,
            out TextMeshProUGUI waveText,
            out TextMeshProUGUI scoreText)
        {
            var topBar = CreateRect("TopBar", parent);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.anchoredPosition = Vector2.zero;
            topBar.sizeDelta = new Vector2(0f, 140f);

            var barImage = topBar.gameObject.AddComponent<Image>();
            barImage.color = new Color(0.06f, 0.07f, 0.12f, 0.95f);
            barImage.raycastTarget = false;

            titleText = CreateNamedTmpText(topBar, "TitleText", "CROWNSFALL", 52f, FontStyles.Bold);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(600f, 80f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.92f, 0.88f, 0.72f, 1f);

            waveText = CreateNamedTmpText(topBar, "WaveText", "Wave 1", 32f, FontStyles.Normal);
            var waveRect = waveText.rectTransform;
            waveRect.anchorMin = new Vector2(0f, 0.5f);
            waveRect.anchorMax = new Vector2(0f, 0.5f);
            waveRect.pivot = new Vector2(0f, 0.5f);
            waveRect.anchoredPosition = new Vector2(36f, 0f);
            waveRect.sizeDelta = new Vector2(240f, 60f);
            waveText.alignment = TextAlignmentOptions.MidlineLeft;

            scoreText = CreateNamedTmpText(topBar, "ScoreText", "Score: 0", 32f, FontStyles.Normal);
            var scoreRect = scoreText.rectTransform;
            scoreRect.anchorMin = new Vector2(1f, 0.5f);
            scoreRect.anchorMax = new Vector2(1f, 0.5f);
            scoreRect.pivot = new Vector2(1f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(-36f, 0f);
            scoreRect.sizeDelta = new Vector2(280f, 60f);
            scoreText.alignment = TextAlignmentOptions.MidlineRight;

            return topBar;
        }

        private static RectTransform CreateBattleArea(
            RectTransform parent,
            out FighterRig playerRig,
            out FighterRig enemyRig,
            out TextMeshProUGUI vsText,
            out Image playerHealthFill,
            out Image enemyHealthFill)
        {
            var battleArea = CreateRect("BattleArea", parent);
            battleArea.anchorMin = new Vector2(0f, 0.28f);
            battleArea.anchorMax = new Vector2(1f, 0.78f);
            battleArea.offsetMin = new Vector2(24f, 0f);
            battleArea.offsetMax = new Vector2(-24f, 0f);
            battleArea.pivot = new Vector2(0.5f, 0.5f);

            var playerSlot = CreateRect("PlayerSlot", battleArea);
            playerSlot.anchorMin = new Vector2(0f, 0f);
            playerSlot.anchorMax = new Vector2(0.42f, 1f);
            playerSlot.offsetMin = Vector2.zero;
            playerSlot.offsetMax = Vector2.zero;

            playerRig = CreateFighterRig(playerSlot, "PlayerFighterRig", isPlayer: true);
            playerHealthFill = CreateHealthBar(playerSlot, "PlayerHealthBar",
                new Color(0.25f, 0.75f, 0.35f, 1f), "Player HP");

            vsText = CreateNamedTmpText(battleArea, "VSText", "VS", 88f, FontStyles.Bold);
            var vsRect = vsText.rectTransform;
            vsRect.anchorMin = new Vector2(0.5f, 0.5f);
            vsRect.anchorMax = new Vector2(0.5f, 0.5f);
            vsRect.pivot = new Vector2(0.5f, 0.5f);
            vsRect.anchoredPosition = new Vector2(0f, 40f);
            vsRect.sizeDelta = new Vector2(200f, 140f);
            vsText.alignment = TextAlignmentOptions.Center;
            vsText.color = new Color(0.85f, 0.85f, 0.92f, 0.35f);

            var enemySlot = CreateRect("EnemySlot", battleArea);
            enemySlot.anchorMin = new Vector2(0.58f, 0f);
            enemySlot.anchorMax = new Vector2(1f, 1f);
            enemySlot.offsetMin = Vector2.zero;
            enemySlot.offsetMax = Vector2.zero;

            CreateEnemyBackdrop(enemySlot);

            enemyRig = CreateFighterRig(enemySlot, "EnemyFighterRig", isPlayer: false);
            enemyHealthFill = CreateHealthBar(enemySlot, "EnemyHealthBar",
                new Color(0.85f, 0.28f, 0.28f, 1f), "Enemy HP");

            return battleArea;
        }

        /// <summary>
        /// Creates a FighterRig root with anchor → Image hierarchy (see FighterRig.cs comments).
        /// Player sits slightly lower-left; enemy slightly lower-right inside each slot.
        /// </summary>
        private static FighterRig CreateFighterRig(RectTransform slot, string rigName, bool isPlayer)
        {
            var rigRect = CreateRect(rigName, slot);

            // Anchor toward the lower corner so fighters feel grounded on mobile portrait.
            var anchorX = isPlayer ? 0.32f : 0.68f;
            rigRect.anchorMin = new Vector2(anchorX, 0.22f);
            rigRect.anchorMax = new Vector2(anchorX, 0.22f);
            rigRect.pivot = new Vector2(0.5f, 0.5f);
            rigRect.anchoredPosition = isPlayer ? new Vector2(-12f, 8f) : new Vector2(12f, 8f);
            rigRect.sizeDelta = FighterRigSize;

            var rig = rigRect.gameObject.AddComponent<FighterRig>();

            var mountAnchor = CreateEquipmentAnchor(rigRect, "MountAnchor");
            var bodyAnchor = CreateEquipmentAnchor(rigRect, "BodyAnchor");
            var weaponAnchor = CreateEquipmentAnchor(rigRect, "WeaponAnchor");
            var headAnchor = CreateEquipmentAnchor(rigRect, "HeadAnchor");
            var crownAnchor = CreateEquipmentAnchor(rigRect, "CrownAnchor");

            // Future UI attachment points — empty RectTransforms for now.
            var damageAnchor = CreateFutureAnchor(rigRect, "DamageAnchor");
            var healthBarAnchor = CreateFutureAnchor(rigRect, "HealthBarAnchor");
            var nameAnchor = CreateFutureAnchor(rigRect, "NameAnchor");

            var mountImage = CreateLayerImage(mountAnchor, "MountImage", LayerMountBodySize);
            var bodyImage = CreateLayerImage(bodyAnchor, "BodyImage", LayerMountBodySize);
            var weaponImage = CreateLayerImage(weaponAnchor, "WeaponImage", LayerWeaponSize);
            var headImage = CreateLayerImage(headAnchor, "HeadImage", LayerHeadSize);
            var crownImage = CreateLayerImage(crownAnchor, "CrownImage", LayerCrownSize);

            WireFighterRig(
                rig,
                mountAnchor,
                bodyAnchor,
                weaponAnchor,
                headAnchor,
                crownAnchor,
                mountImage,
                bodyImage,
                weaponImage,
                headImage,
                crownImage,
                damageAnchor,
                healthBarAnchor,
                nameAnchor);

            return rig;
        }

        /// <summary>
        /// Empty anchor for one equipment layer. FighterRig.ApplyOffsets() moves this RectTransform.
        /// </summary>
        private static RectTransform CreateEquipmentAnchor(RectTransform parent, string anchorName)
        {
            var anchor = CreateRect(anchorName, parent);
            anchor.anchorMin = new Vector2(0.5f, 0.5f);
            anchor.anchorMax = new Vector2(0.5f, 0.5f);
            anchor.pivot = new Vector2(0.5f, 0.5f);
            anchor.anchoredPosition = Vector2.zero;
            anchor.sizeDelta = Vector2.zero;
            return anchor;
        }

        /// <summary>
        /// Empty anchor reserved for future HUD elements on the rig (damage, health bar, name).
        /// </summary>
        private static RectTransform CreateFutureAnchor(RectTransform parent, string anchorName)
        {
            var anchor = CreateEquipmentAnchor(parent, anchorName);
            anchor.SetAsLastSibling();
            return anchor;
        }

        /// <summary>
        /// Subtle red panel behind the enemy rig so the tinted mirror sprite stands out.
        /// </summary>
        private static void CreateEnemyBackdrop(RectTransform enemySlot)
        {
            var backdrop = CreateRect("EnemyBackdrop", enemySlot);
            backdrop.SetAsFirstSibling();

            backdrop.anchorMin = new Vector2(0.68f, 0.22f);
            backdrop.anchorMax = new Vector2(0.68f, 0.22f);
            backdrop.pivot = new Vector2(0.5f, 0.5f);
            backdrop.anchoredPosition = new Vector2(12f, 8f);
            backdrop.sizeDelta = new Vector2(FighterRigSize.x + 40f, FighterRigSize.y + 40f);

            var image = backdrop.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.08f, 0.1f, 0.55f);
            image.raycastTarget = false;
        }

        private static Image CreateLayerImage(RectTransform parent, string layerName, float size)
        {
            var layerRect = CreateRect(layerName, parent);
            layerRect.anchorMin = new Vector2(0.5f, 0.5f);
            layerRect.anchorMax = new Vector2(0.5f, 0.5f);
            layerRect.pivot = new Vector2(0.5f, 0.5f);
            layerRect.anchoredPosition = Vector2.zero;
            layerRect.sizeDelta = new Vector2(size, size);

            var image = layerRect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            return image;
        }

        private static void WireFighterRig(
            FighterRig rig,
            RectTransform mountAnchor,
            RectTransform bodyAnchor,
            RectTransform weaponAnchor,
            RectTransform headAnchor,
            RectTransform crownAnchor,
            Image mountImage,
            Image bodyImage,
            Image weaponImage,
            Image headImage,
            Image crownImage,
            RectTransform damageAnchor,
            RectTransform healthBarAnchor,
            RectTransform nameAnchor)
        {
            var serialized = new SerializedObject(rig);
            serialized.FindProperty("mountAnchor").objectReferenceValue = mountAnchor;
            serialized.FindProperty("bodyAnchor").objectReferenceValue = bodyAnchor;
            serialized.FindProperty("weaponAnchor").objectReferenceValue = weaponAnchor;
            serialized.FindProperty("headAnchor").objectReferenceValue = headAnchor;
            serialized.FindProperty("crownAnchor").objectReferenceValue = crownAnchor;
            serialized.FindProperty("mountImage").objectReferenceValue = mountImage;
            serialized.FindProperty("bodyImage").objectReferenceValue = bodyImage;
            serialized.FindProperty("weaponImage").objectReferenceValue = weaponImage;
            serialized.FindProperty("headImage").objectReferenceValue = headImage;
            serialized.FindProperty("crownImage").objectReferenceValue = crownImage;
            serialized.FindProperty("damageAnchor").objectReferenceValue = damageAnchor;
            serialized.FindProperty("healthBarAnchor").objectReferenceValue = healthBarAnchor;
            serialized.FindProperty("nameAnchor").objectReferenceValue = nameAnchor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rig);
        }

        /// <summary>
        /// Health bar anchored to the bottom of a fighter slot, with a label above it.
        /// Returns the fill Image for BattleHUD.
        /// </summary>
        private static Image CreateHealthBar(RectTransform slot, string barName, Color fillColor, string labelText)
        {
            var barRect = CreateRect(barName, slot);
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 28f);
            barRect.sizeDelta = new Vector2(HealthBarWidth, HealthBarHeight);

            // Static label above the bar (BattleHUD only drives the fill amount).
            var label = CreateNamedTmpText(slot, barName + "Label", labelText,
                HealthBarLabelFontSize, FontStyles.Bold);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 28f + HealthBarHeight + 6f);
            labelRect.sizeDelta = new Vector2(HealthBarWidth, 28f);
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.88f, 0.9f, 0.95f, 1f);

            var background = barRect.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.13f, 0.18f, 1f);
            background.raycastTarget = false;

            var fillRect = CreateRect("Fill", barRect);
            StretchToParent(fillRect, new Vector2(4f, 4f), new Vector2(-4f, -4f));

            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            return fill;
        }

        private static RectTransform CreateProductionBattleLogPanel(
            RectTransform parent,
            out TextMeshProUGUI battleLogText)
        {
            var panel = CreateRect("BattleLogPanel", parent);
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, BottomBarHeight + 16f);
            panel.sizeDelta = new Vector2(960f, 280f);

            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.06f, 0.07f, 0.11f, 0.88f);
            panelImage.raycastTarget = false;

            var logTextRect = CreateRect("BattleLogText", panel);
            StretchToParent(logTextRect, new Vector2(20f, 16f), new Vector2(-20f, -16f));

            battleLogText = CreateTmpText(logTextRect, string.Empty, BattleLogFontSize, FontStyles.Normal);
            battleLogText.gameObject.name = "BattleLogText";
            battleLogText.alignment = TextAlignmentOptions.TopLeft;
            battleLogText.enableWordWrapping = true;
            battleLogText.overflowMode = TextOverflowModes.Truncate;
            battleLogText.color = new Color(0.82f, 0.84f, 0.9f, 1f);

            return panel;
        }

        private static RectTransform CreateBottomStatsBar(
            RectTransform parent,
            out TextMeshProUGUI playerStatsText,
            out TextMeshProUGUI enemyStatsText)
        {
            var bottomBar = CreateRect("BottomStatsBar", parent);
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.anchoredPosition = Vector2.zero;
            bottomBar.sizeDelta = new Vector2(0f, BottomBarHeight);

            var barImage = bottomBar.gameObject.AddComponent<Image>();
            barImage.color = new Color(0.06f, 0.07f, 0.12f, 0.95f);
            barImage.raycastTarget = false;

            playerStatsText = CreateNamedTmpText(bottomBar, "PlayerStatsText",
                "Player\nATK 0   DEF 0\nSPD 0   HP 0/0", StatsFontSize, FontStyles.Normal);
            var playerRect = playerStatsText.rectTransform;
            playerRect.anchorMin = new Vector2(0f, 0f);
            playerRect.anchorMax = new Vector2(0.5f, 1f);
            playerRect.offsetMin = new Vector2(BottomBarPadding, 24f);
            playerRect.offsetMax = new Vector2(-16f, -24f);
            playerStatsText.alignment = TextAlignmentOptions.TopLeft;
            playerStatsText.lineSpacing = 4f;

            enemyStatsText = CreateNamedTmpText(bottomBar, "EnemyStatsText",
                "Enemy\nATK 0   DEF 0\nSPD 0   HP 0/0", StatsFontSize, FontStyles.Normal);
            var enemyRect = enemyStatsText.rectTransform;
            enemyRect.anchorMin = new Vector2(0.5f, 0f);
            enemyRect.anchorMax = new Vector2(1f, 1f);
            enemyRect.offsetMin = new Vector2(16f, 24f);
            enemyRect.offsetMax = new Vector2(-BottomBarPadding, -24f);
            enemyStatsText.alignment = TextAlignmentOptions.TopRight;

            return bottomBar;
        }

        /// <summary>
        /// Full-screen game over overlay under BattleHUD. Hidden by default (SetActive false).
        /// BattleManager fills stat lines when the player dies; buttons reload scenes.
        /// </summary>
        private static GameOverPanelRefs CreateGameOverPanel(RectTransform hudRoot)
        {
            var panel = CreateRect("GameOverPanel", hudRoot);
            StretchToParent(panel);

            // Dim the battle HUD behind the summary card.
            var backdrop = panel.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.04f, 0.05f, 0.1f, 0.92f);
            backdrop.raycastTarget = true;

            var card = CreateRect("GameOverCard", panel);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(880f, 920f);

            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.08f, 0.09f, 0.15f, 0.98f);
            cardImage.raycastTarget = true;

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 40);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var titleText = CreateGameOverStatLine(card, "GameOverTitleText", "YOU HAVE FALLEN", 48f, FontStyles.Bold);
            titleText.color = new Color(0.92f, 0.45f, 0.42f, 1f);

            var fighterNameText = CreateGameOverStatLine(card, "GameOverFighterNameText", "Fighter", 36f, FontStyles.Bold);
            var finalScoreText = CreateGameOverStatLine(card, "GameOverFinalScoreText", "Final Score: 0", 32f, FontStyles.Normal);
            var waveReachedText = CreateGameOverStatLine(card, "GameOverWaveReachedText", "Wave Reached: 0", 30f, FontStyles.Normal);
            var enemiesDefeatedText = CreateGameOverStatLine(card, "GameOverEnemiesDefeatedText", "Enemies Defeated: 0", 30f, FontStyles.Normal);
            var highestWaveText = CreateGameOverStatLine(card, "GameOverHighestWaveText", "Highest Wave: 0", 30f, FontStyles.Normal);
            var bestScoreText = CreateGameOverStatLine(card, "GameOverBestScoreText", "Best Score: 0", 30f, FontStyles.Normal);
            var allTimeHighestWaveText = CreateGameOverStatLine(card, "GameOverAllTimeHighestWaveText", "Best Wave: 0", 30f, FontStyles.Normal);

            var buttonRow = CreateRect("GameOverButtonRow", card);
            buttonRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;

            var buttonLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 24f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = true;

            var playAgainButton = CreateGameOverButton(buttonRow, "PlayAgainButton", "Play Again",
                new Color(0.18f, 0.62f, 0.36f, 1f));
            var characterBuilderButton = CreateGameOverButton(buttonRow, "CharacterBuilderButton", "Character Builder",
                new Color(0.24f, 0.42f, 0.72f, 1f));

            // Hidden until BattleManager calls ShowGameOver after player death.
            panel.gameObject.SetActive(false);

            return new GameOverPanelRefs
            {
                Panel = panel.gameObject,
                TitleText = titleText,
                FighterNameText = fighterNameText,
                FinalScoreText = finalScoreText,
                WaveReachedText = waveReachedText,
                EnemiesDefeatedText = enemiesDefeatedText,
                HighestWaveText = highestWaveText,
                BestScoreText = bestScoreText,
                AllTimeHighestWaveText = allTimeHighestWaveText,
                PlayAgainButton = playAgainButton,
                CharacterBuilderButton = characterBuilderButton
            };
        }

        private static TextMeshProUGUI CreateGameOverStatLine(
            RectTransform parent,
            string objectName,
            string defaultText,
            float fontSize,
            FontStyles fontStyle)
        {
            var line = CreateNamedTmpText(parent, objectName, defaultText, fontSize, fontStyle);
            line.alignment = TextAlignmentOptions.Center;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = fontSize + 16f;
            return line;
        }

        private static Button CreateGameOverButton(RectTransform parent, string buttonName, string label, Color color)
        {
            var buttonRect = CreateRect(buttonName, parent);

            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = color;

            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.75f;
            button.colors = colors;

            var text = CreateNamedTmpText(buttonRect, "Label", label, 32f, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchToParent(text.rectTransform);

            return button;
        }

        private static TextMeshProUGUI CreateNamedTmpText(
            Transform parent,
            string objectName,
            string defaultText,
            float fontSize,
            FontStyles fontStyle)
        {
            var tmp = CreateTmpText(parent, defaultText, fontSize, fontStyle);
            tmp.gameObject.name = objectName;
            return tmp;
        }

        private static void WireBattleHud(
            BattleHUD hud,
            TextMeshProUGUI titleText,
            TextMeshProUGUI waveText,
            TextMeshProUGUI scoreText,
            TextMeshProUGUI battleLogText,
            TextMeshProUGUI playerStatsText,
            TextMeshProUGUI enemyStatsText,
            Image playerHealthFill,
            Image enemyHealthFill,
            GameOverPanelRefs gameOverRefs)
        {
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("titleText").objectReferenceValue = titleText;
            serialized.FindProperty("waveText").objectReferenceValue = waveText;
            serialized.FindProperty("scoreText").objectReferenceValue = scoreText;
            serialized.FindProperty("battleLogText").objectReferenceValue = battleLogText;
            serialized.FindProperty("playerStatsText").objectReferenceValue = playerStatsText;
            serialized.FindProperty("enemyStatsText").objectReferenceValue = enemyStatsText;
            serialized.FindProperty("playerHealthBarFill").objectReferenceValue = playerHealthFill;
            serialized.FindProperty("enemyHealthBarFill").objectReferenceValue = enemyHealthFill;

            serialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverRefs.Panel;
            serialized.FindProperty("gameOverTitleText").objectReferenceValue = gameOverRefs.TitleText;
            serialized.FindProperty("gameOverFighterNameText").objectReferenceValue = gameOverRefs.FighterNameText;
            serialized.FindProperty("gameOverFinalScoreText").objectReferenceValue = gameOverRefs.FinalScoreText;
            serialized.FindProperty("gameOverWaveReachedText").objectReferenceValue = gameOverRefs.WaveReachedText;
            serialized.FindProperty("gameOverEnemiesDefeatedText").objectReferenceValue = gameOverRefs.EnemiesDefeatedText;
            serialized.FindProperty("gameOverHighestWaveText").objectReferenceValue = gameOverRefs.HighestWaveText;
            serialized.FindProperty("gameOverBestScoreText").objectReferenceValue = gameOverRefs.BestScoreText;
            serialized.FindProperty("gameOverAllTimeHighestWaveText").objectReferenceValue = gameOverRefs.AllTimeHighestWaveText;
            serialized.FindProperty("playAgainButton").objectReferenceValue = gameOverRefs.PlayAgainButton;
            serialized.FindProperty("characterBuilderButton").objectReferenceValue = gameOverRefs.CharacterBuilderButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        private static void WireBattleManagerProduction(
            BattleManager manager,
            FighterRig playerRig,
            FighterRig enemyRig,
            BattleHUD battleHud)
        {
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("playerFighterRig").objectReferenceValue = playerRig;
            serializedManager.FindProperty("enemyFighterRig").objectReferenceValue = enemyRig;
            serializedManager.FindProperty("battleHUD").objectReferenceValue = battleHud;
            // Mirror player gear on the enemy so the dummy is visible during UI testing.
            serializedManager.FindProperty("enemyMirrorPlayerAppearance").boolValue = true;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        /// <summary>
        /// Adds FloatingCombatTextSpawner to BattleManager and wires rigs + prefab for floating numbers.
        /// </summary>
        private static void EnsureFloatingCombatTextSpawner(
            GameObject battleManagerObject,
            FighterRig playerRig,
            FighterRig enemyRig)
        {
            if (battleManagerObject == null)
            {
                return;
            }

            var spawner = battleManagerObject.GetComponent<FloatingCombatTextSpawner>();
            if (spawner == null)
            {
                spawner = battleManagerObject.AddComponent<FloatingCombatTextSpawner>();
            }

            var prefab = FloatingCombatTextPrefabCreator.LoadOrCreatePrefab();
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("floatingCombatTextPrefab").objectReferenceValue = prefab;
            serialized.FindProperty("playerFighterRig").objectReferenceValue = playerRig;
            serialized.FindProperty("enemyFighterRig").objectReferenceValue = enemyRig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);
        }

        private static void EnsureFloatingCombatTextSpawnerOnBattleManager()
        {
            var battleManager = Object.FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                return;
            }

            var playerRig = GameObject.Find("PlayerFighterRig")?.GetComponent<FighterRig>();
            var enemyRig = GameObject.Find("EnemyFighterRig")?.GetComponent<FighterRig>();
            EnsureFloatingCombatTextSpawner(battleManager.gameObject, playerRig, enemyRig);
        }

        /// <summary>
        /// Finds production UI objects in the open scene and applies the latest layout constants.
        /// Returns false when the expected hierarchy is missing.
        /// </summary>
        private static bool PolishExistingProductionUi()
        {
            var playerRigObject = GameObject.Find("PlayerFighterRig");
            var enemyRigObject = GameObject.Find("EnemyFighterRig");
            var battleLogText = GameObject.Find("BattleLogText")?.GetComponent<TextMeshProUGUI>();
            var playerStatsText = GameObject.Find("PlayerStatsText")?.GetComponent<TextMeshProUGUI>();
            var enemyStatsText = GameObject.Find("EnemyStatsText")?.GetComponent<TextMeshProUGUI>();
            var bottomBar = GameObject.Find("BottomStatsBar")?.GetComponent<RectTransform>();
            var battleLogPanel = GameObject.Find("BattleLogPanel")?.GetComponent<RectTransform>();
            var playerHealthBar = GameObject.Find("PlayerHealthBar")?.GetComponent<RectTransform>();
            var enemyHealthBar = GameObject.Find("EnemyHealthBar")?.GetComponent<RectTransform>();
            var enemySlot = GameObject.Find("EnemySlot")?.GetComponent<RectTransform>();

            if (playerRigObject == null || enemyRigObject == null || battleLogText == null)
            {
                return false;
            }

            ApplyFighterRigLayout(playerRigObject.GetComponent<RectTransform>(), isPlayer: true);
            ApplyFighterRigLayout(enemyRigObject.GetComponent<RectTransform>(), isPlayer: false);
            ApplyLayerSizes(playerRigObject.transform);
            ApplyLayerSizes(enemyRigObject.transform);

            EnsureHealthBarLabel(enemySlot, "EnemyHealthBarLabel", "Enemy HP", enemyHealthBar);
            EnsureHealthBarLabel(
                playerHealthBar != null ? playerHealthBar.parent as RectTransform : null,
                "PlayerHealthBarLabel",
                "Player HP",
                playerHealthBar);
            ApplyHealthBarLayout(playerHealthBar);
            ApplyHealthBarLayout(enemyHealthBar);

            EnsureEnemyBackdrop(enemySlot);

            battleLogText.fontSize = BattleLogFontSize;
            battleLogText.lineSpacing = 2f;

            if (playerStatsText != null)
            {
                playerStatsText.fontSize = StatsFontSize;
                playerStatsText.lineSpacing = 4f;
                var playerRect = playerStatsText.rectTransform;
                playerRect.offsetMin = new Vector2(BottomBarPadding, 24f);
                playerRect.offsetMax = new Vector2(-16f, -24f);
            }

            if (enemyStatsText != null)
            {
                enemyStatsText.fontSize = StatsFontSize;
                enemyStatsText.lineSpacing = 4f;
                var enemyRect = enemyStatsText.rectTransform;
                enemyRect.offsetMin = new Vector2(16f, 24f);
                enemyRect.offsetMax = new Vector2(-BottomBarPadding, -24f);
            }

            if (bottomBar != null)
            {
                bottomBar.sizeDelta = new Vector2(0f, BottomBarHeight);
            }

            if (battleLogPanel != null)
            {
                battleLogPanel.anchoredPosition = new Vector2(0f, BottomBarHeight + 16f);
                battleLogPanel.sizeDelta = new Vector2(960f, 280f);
            }

            var battleManager = Object.FindObjectOfType<BattleManager>();
            if (battleManager != null)
            {
                var serializedManager = new SerializedObject(battleManager);
                serializedManager.FindProperty("enemyMirrorPlayerAppearance").boolValue = true;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(battleManager);
            }

            EnsureGameOverPanelOnBattleHud();
            EnsureFloatingCombatTextSpawnerOnBattleManager();

            return true;
        }

        /// <summary>
        /// Adds GameOverPanel to an existing BattleHUD when polishing or upgrading older scenes.
        /// </summary>
        private static void EnsureGameOverPanelOnBattleHud()
        {
            var battleHud = Object.FindObjectOfType<BattleHUD>();
            if (battleHud == null)
            {
                return;
            }

            var hudRoot = battleHud.GetComponent<RectTransform>();
            var existingPanel = hudRoot.Find("GameOverPanel");
            GameOverPanelRefs gameOverRefs;

            if (existingPanel != null)
            {
                existingPanel.SetAsLastSibling();
                gameOverRefs = CollectGameOverPanelRefs(existingPanel);
            }
            else
            {
                gameOverRefs = CreateGameOverPanel(hudRoot);
            }

            WireGameOverPanelOnly(battleHud, gameOverRefs);
        }

        private static GameOverPanelRefs CollectGameOverPanelRefs(Transform panelRoot)
        {
            return new GameOverPanelRefs
            {
                Panel = panelRoot.gameObject,
                TitleText = panelRoot.Find("GameOverCard/GameOverTitleText")?.GetComponent<TextMeshProUGUI>(),
                FighterNameText = panelRoot.Find("GameOverCard/GameOverFighterNameText")?.GetComponent<TextMeshProUGUI>(),
                FinalScoreText = panelRoot.Find("GameOverCard/GameOverFinalScoreText")?.GetComponent<TextMeshProUGUI>(),
                WaveReachedText = panelRoot.Find("GameOverCard/GameOverWaveReachedText")?.GetComponent<TextMeshProUGUI>(),
                EnemiesDefeatedText = panelRoot.Find("GameOverCard/GameOverEnemiesDefeatedText")?.GetComponent<TextMeshProUGUI>(),
                HighestWaveText = panelRoot.Find("GameOverCard/GameOverHighestWaveText")?.GetComponent<TextMeshProUGUI>(),
                BestScoreText = panelRoot.Find("GameOverCard/GameOverBestScoreText")?.GetComponent<TextMeshProUGUI>(),
                AllTimeHighestWaveText = panelRoot.Find("GameOverCard/GameOverAllTimeHighestWaveText")?.GetComponent<TextMeshProUGUI>(),
                PlayAgainButton = panelRoot.Find("GameOverCard/GameOverButtonRow/PlayAgainButton")?.GetComponent<Button>(),
                CharacterBuilderButton = panelRoot.Find("GameOverCard/GameOverButtonRow/CharacterBuilderButton")?.GetComponent<Button>()
            };
        }

        private static void WireGameOverPanelOnly(BattleHUD hud, GameOverPanelRefs gameOverRefs)
        {
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("gameOverPanel").objectReferenceValue = gameOverRefs.Panel;
            serialized.FindProperty("gameOverTitleText").objectReferenceValue = gameOverRefs.TitleText;
            serialized.FindProperty("gameOverFighterNameText").objectReferenceValue = gameOverRefs.FighterNameText;
            serialized.FindProperty("gameOverFinalScoreText").objectReferenceValue = gameOverRefs.FinalScoreText;
            serialized.FindProperty("gameOverWaveReachedText").objectReferenceValue = gameOverRefs.WaveReachedText;
            serialized.FindProperty("gameOverEnemiesDefeatedText").objectReferenceValue = gameOverRefs.EnemiesDefeatedText;
            serialized.FindProperty("gameOverHighestWaveText").objectReferenceValue = gameOverRefs.HighestWaveText;
            serialized.FindProperty("gameOverBestScoreText").objectReferenceValue = gameOverRefs.BestScoreText;
            serialized.FindProperty("gameOverAllTimeHighestWaveText").objectReferenceValue = gameOverRefs.AllTimeHighestWaveText;
            serialized.FindProperty("playAgainButton").objectReferenceValue = gameOverRefs.PlayAgainButton;
            serialized.FindProperty("characterBuilderButton").objectReferenceValue = gameOverRefs.CharacterBuilderButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        /// <summary>
        /// Moves and resizes one FighterRig RectTransform to the polished mobile layout.
        /// </summary>
        private static void ApplyFighterRigLayout(RectTransform rigRect, bool isPlayer)
        {
            if (rigRect == null)
            {
                return;
            }

            var anchorX = isPlayer ? 0.32f : 0.68f;
            rigRect.anchorMin = new Vector2(anchorX, 0.22f);
            rigRect.anchorMax = new Vector2(anchorX, 0.22f);
            rigRect.pivot = new Vector2(0.5f, 0.5f);
            rigRect.anchoredPosition = isPlayer ? new Vector2(-12f, 8f) : new Vector2(12f, 8f);
            rigRect.sizeDelta = FighterRigSize;
            EditorUtility.SetDirty(rigRect);
        }

        /// <summary>
        /// Scales each equipment Image inside a rig by the +25% layout constants.
        /// Supports both anchor hierarchy (MountAnchor/MountImage) and legacy flat layout.
        /// </summary>
        private static void ApplyLayerSizes(Transform rigRoot)
        {
            if (rigRoot == null)
            {
                return;
            }

            SetLayerSize(rigRoot, "MountAnchor", "MountImage", LayerMountBodySize);
            SetLayerSize(rigRoot, "BodyAnchor", "BodyImage", LayerMountBodySize);
            SetLayerSize(rigRoot, "WeaponAnchor", "WeaponImage", LayerWeaponSize);
            SetLayerSize(rigRoot, "HeadAnchor", "HeadImage", LayerHeadSize);
            SetLayerSize(rigRoot, "CrownAnchor", "CrownImage", LayerCrownSize);
        }

        private static void SetLayerSize(Transform rigRoot, string anchorName, string imageName, float size)
        {
            var imageTransform = rigRoot.Find($"{anchorName}/{imageName}") ?? rigRoot.Find(imageName);
            if (imageTransform == null)
            {
                return;
            }

            var layer = imageTransform as RectTransform;
            layer.sizeDelta = new Vector2(size, size);
            EditorUtility.SetDirty(layer);
        }

        private static void ApplyHealthBarLayout(RectTransform barRect)
        {
            if (barRect == null)
            {
                return;
            }

            barRect.anchoredPosition = new Vector2(0f, 28f);
            barRect.sizeDelta = new Vector2(HealthBarWidth, HealthBarHeight);
            EditorUtility.SetDirty(barRect);
        }

        /// <summary>
        /// Adds a health bar label if the scene was built before labels existed.
        /// </summary>
        private static void EnsureHealthBarLabel(
            RectTransform slot,
            string labelName,
            string labelText,
            RectTransform healthBar)
        {
            if (slot == null || healthBar == null)
            {
                return;
            }

            var existing = slot.Find(labelName);
            if (existing != null)
            {
                var existingLabel = existing.GetComponent<TextMeshProUGUI>();
                if (existingLabel != null)
                {
                    existingLabel.fontSize = HealthBarLabelFontSize;
                    existingLabel.text = labelText;
                }

                return;
            }

            var label = CreateNamedTmpText(slot, labelName, labelText,
                HealthBarLabelFontSize, FontStyles.Bold);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 28f + HealthBarHeight + 6f);
            labelRect.sizeDelta = new Vector2(HealthBarWidth, 28f);
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.88f, 0.9f, 0.95f, 1f);
        }

        /// <summary>
        /// Adds the enemy backdrop panel when polishing an older scene.
        /// </summary>
        private static void EnsureEnemyBackdrop(RectTransform enemySlot)
        {
            if (enemySlot == null)
            {
                return;
            }

            var existing = enemySlot.Find("EnemyBackdrop");
            if (existing != null)
            {
                var existingRect = existing as RectTransform;
                existingRect.anchorMin = new Vector2(0.68f, 0.22f);
                existingRect.anchorMax = new Vector2(0.68f, 0.22f);
                existingRect.pivot = new Vector2(0.5f, 0.5f);
                existingRect.anchoredPosition = new Vector2(12f, 8f);
                existingRect.sizeDelta = new Vector2(FighterRigSize.x + 40f, FighterRigSize.y + 40f);
                return;
            }

            CreateEnemyBackdrop(enemySlot);
        }
    }
}
