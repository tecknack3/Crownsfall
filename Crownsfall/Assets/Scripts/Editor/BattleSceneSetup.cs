using System.IO;
using Crownsfall.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Crownsfall.Editor
{
    /// <summary>
    /// Editor menu that creates a minimal Battle scene hierarchy for beginners.
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

        [MenuItem("Tools/Fighter Tools/Setup Battle Scene")]
        public static void SetupSceneFromMenu()
        {
            SetupScene();
            EditorUtility.DisplayDialog(
                "Battle Scene",
                "Battle scene created at:\n" + ScenePath,
                "OK");
        }

        /// <summary>
        /// Creates the scene, camera, BattleManager, and two fighter views with sprite layers.
        /// </summary>
        public static void SetupScene()
        {
            EnsureSceneFolderExists();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            var battleManagerObject = CreateBattleManagerObject();
            var playerView = CreateFighterView("PlayerFighterView", PlayerPosition);
            var enemyView = CreateFighterView("EnemyFighterView", EnemyPosition);

            WireBattleManager(battleManagerObject.GetComponent<BattleManager>(), playerView, enemyView);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Battle scene saved to {ScenePath}");
        }

        /// <summary>
        /// Creates Assets/Scenes/BattleScene if it does not exist yet.
        /// </summary>
        private static void EnsureSceneFolderExists()
        {
            var folder = Path.GetDirectoryName(ScenePath);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "BattleScene");
            }
        }

        /// <summary>
        /// Adds an orthographic camera suited for 2D battle sprites on mobile portrait.
        /// Orthographic size 3.5 shows roughly 7 world units tall — good with fighter positions ±0.9.
        /// </summary>
        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            camera.transform.position = CameraPosition;

            cameraObject.AddComponent<AudioListener>();
        }

        /// <summary>
        /// Creates the root object that runs battle-scene startup logic.
        /// </summary>
        private static GameObject CreateBattleManagerObject()
        {
            return new GameObject("BattleManager", typeof(BattleManager));
        }

        /// <summary>
        /// Creates a fighter view with four child SpriteRenderers (Mount → Head).
        /// </summary>
        private static BattleFighterView CreateFighterView(string objectName, Vector3 position)
        {
            var root = new GameObject(objectName);
            root.transform.position = position;
            root.transform.localScale = FighterScale;

            var view = root.AddComponent<BattleFighterView>();

            var mountRenderer = CreateSpriteLayer(root.transform, "Mount", 0);
            var bodyRenderer = CreateSpriteLayer(root.transform, "Body", 1);
            var weaponRenderer = CreateSpriteLayer(root.transform, "Weapon", 2);
            var headRenderer = CreateSpriteLayer(root.transform, "Head", 3);

            WireFighterView(view, mountRenderer, bodyRenderer, weaponRenderer, headRenderer);

            return view;
        }

        /// <summary>
        /// Creates one child GameObject with a SpriteRenderer for a single equipment layer.
        /// Child stays at local (0, 0, 0) so scaling the root keeps sprites centered.
        /// </summary>
        private static SpriteRenderer CreateSpriteLayer(Transform parent, string layerName, int sortingOrder)
        {
            var layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.localPosition = Vector3.zero;

            var renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>
        /// Assigns serialized SpriteRenderer fields on BattleFighterView.
        /// </summary>
        private static void WireFighterView(
            BattleFighterView view,
            SpriteRenderer mountRenderer,
            SpriteRenderer bodyRenderer,
            SpriteRenderer weaponRenderer,
            SpriteRenderer headRenderer)
        {
            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("mountRenderer").objectReferenceValue = mountRenderer;
            serializedView.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
            serializedView.FindProperty("weaponRenderer").objectReferenceValue = weaponRenderer;
            serializedView.FindProperty("headRenderer").objectReferenceValue = headRenderer;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        /// <summary>
        /// Assigns player and enemy BattleFighterView references on BattleManager.
        /// </summary>
        private static void WireBattleManager(
            BattleManager manager,
            BattleFighterView playerView,
            BattleFighterView enemyView)
        {
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("playerFighterView").objectReferenceValue = playerView;
            serializedManager.FindProperty("enemyFighterView").objectReferenceValue = enemyView;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }
    }
}
