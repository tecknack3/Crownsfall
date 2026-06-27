using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crownsfall.Characters;
using Crownsfall.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crownsfall.Editor
{
    public static class CharacterBuilderSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/CharacterBuilder/CharacterBuilder.unity";

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        private const float TouchButtonSize = 120f;

        [MenuItem("Tools/Fighter Tools/Setup Character Builder Scene")]
        public static void SetupSceneFromMenu()
        {
            SetupScene();
            EditorUtility.DisplayDialog(
                "Character Builder",
                "Character Builder scene created at:\n" + ScenePath,
                "OK");
        }

        public static void SetupScene()
        {
            EnsureSceneFolderExists();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = CreateCamera();
            var eventSystem = CreateEventSystem();
            var canvas = CreateCanvas(out var canvasRect);
            var managerObject = CreateManagerObject();

            var mainLayout = CreateVerticalLayout(
                "MainLayout",
                canvasRect,
                padding: new RectOffset(48, 48, 64, 64),
                spacing: 32f,
                childAlignment: TextAnchor.UpperCenter);

            CreateTitle(mainLayout);
            var preview = CreatePreviewArea(mainLayout);
            CreateNameInput(mainLayout, out var nameInput);
            var headSelector = CreateEquipmentSelector(mainLayout, "HeadSelector", "Head");
            var bodySelector = CreateEquipmentSelector(mainLayout, "BodySelector", "Body");
            var weaponSelector = CreateEquipmentSelector(mainLayout, "WeaponSelector", "Weapon");
            var mountSelector = CreateEquipmentSelector(mainLayout, "MountSelector", "Mount");
            var createButton = CreateCreateButton(mainLayout);

            var manager = managerObject.GetComponent<CharacterBuilderManager>();
            PopulateEquipmentLists(manager);

            AssignManagerReferences(
                manager,
                nameInput,
                headSelector,
                bodySelector,
                weaponSelector,
                mountSelector,
                preview,
                createButton);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Character Builder scene saved to {ScenePath}");
        }

        private static void EnsureSceneFolderExists()
        {
            var folder = Path.GetDirectoryName(ScenePath);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "CharacterBuilder");
            }
        }

        private static GameObject CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            cameraObject.AddComponent<AudioListener>();
            return cameraObject;
        }

        private static GameObject CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            return eventSystemObject;
        }

        private static GameObject CreateCanvas(out RectTransform canvasRect)
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            canvasRect = canvasObject.GetComponent<RectTransform>();
            StretchToParent(canvasRect);
            return canvasObject;
        }

        private static GameObject CreateManagerObject()
        {
            var managerObject = new GameObject("CharacterBuilderManager");
            managerObject.AddComponent<CharacterBuilderManager>();
            return managerObject;
        }

        private static RectTransform CreateVerticalLayout(
            string name,
            Transform parent,
            RectOffset padding,
            float spacing,
            TextAnchor childAlignment)
        {
            var layoutObject = new GameObject(name, typeof(RectTransform));
            layoutObject.transform.SetParent(parent, false);

            var rect = layoutObject.GetComponent<RectTransform>();
            StretchToParent(rect);

            var layout = layoutObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = childAlignment;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            layoutObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private static void CreateTitle(Transform parent)
        {
            var titleObject = CreateTextObject("Title", parent, "Create Your Fighter", 56, FontStyles.Bold);
            var layout = titleObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 80f;
        }

        private static FighterPreview CreatePreviewArea(Transform parent)
        {
            var previewRoot = new GameObject("FighterPreview", typeof(RectTransform));
            previewRoot.transform.SetParent(parent, false);

            var previewLayout = previewRoot.AddComponent<LayoutElement>();
            previewLayout.preferredHeight = 520f;
            previewLayout.flexibleHeight = 0f;

            var previewRect = previewRoot.GetComponent<RectTransform>();
            previewRect.sizeDelta = new Vector2(0f, 520f);

            var background = previewRoot.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

            var stack = new GameObject("PreviewStack", typeof(RectTransform));
            stack.transform.SetParent(previewRoot.transform, false);
            var stackRect = stack.GetComponent<RectTransform>();
            StretchToParent(stackRect);

            var mountImage = CreatePreviewLayer(stack.transform, "MountImage", 0);
            var bodyImage = CreatePreviewLayer(stack.transform, "BodyImage", 1);
            var weaponImage = CreatePreviewLayer(stack.transform, "WeaponImage", 2);
            var headImage = CreatePreviewLayer(stack.transform, "HeadImage", 3);

            var preview = previewRoot.AddComponent<FighterPreview>();
            AssignPreviewReferences(preview, mountImage, bodyImage, weaponImage, headImage);
            return preview;
        }

        private static Image CreatePreviewLayer(Transform parent, string layerName, int siblingIndex)
        {
            var layerObject = new GameObject(layerName, typeof(RectTransform));
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.SetSiblingIndex(siblingIndex);

            var rect = layerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 420f);
            rect.anchoredPosition = Vector2.zero;

            var image = layerObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void CreateNameInput(Transform parent, out TMP_InputField inputField)
        {
            var section = CreateSection(parent, "NameSection", "Fighter Name");

            var inputRoot = new GameObject("FighterNameInput", typeof(RectTransform));
            inputRoot.transform.SetParent(section, false);

            var inputLayout = inputRoot.AddComponent<LayoutElement>();
            inputLayout.preferredHeight = 96f;

            var background = inputRoot.AddComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.24f, 1f);

            inputField = inputRoot.AddComponent<TMP_InputField>();
            inputField.textViewport = CreateInputViewport(inputRoot.transform, out var textComponent, out var placeholder);

            inputField.textComponent = textComponent;
            inputField.placeholder = placeholder;
            inputField.fontAsset = GetDefaultTmpFont();
            inputField.pointSize = 36f;
            inputField.text = string.Empty;
        }

        private static RectTransform CreateInputViewport(
            Transform parent,
            out TextMeshProUGUI textComponent,
            out TextMeshProUGUI placeholder)
        {
            var viewport = new GameObject("Text Area", typeof(RectTransform));
            viewport.transform.SetParent(parent, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            StretchToParent(viewportRect, new Vector2(24f, 12f), new Vector2(-24f, -12f));

            textComponent = CreateTmpText("Text", viewport.transform, string.Empty, 36, FontStyles.Normal);
            StretchToParent(textComponent.rectTransform);

            placeholder = CreateTmpText("Placeholder", viewport.transform, "Enter fighter name...", 36, FontStyles.Italic);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            StretchToParent(placeholder.rectTransform);

            return viewportRect;
        }

        private static EquipmentSelector CreateEquipmentSelector(Transform parent, string objectName, string slotLabel)
        {
            var selectorRoot = new GameObject(objectName, typeof(RectTransform));
            selectorRoot.transform.SetParent(parent, false);

            var rootLayout = selectorRoot.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = 140f;

            var background = selectorRoot.AddComponent<Image>();
            background.color = new Color(0.14f, 0.16f, 0.22f, 1f);

            var horizontal = selectorRoot.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(24, 24, 16, 16);
            horizontal.spacing = 20f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = true;

            var label = CreateTmpText("SlotLabel", selectorRoot.transform, slotLabel, 34, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;

            var previousButton = CreateNavigationButton(selectorRoot.transform, "PreviousButton", "<");
            var iconObject = CreateIconDisplay(selectorRoot.transform, out var iconImage);
            var nextButton = CreateNavigationButton(selectorRoot.transform, "NextButton", ">");
            var itemName = CreateTmpText("ItemName", selectorRoot.transform, "None", 30, FontStyles.Normal);
            itemName.alignment = TextAlignmentOptions.MidlineLeft;
            itemName.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var selector = selectorRoot.AddComponent<EquipmentSelector>();
            AssignSelectorReferences(selector, iconImage, previousButton, nextButton, label, itemName);
            return selector;
        }

        private static void AssignSelectorReferences(
            EquipmentSelector selector,
            Image iconImage,
            Button previousButton,
            Button nextButton,
            TextMeshProUGUI label,
            TextMeshProUGUI itemName)
        {
            var serializedSelector = new SerializedObject(selector);
            serializedSelector.FindProperty("iconImage").objectReferenceValue = iconImage;
            serializedSelector.FindProperty("previousButton").objectReferenceValue = previousButton;
            serializedSelector.FindProperty("nextButton").objectReferenceValue = nextButton;
            serializedSelector.FindProperty("labelText").objectReferenceValue = label;
            serializedSelector.FindProperty("itemNameText").objectReferenceValue = itemName;
            serializedSelector.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreateNavigationButton(Transform parent, string name, string label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = TouchButtonSize;
            layout.preferredHeight = TouchButtonSize;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.24f, 0.42f, 0.72f, 1f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.34f, 0.52f, 0.82f, 1f);
            colors.pressedColor = new Color(0.18f, 0.32f, 0.58f, 1f);
            button.colors = colors;

            var text = CreateTmpText("Label", buttonObject.transform, label, 48, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchToParent(text.rectTransform);

            return button;
        }

        private static GameObject CreateIconDisplay(Transform parent, out Image iconImage)
        {
            var iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);

            var layout = iconObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 96f;
            layout.preferredHeight = 96f;

            iconImage = iconObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;
            return iconObject;
        }

        private static Button CreateCreateButton(Transform parent)
        {
            var buttonObject = new GameObject("CreateFighterButton", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 120f;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.62f, 0.36f, 1f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.24f, 0.72f, 0.42f, 1f);
            colors.pressedColor = new Color(0.12f, 0.48f, 0.28f, 1f);
            button.colors = colors;

            var text = CreateTmpText("Label", buttonObject.transform, "Create Fighter", 40, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            StretchToParent(text.rectTransform);

            return button;
        }

        private static Transform CreateSection(Transform parent, string sectionName, string headerText)
        {
            var section = new GameObject(sectionName, typeof(RectTransform));
            section.transform.SetParent(parent, false);

            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var header = CreateTmpText("Header", section.transform, headerText, 32, FontStyles.Bold);
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            return section.transform;
        }

        private static GameObject CreateTextObject(
            string name,
            Transform parent,
            string text,
            float fontSize,
            FontStyles fontStyle)
        {
            var textObject = CreateTmpText(name, parent, text, fontSize, fontStyle);
            textObject.alignment = TextAlignmentOptions.Center;
            return textObject.gameObject;
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

        private static void PopulateEquipmentLists(CharacterBuilderManager manager)
        {
            var heads = LoadEquipment<HeadSO>("Assets/ScriptableObjects/Heads");
            var bodies = LoadEquipment<BodySO>("Assets/ScriptableObjects/Bodies");
            var weapons = LoadEquipment<WeaponSO>("Assets/ScriptableObjects/Weapons");
            var mounts = LoadEquipment<MountSO>("Assets/ScriptableObjects/Mounts");

            var serializedManager = new SerializedObject(manager);
            AssignEquipmentList(serializedManager, "headOptions", heads);
            AssignEquipmentList(serializedManager, "bodyOptions", bodies);
            AssignEquipmentList(serializedManager, "weaponOptions", weapons);
            AssignEquipmentList(serializedManager, "mountOptions", mounts);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static void AssignEquipmentList<T>(
            SerializedObject serializedManager,
            string propertyName,
            List<T> items) where T : Object
        {
            var property = serializedManager.FindProperty(propertyName);
            property.ClearArray();
            for (var i = 0; i < items.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static List<T> LoadEquipment<T>(string folder) where T : EquipmentItemSO
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(item => item != null)
                .OrderBy(item => item.itemName)
                .ToList();
        }

        private static void AssignManagerReferences(
            CharacterBuilderManager manager,
            TMP_InputField nameInput,
            EquipmentSelector headSelector,
            EquipmentSelector bodySelector,
            EquipmentSelector weaponSelector,
            EquipmentSelector mountSelector,
            FighterPreview preview,
            Button createButton)
        {
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("fighterNameInput").objectReferenceValue = nameInput;
            serializedManager.FindProperty("headSelector").objectReferenceValue = headSelector;
            serializedManager.FindProperty("bodySelector").objectReferenceValue = bodySelector;
            serializedManager.FindProperty("weaponSelector").objectReferenceValue = weaponSelector;
            serializedManager.FindProperty("mountSelector").objectReferenceValue = mountSelector;
            serializedManager.FindProperty("fighterPreview").objectReferenceValue = preview;
            serializedManager.FindProperty("createFighterButton").objectReferenceValue = createButton;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignPreviewReferences(
            FighterPreview preview,
            Image mountImage,
            Image bodyImage,
            Image weaponImage,
            Image headImage)
        {
            var serializedPreview = new SerializedObject(preview);
            serializedPreview.FindProperty("mountImage").objectReferenceValue = mountImage;
            serializedPreview.FindProperty("bodyImage").objectReferenceValue = bodyImage;
            serializedPreview.FindProperty("weaponImage").objectReferenceValue = weaponImage;
            serializedPreview.FindProperty("headImage").objectReferenceValue = headImage;
            serializedPreview.ApplyModifiedPropertiesWithoutUndo();
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
