using System.IO;
using System.Text;
using Crownsfall.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.Editor
{
    /// <summary>
    /// Adds the Fighter Card confirmation panel under Canvas/MainLayout.
    /// Run once, then use Auto Wire Character Builder to connect references on the manager.
    /// </summary>
    public static class CharacterBuilderFighterCardSetup
    {
        private const string ScenePath = "Assets/Scenes/CharacterBuilder/CharacterBuilder.unity";
        private const string PanelName = "FighterCardPanel";

        [MenuItem("Tools/Fighter Tools/Add Fighter Card Panel")]
        public static void AddFighterCardPanelFromMenu()
        {
            if (!TryEnsureCharacterBuilderScene())
            {
                return;
            }

            var mainLayout = FindMainLayout();
            if (mainLayout == null)
            {
                EditorUtility.DisplayDialog(
                    "Fighter Card Panel",
                    "Could not find Canvas/MainLayout in the active scene.\n\n" +
                    "Use Tools → Fighter Tools → Setup Character Builder Scene first.",
                    "OK");
                return;
            }

            var existing = mainLayout.Find(PanelName);
            if (existing != null)
            {
                var replace = EditorUtility.DisplayDialog(
                    "Fighter Card Panel",
                    PanelName + " already exists under MainLayout.\n\nReplace it?",
                    "Replace",
                    "Cancel");

                if (!replace)
                {
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            var panel = BuildFighterCardPanel(mainLayout);
            panel.SetActive(false);
            panel.transform.SetAsLastSibling();

            MarkSceneDirtyAndSave();

            var summary = BuildSummary(panel);
            Debug.Log(summary);
            EditorUtility.DisplayDialog(
                "Fighter Card Panel",
                summary + "\n\nNext: Tools → Fighter Tools → Auto Wire Character Builder",
                "OK");
        }

        [MenuItem("Tools/Fighter Tools/Rebuild Fighter Card UI")]
        public static void RebuildFighterCardUiLayoutFromMenu()
        {
            if (!TryEnsureCharacterBuilderScene())
            {
                return;
            }

            var panel = FindMainLayout()?.Find(PanelName);
            if (panel == null)
            {
                EditorUtility.DisplayDialog(
                    "Rebuild Fighter Card UI",
                    PanelName + " not found.\n\nRun Add Fighter Card Panel first.",
                    "OK");
                return;
            }

            var cardUi = panel.GetComponent<FighterCardUI>();
            if (cardUi == null)
            {
                cardUi = panel.gameObject.AddComponent<FighterCardUI>();
            }

            for (var i = panel.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(panel.GetChild(i).gameObject);
            }

            cardUi.EnsureBuilt();
            panel.gameObject.SetActive(false);
            panel.SetAsLastSibling();

            MarkSceneDirtyAndSave();

            EditorUtility.DisplayDialog(
                "Rebuild Fighter Card UI",
                "Fighter card layout rebuilt with mobile-safe hierarchy.\n\n" +
                "Run Tools → Fighter Tools → Auto Wire Character Builder.",
                "OK");
        }

        private static bool TryEnsureCharacterBuilderScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path == ScenePath || activeScene.name == "CharacterBuilder")
            {
                return true;
            }

            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Fighter Card Panel",
                    "Character Builder scene not found at:\n" + ScenePath + "\n\n" +
                    "Open the Character Builder scene or run Setup Character Builder Scene first.",
                    "OK");
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static Transform FindMainLayout()
        {
            var canvasObject = GameObject.Find("Canvas");
            if (canvasObject == null)
            {
                return null;
            }

            return canvasObject.transform.Find("MainLayout");
        }

        private static GameObject BuildFighterCardPanel(Transform mainLayout)
        {
            var panelRect = CreateRect(PanelName, mainLayout);
            StretchToParent(panelRect);

            var ignoreLayout = panelRect.gameObject.AddComponent<LayoutElement>();
            ignoreLayout.ignoreLayout = true;

            panelRect.gameObject.AddComponent<FighterCardUI>().EnsureBuilt();

            return panelRect.gameObject;
        }

        private static void MarkSceneDirtyAndSave()
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string BuildSummary(GameObject panel)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Fighter Card Panel added successfully.");
            builder.AppendLine();
            builder.AppendLine("Hierarchy (under Canvas/MainLayout):");
            AppendTransformTree(panel.transform, builder, 0);
            builder.AppendLine();
            builder.AppendLine("Panel starts hidden (SetActive false).");
            builder.AppendLine("FighterCardUI builds SafeArea/Card/ScrollView at runtime.");
            builder.AppendLine("Run Tools → Fighter Tools → Auto Wire Character Builder to wire manager references.");
            return builder.ToString();
        }

        private static void AppendTransformTree(Transform transform, StringBuilder builder, int depth)
        {
            builder.Append(' ', depth * 2);
            builder.AppendLine(transform.name);

            for (var i = 0; i < transform.childCount; i++)
            {
                AppendTransformTree(transform.GetChild(i), builder, depth + 1);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static void StretchToParent(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
