using System.IO;
using System.Text;
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

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        private const float TouchButtonSize = 120f;

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

        /// <summary>
        /// Adds PowerSummaryText under an existing FighterCardPanel/Content if it is missing.
        /// </summary>
        [MenuItem("Tools/Fighter Tools/Add Power Summary To Fighter Card")]
        public static void AddPowerSummaryToExistingPanelFromMenu()
        {
            if (!TryEnsureCharacterBuilderScene())
            {
                return;
            }

            var mainLayout = FindMainLayout();
            if (mainLayout == null)
            {
                EditorUtility.DisplayDialog(
                    "Power Summary",
                    "Could not find Canvas/MainLayout in the active scene.",
                    "OK");
                return;
            }

            var content = mainLayout.Find(PanelName + "/Content");
            if (content == null)
            {
                EditorUtility.DisplayDialog(
                    "Power Summary",
                    PanelName + " not found.\n\nRun Add Fighter Card Panel first.",
                    "OK");
                return;
            }

            var existing = content.Find("PowerSummaryText");
            if (existing != null)
            {
                EditorUtility.DisplayDialog(
                    "Power Summary",
                    "PowerSummaryText already exists on the Fighter Card.",
                    "OK");
                return;
            }

            CreatePowerSummaryText(content.GetComponent<RectTransform>());

            // Place after fighter name, before stats panel.
            var summary = content.Find("PowerSummaryText");
            var nameText = content.Find("CardFighterNameText");
            if (summary != null && nameText != null)
            {
                summary.SetSiblingIndex(nameText.GetSiblingIndex() + 1);
            }

            MarkSceneDirtyAndSave();

            EditorUtility.DisplayDialog(
                "Power Summary",
                "PowerSummaryText added under FighterCardPanel/Content.\n\n" +
                "Run Tools → Fighter Tools → Auto Wire Character Builder to wire the reference.",
                "OK");
        }

        /// <summary>
        /// Adds SkillsSummaryText under an existing FighterCardPanel/Content if it is missing.
        /// </summary>
        [MenuItem("Tools/Fighter Tools/Add Skills Summary To Fighter Card")]
        public static void AddSkillsSummaryToExistingPanelFromMenu()
        {
            if (!TryEnsureCharacterBuilderScene())
            {
                return;
            }

            var mainLayout = FindMainLayout();
            if (mainLayout == null)
            {
                EditorUtility.DisplayDialog(
                    "Skills Summary",
                    "Could not find Canvas/MainLayout in the active scene.",
                    "OK");
                return;
            }

            var content = mainLayout.Find(PanelName + "/Content");
            if (content == null)
            {
                EditorUtility.DisplayDialog(
                    "Skills Summary",
                    PanelName + " not found.\n\nRun Add Fighter Card Panel first.",
                    "OK");
                return;
            }

            var existing = content.Find("SkillsSummaryText");
            if (existing != null)
            {
                EditorUtility.DisplayDialog(
                    "Skills Summary",
                    "SkillsSummaryText already exists on the Fighter Card.",
                    "OK");
                return;
            }

            CreateSkillsSummaryText(content.GetComponent<RectTransform>());

            // Place after power summary, before stats panel.
            var summary = content.Find("SkillsSummaryText");
            var powerSummary = content.Find("PowerSummaryText");
            if (summary != null && powerSummary != null)
            {
                summary.SetSiblingIndex(powerSummary.GetSiblingIndex() + 1);
            }

            MarkSceneDirtyAndSave();

            EditorUtility.DisplayDialog(
                "Skills Summary",
                "SkillsSummaryText added under FighterCardPanel/Content.\n\n" +
                "Run Tools → Fighter Tools → Auto Wire Character Builder to wire the reference.",
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

            // Full-screen overlay; ignore the vertical layout on MainLayout.
            var ignoreLayout = panelRect.gameObject.AddComponent<LayoutElement>();
            ignoreLayout.ignoreLayout = true;

            var overlay = panelRect.gameObject.AddComponent<Image>();
            overlay.color = new Color(0.04f, 0.06f, 0.1f, 0.96f);
            overlay.raycastTarget = true;

            var content = CreateRect("Content", panelRect);
            StretchToParent(content, new Vector2(48f, 64f), new Vector2(-48f, -64f));

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateTitle(content);
            CreateCardPreviewArea(content);
            CreateFighterNameText(content);
            CreatePowerSummaryText(content);
            CreateSkillsSummaryText(content);
            CreateStatTexts(content);
            CreateActionButtons(content);

            return panelRect.gameObject;
        }

        private static void CreateTitle(RectTransform parent)
        {
            var title = CreateTmpText("TitleText", parent, "Fighter Created", 52, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        }

        private static void CreateCardPreviewArea(RectTransform parent)
        {
            var preview = CreateRect("CardPreviewArea", parent);
            preview.gameObject.AddComponent<LayoutElement>().preferredHeight = 480f;

            var background = preview.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            background.raycastTarget = false;

            // Stack order back to front: mount, body, weapon, head.
            CreateCardPreviewLayer(preview, "CardMountImage", 0);
            CreateCardPreviewLayer(preview, "CardBodyImage", 1);
            CreateCardPreviewLayer(preview, "CardWeaponImage", 2);
            CreateCardPreviewLayer(preview, "CardHeadImage", 3);
        }

        private static void CreateCardPreviewLayer(RectTransform parent, string layerName, int siblingIndex)
        {
            var layer = CreateRect(layerName, parent);
            layer.SetSiblingIndex(siblingIndex);

            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.sizeDelta = new Vector2(400f, 400f);
            layer.anchoredPosition = Vector2.zero;

            var image = layer.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
        }

        private static void CreateFighterNameText(RectTransform parent)
        {
            var nameText = CreateTmpText("CardFighterNameText", parent, "Fighter Name", 40, FontStyles.Bold);
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        }

        private static void CreatePowerSummaryText(RectTransform parent)
        {
            var summary = CreateTmpText(
                "PowerSummaryText",
                parent,
                "Head: Common\nBody: Rare\nWeapon: Legendary\nMount: Epic",
                28,
                FontStyles.Normal);
            summary.alignment = TextAlignmentOptions.Center;
            summary.richText = true;
            summary.gameObject.AddComponent<LayoutElement>().preferredHeight = 140f;
        }

        private static void CreateSkillsSummaryText(RectTransform parent)
        {
            var summary = CreateTmpText(
                "SkillsSummaryText",
                parent,
                "Skills\nHead: None\nBody: None\nWeapon: None\nMount: None",
                28,
                FontStyles.Normal);
            summary.alignment = TextAlignmentOptions.Center;
            summary.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;
        }

        private static void CreateStatTexts(RectTransform parent)
        {
            var statsPanel = CreateRect("StatsPanel", parent);
            AddVerticalSectionLayout(statsPanel, spacing: 8f);
            statsPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 200f;

            CreateStatLine(statsPanel, "CardAttackText", "Attack: 0");
            CreateStatLine(statsPanel, "CardDefenseText", "Defense: 0");
            CreateStatLine(statsPanel, "CardSpeedText", "Speed: 0");
            CreateStatLine(statsPanel, "CardHealthText", "Health: 0");
        }

        private static void CreateStatLine(RectTransform parent, string objectName, string defaultText)
        {
            var statText = CreateTmpText(objectName, parent, defaultText, 32, FontStyles.Normal);
            statText.alignment = TextAlignmentOptions.Center;
            statText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        }

        private static void CreateActionButtons(RectTransform parent)
        {
            var row = CreateRect("ButtonRow", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = TouchButtonSize;

            var horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 24f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;

            CreatePanelButton(row, "BackEditButton", "Back / Edit", new Color(0.24f, 0.42f, 0.72f, 1f));
            CreatePanelButton(row, "StartBattleButton", "Start Battle", new Color(0.18f, 0.62f, 0.36f, 1f));
        }

        private static void CreatePanelButton(RectTransform parent, string buttonName, string label, Color color)
        {
            var buttonRect = CreateRect(buttonName, parent);

            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = color;

            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.75f;
            button.colors = colors;

            var text = CreateTmpText("Label", buttonRect, label, 34, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchToParent(text.rectTransform);
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

        private static void AddVerticalSectionLayout(RectTransform section, float spacing)
        {
            var layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static TextMeshProUGUI CreateTmpText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            FontStyles fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
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
    }
}
