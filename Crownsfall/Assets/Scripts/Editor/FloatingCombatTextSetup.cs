using Crownsfall.Combat;
using Crownsfall.Combat.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crownsfall.Editor
{
    /// <summary>
    /// One-shot setup for floating damage numbers: prefab asset + scene spawner under Canvas.
    /// Run with BattleScene open via Tools → Fighter Tools → Setup Floating Combat Text.
    /// </summary>
    public static class FloatingCombatTextSetup
    {
        private const string SpawnerObjectName = "FloatingCombatTextSpawner";
        private const string LogPrefix = "[Floating Combat Text Setup]";

        [MenuItem("Tools/Fighter Tools/Setup Floating Combat Text")]
        public static void SetupFromMenu()
        {
            SetupInOpenScene(saveScene: true);
        }

        /// <summary>
        /// Ensures prefab + spawner exist in the active scene and wires FighterRig references.
        /// </summary>
        public static void SetupInOpenScene(bool saveScene = true)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"{LogPrefix} No scene is open. Open BattleScene first.");
                return;
            }

            Undo.SetCurrentGroupName("Setup Floating Combat Text");
            var undoGroup = Undo.GetCurrentGroup();

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"{LogPrefix} No Canvas found in '{scene.name}'. Add a Canvas or open BattleScene.");
                return;
            }

            Debug.Log($"{LogPrefix} Found Canvas: {canvas.name}");

            var floatingTextLayer = FloatingCombatTextSpawner.EnsureFloatingTextLayer(canvas);
            if (floatingTextLayer != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(floatingTextLayer.gameObject, "Setup FloatingTextLayer");
                EditorUtility.SetDirty(floatingTextLayer.gameObject);
                Debug.Log($"{LogPrefix} Ensured {FloatingCombatTextSpawner.FloatingTextLayerName} under Canvas (sortingOrder {FloatingCombatTextSpawner.FloatingTextSortingOrder}).");
            }

            var prefabExisted = AssetDatabase.LoadAssetAtPath<FloatingCombatText>(
                FloatingCombatTextPrefabCreator.PrefabPath) != null;
            var prefab = FloatingCombatTextPrefabCreator.LoadOrCreatePrefab();
            if (prefab == null)
            {
                Debug.LogError($"{LogPrefix} Failed to create FloatingCombatText prefab.");
                return;
            }

            Debug.Log(prefabExisted
                ? $"{LogPrefix} Using existing prefab at {FloatingCombatTextPrefabCreator.PrefabPath}"
                : $"{LogPrefix} Created prefab at {FloatingCombatTextPrefabCreator.PrefabPath}");

            var spawner = EnsureSpawnerUnderCanvas(canvas.transform, prefab, out var spawnerCreated);
            if (spawnerCreated)
            {
                Debug.Log($"{LogPrefix} Created {SpawnerObjectName} under Canvas.");
            }
            else
            {
                Debug.Log($"{LogPrefix} Found existing {SpawnerObjectName} under Canvas — skipped create.");
            }

            var playerRig = FindPlayerFighterRig();
            var enemyRig = FindEnemyFighterRig();

            LogFighterRigStatus("Player", playerRig);
            LogFighterRigStatus("Enemy", enemyRig);

            WireSpawner(spawner, prefab, playerRig, enemyRig);
            RemoveLegacySpawnerOnBattleManager(spawner);

            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(spawner.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            if (saveScene)
            {
                EditorSceneManager.SaveOpenScenes();
                Debug.Log($"{LogPrefix} Scene saved.");
            }

            Debug.Log($"{LogPrefix} Complete.");
        }

        /// <summary>
        /// Creates or updates the spawner under a Canvas. Used by BattleSceneSetup during full scene builds.
        /// </summary>
        public static FloatingCombatTextSpawner EnsureSpawnerUnderCanvas(
            Transform canvasTransform,
            FloatingCombatText prefab,
            FighterRig playerRig,
            FighterRig enemyRig)
        {
            var spawner = EnsureSpawnerUnderCanvas(canvasTransform, prefab, out _);
            WireSpawner(spawner, prefab, playerRig, enemyRig);
            EditorUtility.SetDirty(spawner);
            return spawner;
        }

        private static FloatingCombatTextSpawner EnsureSpawnerUnderCanvas(
            Transform canvasTransform,
            FloatingCombatText prefab,
            out bool created)
        {
            created = false;
            var existing = canvasTransform.Find(SpawnerObjectName);
            GameObject spawnerObject;

            if (existing != null)
            {
                spawnerObject = existing.gameObject;
            }
            else
            {
                spawnerObject = new GameObject(SpawnerObjectName);
                Undo.RegisterCreatedObjectUndo(spawnerObject, "Create FloatingCombatTextSpawner");
                spawnerObject.transform.SetParent(canvasTransform, false);
                created = true;
            }

            var spawner = spawnerObject.GetComponent<FloatingCombatTextSpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<FloatingCombatTextSpawner>(spawnerObject);
                Debug.Log($"{LogPrefix} Added FloatingCombatTextSpawner component.");
            }
            else
            {
                Debug.Log($"{LogPrefix} FloatingCombatTextSpawner component already present — skipped add.");
            }

            return spawner;
        }

        private static void WireSpawner(
            FloatingCombatTextSpawner spawner,
            FloatingCombatText prefab,
            FighterRig playerRig,
            FighterRig enemyRig)
        {
            var serialized = new SerializedObject(spawner);

            AssignIfChanged(serialized, "floatingCombatTextPrefab", prefab,
                $"{LogPrefix} Assigned FloatingCombatText prefab to spawner.",
                $"{LogPrefix} FloatingCombatText prefab already assigned — skipped.");

            var canvas = spawner.GetComponentInParent<Canvas>() ?? Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                AssignIfChanged(serialized, "targetCanvas", canvas,
                    $"{LogPrefix} Assigned targetCanvas: {canvas.name}.",
                    $"{LogPrefix} targetCanvas already assigned to {canvas.name} — skipped.");
            }

            if (playerRig != null)
            {
                AssignIfChanged(serialized, "playerFighterRig", playerRig,
                    $"{LogPrefix} Assigned playerFighterRig: {playerRig.name} (DamageAnchor: {FormatAnchor(playerRig)}).",
                    $"{LogPrefix} playerFighterRig already assigned to {playerRig.name} — skipped.");
            }

            if (enemyRig != null)
            {
                AssignIfChanged(serialized, "enemyFighterRig", enemyRig,
                    $"{LogPrefix} Assigned enemyFighterRig: {enemyRig.name} (DamageAnchor: {FormatAnchor(enemyRig)}).",
                    $"{LogPrefix} enemyFighterRig already assigned to {enemyRig.name} — skipped.");
            }

            serialized.ApplyModifiedProperties();
        }

        private static void AssignIfChanged(
            SerializedObject serialized,
            string propertyName,
            Object value,
            string assignedMessage,
            string skippedMessage)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"{LogPrefix} Could not find spawner field '{propertyName}'.");
                return;
            }

            if (property.objectReferenceValue != value)
            {
                property.objectReferenceValue = value;
                Debug.Log(assignedMessage);
            }
            else
            {
                Debug.Log(skippedMessage);
            }
        }

        private static void LogFighterRigStatus(string label, FighterRig rig)
        {
            if (rig == null)
            {
                Debug.LogWarning($"{LogPrefix} {label} FighterRig not found. Assign {label.ToLowerInvariant()}FighterRig manually in the Inspector.");
                return;
            }

            if (rig.DamageAnchor == null)
            {
                Debug.LogWarning($"{LogPrefix} {rig.name} has no DamageAnchor wired. Run Build Fighter Rig Hierarchy or Setup Battle Scene Production UI.");
            }
        }

        private static string FormatAnchor(FighterRig rig)
        {
            return rig.DamageAnchor != null ? rig.DamageAnchor.name : "missing";
        }

        private static void RemoveLegacySpawnerOnBattleManager(FloatingCombatTextSpawner currentSpawner)
        {
            var battleManager = Object.FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                return;
            }

            var legacySpawner = battleManager.GetComponent<FloatingCombatTextSpawner>();
            if (legacySpawner == null || legacySpawner == currentSpawner)
            {
                return;
            }

            Undo.DestroyObjectImmediate(legacySpawner);
            Debug.Log($"{LogPrefix} Removed legacy FloatingCombatTextSpawner from BattleManager.");
        }

        private static FighterRig FindPlayerFighterRig()
        {
            var fromManager = FindFighterRigFromBattleManager("playerFighterRig");
            return fromManager != null
                ? fromManager
                : GameObject.Find("PlayerFighterRig")?.GetComponent<FighterRig>();
        }

        private static FighterRig FindEnemyFighterRig()
        {
            var fromManager = FindFighterRigFromBattleManager("enemyFighterRig");
            return fromManager != null
                ? fromManager
                : GameObject.Find("EnemyFighterRig")?.GetComponent<FighterRig>();
        }

        private static FighterRig FindFighterRigFromBattleManager(string propertyName)
        {
            var battleManager = Object.FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                return null;
            }

            var serialized = new SerializedObject(battleManager);
            var property = serialized.FindProperty(propertyName);
            return property?.objectReferenceValue as FighterRig;
        }
    }
}
