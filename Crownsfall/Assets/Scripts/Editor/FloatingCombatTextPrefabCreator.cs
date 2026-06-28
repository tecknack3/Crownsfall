using Crownsfall.Combat.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.Editor
{
    /// <summary>
    /// Builds the FloatingCombatText prefab used by FloatingCombatTextSpawner.
    /// </summary>
    public static class FloatingCombatTextPrefabCreator
    {
        public const string PrefabPath = "Assets/Prefabs/UI/FloatingCombatText.prefab";

        private const float DefaultFontSize = 64f;
        private const float DefaultFloatDuration = 1.2f;

        /// <summary>
        /// Builds or overwrites the FloatingCombatText prefab asset. Called by Setup Floating Combat Text.
        /// </summary>
        public static void CreateFloatingCombatTextPrefab()
        {
            EnsurePrefabFolderExists();

            var root = new GameObject(
                "FloatingCombatText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(FloatingCombatText));

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(240f, 96f);
            rect.localScale = Vector3.one * 1.2f;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(root.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelObject.AddComponent<TextMeshProUGUI>();
            tmp.text = "-0";
            tmp.font = GetDefaultTmpFont();
            tmp.fontSize = DefaultFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var floatingText = root.GetComponent<FloatingCombatText>();
            var serialized = new SerializedObject(floatingText);
            serialized.FindProperty("label").objectReferenceValue = tmp;
            serialized.FindProperty("floatDuration").floatValue = DefaultFloatDuration;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject prefabAsset;

            if (existingPrefab != null)
            {
                prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[Floating Combat Text] Updated prefab at {PrefabPath}");
            }
            else
            {
                prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[Floating Combat Text] Created prefab at {PrefabPath}");
            }

            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
        }

        /// <summary>
        /// Returns the prefab asset, creating it first when missing.
        /// </summary>
        public static FloatingCombatText LoadOrCreatePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<FloatingCombatText>(PrefabPath);
            if (existing != null)
            {
                return existing;
            }

            CreateFloatingCombatTextPrefab();
            return AssetDatabase.LoadAssetAtPath<FloatingCombatText>(PrefabPath);
        }

        private static void EnsurePrefabFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
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
    }
}
