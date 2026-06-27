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
    /// Generates the Character Builder UI hierarchy under an existing Canvas.
    /// UI components only — no runtime scripts are attached.
    /// </summary>
    public static class CharacterBuilderUIGenerator
    {
        private const string ScenePath = "Assets/Scenes/CharacterBuilder/CharacterBuilder.unity";
        private const string RootName = "CharacterBuilderUI";
        private const string PrefabPath = "Assets/Prefabs/UI/CharacterBuilderUI.prefab";

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        private const float TouchButtonSize = 110f;

        [MenuItem("Tools/Fighter Tools/Generate Character Builder UI")]
        public static void GenerateFromMenu()
        {
            if (!TryEnsureCharacterBuilderScene())
            {
                return;
            }

            var canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Character Builder UI",
                    "No Canvas found in the scene.\n\n" +
                    "Use Tools → Fighter Tools → Setup Character Builder Scene first, " +
                    "or add a Canvas manually.",
                    "OK");
                return;
            }

            EnsureCanvasScaler(canvas);
            RemoveExistingRoot(canvas.transform);

            var root = BuildHierarchy(canvas.transform);
            SavePrefab(root);
            MarkSceneDirtyAndSave();

            var summary = BuildHierarchySummary(root);
            Debug.Log(summary);
            EditorUtility.DisplayDialog("Character Builder UI", summary, "OK");
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
                    "Character Builder UI",
                    "Character Builder scene not found at:\n" + ScenePath + "\n\n" +
                    "Open the Character Builder scene or run Setup Character Builder Scene first.",
                    "OK");
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static Canvas FindCanvas()
        {
            return Object.FindObjectOfType<Canvas>();
        }

        private static void EnsureCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        private static void RemoveExistingRoot(Transform canvasTransform)
        {
            var existing = canvasTransform.Find(RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static GameObject BuildHierarchy(Transform canvasTransform)
        {
            var root = CreateRect(RootName, canvasTransform);
            StretchToParent(root);

            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 64, 64);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateBackgroundPanel(root);
            CreateTitle(root);
            CreateFighterNameSection(root);
            CreatePreviewPanel(root);
            CreateSelectionPanel(root);
            CreateCreateFighterButton(root);

            return root.gameObject;
        }

        private static void CreateBackgroundPanel(RectTransform root)
        {
            var panel = CreateRect("BackgroundPanel", root);
            StretchToParent(panel);

            var ignoreLayout = panel.gameObject.AddComponent<LayoutElement>();
            ignoreLayout.ignoreLayout = true;

            var image = panel.gameObject.AddComponent<Image>();
            image.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            image.raycastTarget = false;

            panel.SetAsFirstSibling();
        }

        private static void CreateTitle(RectTransform root)
        {
            var title = CreateTmpText("TitleText", root, "Create Fighter", 52, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        }

        private static void CreateFighterNameSection(RectTransform root)
        {
            var section = CreateRect("FighterNameSection", root);
            AddVerticalSectionLayout(section, spacing: 10f);

            var label = CreateTmpText("FighterNameLabel", section, "Fighter Name", 32, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            CreateFighterNameInput(section);
        }

        private static void CreateFighterNameInput(RectTransform section)
        {
            var inputRoot = CreateRect("FighterNameInput", section);
            inputRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;

            var background = inputRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.24f, 1f);

            var inputField = inputRoot.gameObject.AddComponent<TMP_InputField>();
            inputField.textViewport = CreateInputViewport(inputRoot, out var textComponent, out var placeholder);
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholder;
            inputField.fontAsset = GetDefaultTmpFont();
            inputField.pointSize = 34f;
            inputField.text = string.Empty;
        }

        private static RectTransform CreateInputViewport(
            RectTransform parent,
            out TextMeshProUGUI textComponent,
            out TextMeshProUGUI placeholder)
        {
            var viewport = CreateRect("Text Area", parent);
            StretchToParent(viewport, new Vector2(20f, 10f), new Vector2(-20f, -10f));

            textComponent = CreateTmpText("Text", viewport, string.Empty, 34, FontStyles.Normal);
            StretchToParent(textComponent.rectTransform);

            placeholder = CreateTmpText("Placeholder", viewport, "Enter fighter name...", 34, FontStyles.Italic);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            StretchToParent(placeholder.rectTransform);

            return viewport;
        }

        private static void CreatePreviewPanel(RectTransform root)
        {
            var panel = CreateRect("PreviewPanel", root);
            panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 480f;

            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

            CreatePreviewLayer(panel, "MountImage", 0);
            CreatePreviewLayer(panel, "BodyImage", 1);
            CreatePreviewLayer(panel, "WeaponImage", 2);
            CreatePreviewLayer(panel, "HeadImage", 3);
        }

        private static void CreatePreviewLayer(RectTransform parent, string layerName, int siblingIndex)
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
            image.color = new Color(1f, 1f, 1f, 0.15f);
        }

        private static void CreateSelectionPanel(RectTransform root)
        {
            var panel = CreateRect("SelectionPanel", root);
            AddVerticalSectionLayout(panel, spacing: 16f);

            CreateEquipmentSelector(panel, "HeadSelector", "HeadIconImage", "HeadNameText");
            CreateEquipmentSelector(panel, "BodySelector", "BodyIconImage", "BodyNameText");
            CreateEquipmentSelector(panel, "WeaponSelector", "WeaponIconImage", "WeaponNameText");
            CreateEquipmentSelector(panel, "MountSelector", "MountIconImage", "MountNameText");
        }

        private static void CreateEquipmentSelector(
            RectTransform parent,
            string selectorName,
            string iconName,
            string nameTextObjectName)
        {
            var selector = CreateRect(selectorName, parent);
            selector.gameObject.AddComponent<LayoutElement>().preferredHeight = 130f;

            var background = selector.gameObject.AddComponent<Image>();
            background.color = new Color(0.14f, 0.16f, 0.22f, 1f);

            var horizontal = selector.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(20, 20, 12, 12);
            horizontal.spacing = 16f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = true;

            CreateNavigationButton(selector, "PreviousButton", "Prev");
            CreateSelectorIcon(selector, iconName);

            var nameText = CreateTmpText(nameTextObjectName, selector, "None", 30, FontStyles.Normal);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateNavigationButton(selector, "NextButton", "Next");
        }

        private static void CreateSelectorIcon(RectTransform parent, string iconName)
        {
            var icon = CreateRect(iconName, parent);
            var layout = icon.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 96f;
            layout.preferredHeight = 96f;

            var image = icon.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0.2f);
        }

        private static void CreateNavigationButton(RectTransform parent, string buttonName, string label)
        {
            var buttonRect = CreateRect(buttonName, parent);

            var layout = buttonRect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = TouchButtonSize;
            layout.preferredHeight = TouchButtonSize;

            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(0.24f, 0.42f, 0.72f, 1f);

            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.34f, 0.52f, 0.82f, 1f);
            colors.pressedColor = new Color(0.18f, 0.32f, 0.58f, 1f);
            button.colors = colors;

            var text = CreateTmpText("Label", buttonRect, label, 36, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchToParent(text.rectTransform);
        }

        private static void CreateCreateFighterButton(RectTransform root)
        {
            var buttonRect = CreateRect("CreateFighterButton", root);
            buttonRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;

            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.62f, 0.36f, 1f);

            var button = buttonRect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.24f, 0.72f, 0.42f, 1f);
            colors.pressedColor = new Color(0.12f, 0.48f, 0.28f, 1f);
            button.colors = colors;

            var text = CreateTmpText("Label", buttonRect, "Create Fighter", 38, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchToParent(text.rectTransform);
        }

        private static void SavePrefab(GameObject root)
        {
            EnsurePrefabFolderExists();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Character Builder UI prefab saved to {PrefabPath}");
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

        private static void MarkSceneDirtyAndSave()
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string BuildHierarchySummary(GameObject root)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Character Builder UI generated successfully.");
            builder.AppendLine();
            builder.AppendLine("Hierarchy:");
            AppendTransformTree(root.transform, builder, 0);
            builder.AppendLine();
            builder.AppendLine($"Prefab: {PrefabPath}");
            builder.AppendLine("Run again anytime from Tools → Fighter Tools → Generate Character Builder UI.");
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
            layout.childAlignment = TextAnchor.UpperLeft;
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
