using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Crownsfall.Characters;
using Crownsfall.Combat;
using Crownsfall.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Crownsfall.Editor
{
    /// <summary>
    /// Finds CharacterBuilderManager in the active scene, populates equipment lists
    /// from ScriptableObject folders, and wires UI references under Canvas/MainLayout.
    /// </summary>
    public static class CharacterBuilderAutoWire
    {
        private const string HeadsFolder = "Assets/ScriptableObjects/Heads";
        private const string BodiesFolder = "Assets/ScriptableObjects/Bodies";
        private const string WeaponsFolder = "Assets/ScriptableObjects/Weapons";
        private const string MountsFolder = "Assets/ScriptableObjects/Mounts";

        [MenuItem("Tools/Fighter Tools/Auto Wire Character Builder")]
        public static void AutoWireFromMenu()
        {
            AutoWire();
        }

        private static void AutoWire()
        {
            var managerObject = GameObject.Find("CharacterBuilderManager");
            if (managerObject == null)
            {
                Debug.LogError(
                    "CharacterBuilderAutoWire: GameObject \"CharacterBuilderManager\" not found in the active scene.");
                return;
            }

            var manager = managerObject.GetComponent<CharacterBuilderManager>();
            if (manager == null)
            {
                Debug.LogError(
                    "CharacterBuilderAutoWire: CharacterBuilderManager component not found on \"CharacterBuilderManager\".");
                return;
            }

            var heads = LoadScriptableObjects<HeadSO>("HeadSO", HeadsFolder);
            var bodies = LoadScriptableObjects<BodySO>("BodySO", BodiesFolder);
            var weapons = LoadScriptableObjects<WeaponSO>("WeaponSO", WeaponsFolder);
            var mounts = LoadScriptableObjects<MountSO>("MountSO", MountsFolder);

            var missing = new List<string>();
            var mainLayout = FindMainLayout(missing);

            var serializedManager = new SerializedObject(manager);

            AssignEquipmentList(serializedManager, "heads", heads);
            AssignEquipmentList(serializedManager, "bodies", bodies);
            AssignEquipmentList(serializedManager, "weapons", weapons);
            AssignEquipmentList(serializedManager, "mounts", mounts);

            if (mainLayout != null)
            {
                WireUiReferences(serializedManager, mainLayout, missing);
            }

            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(manager);
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();

            LogReport(heads.Count, bodies.Count, weapons.Count, mounts.Count, missing);
        }

        private static Transform FindMainLayout(List<string> missing)
        {
            var canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                missing.Add("Canvas (root GameObject not found)");
                return null;
            }

            var mainLayout = FindTransform(canvasObject.transform, "MainLayout");
            if (mainLayout == null)
            {
                missing.Add("MainLayout (not found under Canvas)");
            }

            return mainLayout;
        }

        private static void WireUiReferences(
            SerializedObject serializedManager,
            Transform mainLayout,
            List<string> missing)
        {
            AssignComponentReference<Image>(
                serializedManager,
                "headPreviewImage",
                mainLayout,
                "FighterPreview/PreviewStack/HeadImage",
                missing);
            AssignComponentReference<Image>(
                serializedManager,
                "bodyPreviewImage",
                mainLayout,
                "FighterPreview/PreviewStack/BodyImage",
                missing);
            AssignComponentReference<Image>(
                serializedManager,
                "weaponPreviewImage",
                mainLayout,
                "FighterPreview/PreviewStack/WeaponImage",
                missing);
            AssignComponentReference<Image>(
                serializedManager,
                "mountPreviewImage",
                mainLayout,
                "FighterPreview/PreviewStack/MountImage",
                missing);

            AssignComponentReference<FighterRig>(
                serializedManager,
                "characterBuilderPreviewRig",
                mainLayout,
                "FighterPreview",
                missing);

            WireSelectorReferences(serializedManager, mainLayout, "HeadSelector", "head", missing);
            WireSelectorReferences(serializedManager, mainLayout, "BodySelector", "body", missing);
            WireSelectorReferences(serializedManager, mainLayout, "WeaponSelector", "weapon", missing);
            WireSelectorReferences(serializedManager, mainLayout, "MountSelector", "mount", missing);

            AssignComponentReference<TMP_InputField>(
                serializedManager,
                "fighterNameInput",
                mainLayout,
                "NameSection/FighterNameInput",
                missing);

            AssignComponentReference<Button>(
                serializedManager,
                "createFighterButton",
                mainLayout,
                "CreateFighterButton",
                missing);

            WireFighterCardReferences(serializedManager, mainLayout, missing);
        }

        private static void WireFighterCardReferences(
            SerializedObject serializedManager,
            Transform mainLayout,
            List<string> missing)
        {
            AssignGameObjectReference(
                serializedManager,
                "fighterCardPanel",
                mainLayout,
                "FighterCardPanel",
                missing);

            AssignComponentReference<Image>(
                serializedManager,
                "cardMountImage",
                mainLayout,
                "FighterCardPanel/Content/CardPreviewArea/CardMountImage",
                missing);
            AssignComponentReference<Image>(
                serializedManager,
                "cardBodyImage",
                mainLayout,
                "FighterCardPanel/Content/CardPreviewArea/CardBodyImage",
                missing);
            AssignComponentReference<Image>(
                serializedManager,
                "cardWeaponImage",
                mainLayout,
                "FighterCardPanel/Content/CardPreviewArea/CardWeaponImage",
                missing);
            AssignComponentReference<Image>(
                serializedManager,
                "cardHeadImage",
                mainLayout,
                "FighterCardPanel/Content/CardPreviewArea/CardHeadImage",
                missing);

            AssignComponentReference<TMP_Text>(
                serializedManager,
                "cardFighterNameText",
                mainLayout,
                "FighterCardPanel/Content/CardFighterNameText",
                missing);
            AssignComponentReference<TMP_Text>(
                serializedManager,
                "cardAttackText",
                mainLayout,
                "FighterCardPanel/Content/StatsPanel/CardAttackText",
                missing);
            AssignComponentReference<TMP_Text>(
                serializedManager,
                "cardDefenseText",
                mainLayout,
                "FighterCardPanel/Content/StatsPanel/CardDefenseText",
                missing);
            AssignComponentReference<TMP_Text>(
                serializedManager,
                "cardSpeedText",
                mainLayout,
                "FighterCardPanel/Content/StatsPanel/CardSpeedText",
                missing);
            AssignComponentReference<TMP_Text>(
                serializedManager,
                "cardHealthText",
                mainLayout,
                "FighterCardPanel/Content/StatsPanel/CardHealthText",
                missing);
            AssignComponentReference<TMP_Text>(
                serializedManager,
                "cardPowerSummaryText",
                mainLayout,
                "FighterCardPanel/Content/PowerSummaryText",
                missing);

            AssignComponentReference<Button>(
                serializedManager,
                "startBattleButton",
                mainLayout,
                "FighterCardPanel/Content/ButtonRow/StartBattleButton",
                missing);
            AssignComponentReference<Button>(
                serializedManager,
                "backEditButton",
                mainLayout,
                "FighterCardPanel/Content/ButtonRow/BackEditButton",
                missing);
        }

        private static void AssignGameObjectReference(
            SerializedObject serializedManager,
            string propertyName,
            Transform root,
            string relativePath,
            List<string> missing)
        {
            var transform = FindTransform(root, relativePath);
            if (transform == null)
            {
                missing.Add(propertyName + " (GameObject not found: " + relativePath + ")");
                return;
            }

            var property = serializedManager.FindProperty(propertyName);
            if (property == null)
            {
                missing.Add(propertyName + " (SerializedProperty not found on CharacterBuilderManager)");
                return;
            }

            property.objectReferenceValue = transform.gameObject;
        }

        private static void WireSelectorReferences(
            SerializedObject serializedManager,
            Transform mainLayout,
            string selectorName,
            string fieldPrefix,
            List<string> missing)
        {
            AssignComponentReference<Button>(
                serializedManager,
                fieldPrefix + "PreviousButton",
                mainLayout,
                selectorName + "/PreviousButton",
                missing);
            AssignComponentReference<Button>(
                serializedManager,
                fieldPrefix + "NextButton",
                mainLayout,
                selectorName + "/NextButton",
                missing);
            AssignComponentReference<TMP_Text>(
                serializedManager,
                fieldPrefix + "NameText",
                mainLayout,
                selectorName + "/ItemName",
                missing);
        }

        private static void AssignComponentReference<T>(
            SerializedObject serializedManager,
            string propertyName,
            Transform root,
            string relativePath,
            List<string> missing) where T : Component
        {
            var transform = FindTransform(root, relativePath);
            if (transform == null)
            {
                missing.Add(propertyName + " (GameObject not found: " + relativePath + ")");
                return;
            }

            var component = transform.GetComponent<T>();
            if (component == null)
            {
                missing.Add(propertyName + " (missing " + typeof(T).Name + " on " + relativePath + ")");
                return;
            }

            var property = serializedManager.FindProperty(propertyName);
            if (property == null)
            {
                missing.Add(propertyName + " (SerializedProperty not found on CharacterBuilderManager)");
                return;
            }

            property.objectReferenceValue = component;
        }

        private static void AssignEquipmentList<T>(
            SerializedObject serializedManager,
            string propertyName,
            List<T> items) where T : Object
        {
            var property = serializedManager.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError("CharacterBuilderAutoWire: SerializedProperty \"" + propertyName + "\" not found.");
                return;
            }

            property.ClearArray();
            for (var i = 0; i < items.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static List<T> LoadScriptableObjects<T>(string typeName, string folder) where T : Object
        {
            return AssetDatabase.FindAssets("t:" + typeName, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => new { path, asset = AssetDatabase.LoadAssetAtPath<T>(path) })
                .Where(entry => entry.asset != null)
                .OrderBy(entry => Path.GetFileNameWithoutExtension(entry.path))
                .Select(entry => entry.asset)
                .ToList();
        }

        /// <summary>
        /// Tries Transform.Find for a slash-separated path, then falls back to recursive name search.
        /// </summary>
        private static Transform FindTransform(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var found = root.Find(path);
            if (found != null)
            {
                return found;
            }

            var leafName = path;
            var slashIndex = path.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                leafName = path.Substring(slashIndex + 1);
            }

            return FindChildRecursive(root, leafName);
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent.name == childName)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void LogReport(
            int headCount,
            int bodyCount,
            int weaponCount,
            int mountCount,
            List<string> missing)
        {
            var report = new StringBuilder();
            report.AppendLine("CharacterBuilderAutoWire complete.");
            report.AppendLine("Heads assigned: " + headCount);
            report.AppendLine("Bodies assigned: " + bodyCount);
            report.AppendLine("Weapons assigned: " + weaponCount);
            report.AppendLine("Mounts assigned: " + mountCount);

            if (missing.Count == 0)
            {
                report.Append("Missing UI references: none");
            }
            else
            {
                report.Append("Missing UI references: " + string.Join(", ", missing));
            }

            Debug.Log(report.ToString());
        }
    }
}
