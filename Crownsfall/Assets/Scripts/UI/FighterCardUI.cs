using System.Text;
using Crownsfall.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.UI
{
    /// <summary>
    /// Mobile-friendly fighter confirmation modal shown after Create Fighter.
    /// Builds its own hierarchy at runtime so layout stays consistent across devices.
    /// </summary>
    [DisallowMultipleComponent]
    public class FighterCardUI : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color CardColor = new Color(0.1f, 0.12f, 0.16f, 0.98f);
        private static readonly Color PreviewBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
        private static readonly Color SubtitleColor = new Color(0.72f, 0.76f, 0.82f, 1f);
        private static readonly Color SectionHeaderColor = new Color(0.82f, 0.86f, 0.92f, 1f);
        private static readonly Color SlotLabelColor = new Color(0.65f, 0.7f, 0.78f, 1f);
        private static readonly Color BackButtonColor = new Color(0.24f, 0.42f, 0.72f, 1f);
        private static readonly Color StartButtonColor = new Color(0.18f, 0.62f, 0.36f, 1f);

        private const float PreviewAreaHeight = 260f;
        private const float PreviewImageSize = 220f;
        private const float ButtonRowHeight = 96f;
        private const float SafeAreaInset = 16f;

        [Header("Built at runtime — wired by CharacterBuilderManager")]
        public Image cardMountImage;
        public Image cardBodyImage;
        public Image cardWeaponImage;
        public Image cardHeadImage;

        public TMP_Text cardFighterNameText;
        public TMP_Text cardRarityBadgeText;
        public TMP_Text cardAttackText;
        public TMP_Text cardDefenseText;
        public TMP_Text cardSpeedText;
        public TMP_Text cardHealthText;
        public TMP_Text cardPowerSummaryText;
        public TMP_Text cardSkillsSummaryText;

        public Button startBattleButton;
        public Button backEditButton;

        private bool _uiBuilt;
        private CanvasGroup _canvasGroup;
        private Image _backdropImage;
        private RectTransform _panelRect;
        private RectTransform _safeAreaRect;
        private ScrollRect _scrollRect;
        private TMP_Text _headEquipmentText;
        private TMP_Text _bodyEquipmentText;
        private TMP_Text _weaponEquipmentText;
        private TMP_Text _mountEquipmentText;

        /// <summary>
        /// Builds or repairs the modal hierarchy. Safe to call multiple times.
        /// </summary>
        public void EnsureBuilt()
        {
            if (_uiBuilt)
            {
                ApplyPanelLayout();
                return;
            }

            _uiBuilt = true;
            _panelRect = transform as RectTransform;
            ClearLegacyContent();

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;

            _backdropImage = GetComponent<Image>();
            if (_backdropImage == null)
            {
                _backdropImage = gameObject.AddComponent<Image>();
            }

            _backdropImage.color = BackdropColor;
            _backdropImage.raycastTarget = true;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _safeAreaRect = CreateRect("SafeArea", _panelRect);
            StretchFull(_safeAreaRect);

            var cardRect = CreateRect("Card", _safeAreaRect);
            StretchFull(cardRect);

            var cardImage = cardRect.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            cardImage.raycastTarget = true;

            var cardLayout = cardRect.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(24, 24, 20, 20);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateHeader(cardRect);
            BuildScrollableBody(cardRect);
            BuildButtonRow(cardRect);

            ApplyPanelLayout();
        }

        public void Show(PlayerFighter fighter)
        {
            if (fighter == null)
            {
                return;
            }

            EnsureBuilt();
            ApplyPanelLayout();
            PopulateContent(fighter);

            gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Hide()
        {
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private void PopulateContent(PlayerFighter fighter)
        {
            UpdatePreviewImage(cardMountImage, fighter.mount?.icon);
            UpdatePreviewImage(cardBodyImage, fighter.body?.icon);
            UpdatePreviewImage(cardWeaponImage, fighter.weapon?.icon);
            UpdatePreviewImage(cardHeadImage, fighter.head?.icon);

            SetText(cardFighterNameText, fighter.fighterName);
            UpdateRarityBadge(fighter);

            SetText(cardAttackText, fighter.attack.ToString());
            SetText(cardDefenseText, fighter.defense.ToString());
            SetText(cardSpeedText, fighter.speed.ToString());
            SetText(cardHealthText, fighter.maxHealth.ToString());

            UpdateEquipmentSummary(fighter);
            UpdateSkillsSummary(fighter);
        }

        private void CreateHeader(RectTransform cardRect)
        {
            var headerRect = CreateRect("Header", cardRect);
            AddFixedHeight(headerRect, 88f);

            var headerLayout = headerRect.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.spacing = 4f;
            headerLayout.childAlignment = TextAnchor.UpperCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            CreateCenteredText(headerRect, "TitleText", "Fighter Created!", 40f, FontStyles.Bold, Color.white);
            CreateCenteredText(headerRect, "SubtitleText", "Ready for battle", 26f, FontStyles.Normal, SubtitleColor);
        }

        private void BuildScrollableBody(RectTransform cardRect)
        {
            var scrollViewRect = CreateRect("ScrollView", cardRect);
            var scrollLayoutElement = scrollViewRect.gameObject.AddComponent<LayoutElement>();
            scrollLayoutElement.flexibleHeight = 1f;
            scrollLayoutElement.minHeight = 280f;

            var viewportRect = CreateRect("Viewport", scrollViewRect);
            StretchFull(viewportRect);
            viewportRect.gameObject.AddComponent<RectMask2D>();

            var scrollContentRect = CreateRect("ScrollContent", viewportRect);
            scrollContentRect.anchorMin = new Vector2(0f, 1f);
            scrollContentRect.anchorMax = new Vector2(1f, 1f);
            scrollContentRect.pivot = new Vector2(0.5f, 1f);
            scrollContentRect.anchoredPosition = Vector2.zero;
            scrollContentRect.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = scrollContentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(4, 4, 4, 8);
            contentLayout.spacing = 14f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = scrollContentRect.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect = scrollViewRect.gameObject.AddComponent<ScrollRect>();
            _scrollRect.content = scrollContentRect;
            _scrollRect.viewport = viewportRect;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 24f;

            BuildPreviewSection(scrollContentRect);
            BuildStatsGrid(scrollContentRect);
            BuildEquipmentSection(scrollContentRect);
            BuildSkillsSection(scrollContentRect);
        }

        private void BuildPreviewSection(RectTransform scrollContentRect)
        {
            var previewArea = CreateRect("CardPreviewArea", scrollContentRect);
            AddFixedHeight(previewArea, PreviewAreaHeight);

            var previewBackground = previewArea.gameObject.AddComponent<Image>();
            previewBackground.color = PreviewBackgroundColor;
            previewBackground.raycastTarget = false;

            cardMountImage = CreatePreviewLayer(previewArea, "CardMountImage", 0);
            cardBodyImage = CreatePreviewLayer(previewArea, "CardBodyImage", 1);
            cardWeaponImage = CreatePreviewLayer(previewArea, "CardWeaponImage", 2);
            cardHeadImage = CreatePreviewLayer(previewArea, "CardHeadImage", 3);

            cardFighterNameText = CreateCenteredText(
                scrollContentRect,
                "CardFighterNameText",
                "Fighter Name",
                34f,
                FontStyles.Bold,
                Color.white);
            AddFixedHeight(cardFighterNameText.rectTransform, 44f);

            cardRarityBadgeText = CreateCenteredText(
                scrollContentRect,
                "RarityBadgeText",
                string.Empty,
                24f,
                FontStyles.Bold,
                Color.white);
            cardRarityBadgeText.richText = true;
            AddFixedHeight(cardRarityBadgeText.rectTransform, 32f);
        }

        private static Image CreatePreviewLayer(RectTransform parent, string layerName, int siblingIndex)
        {
            var layer = CreateRect(layerName, parent);
            layer.SetSiblingIndex(siblingIndex);
            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = new Vector2(0.5f, 0.5f);
            layer.sizeDelta = new Vector2(PreviewImageSize, PreviewImageSize);
            layer.anchoredPosition = Vector2.zero;

            var image = layer.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private void BuildStatsGrid(RectTransform scrollContentRect)
        {
            var statsPanel = CreateRect("StatsPanel", scrollContentRect);
            AddVerticalSectionLayout(statsPanel, spacing: 8f);
            AddFixedHeight(statsPanel, 112f);

            var topRow = CreateRect("StatsRowTop", statsPanel);
            BuildStatsRow(topRow, "CardAttackText", "ATK", "CardDefenseText", "DEF", out cardAttackText, out cardDefenseText);

            var bottomRow = CreateRect("StatsRowBottom", statsPanel);
            BuildStatsRow(bottomRow, "CardSpeedText", "SPD", "CardHealthText", "HP", out cardSpeedText, out cardHealthText);
        }

        private static void BuildStatsRow(
            RectTransform rowRect,
            string leftObjectName,
            string leftLabel,
            string rightObjectName,
            string rightLabel,
            out TMP_Text leftValue,
            out TMP_Text rightValue)
        {
            AddFixedHeight(rowRect, 48f);

            var rowLayout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            leftValue = CreateStatCell(rowRect, leftObjectName, leftLabel, "0");
            rightValue = CreateStatCell(rowRect, rightObjectName, rightLabel, "0");
        }

        private static TMP_Text CreateStatCell(RectTransform parent, string objectName, string label, string defaultValue)
        {
            var cellRect = CreateRect(objectName, parent);
            var cellLayoutElement = cellRect.gameObject.AddComponent<LayoutElement>();
            cellLayoutElement.flexibleWidth = 1f;
            cellLayoutElement.minHeight = 48f;
            cellLayoutElement.preferredHeight = 48f;

            var cellLayout = cellRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            cellLayout.spacing = 8f;
            cellLayout.padding = new RectOffset(12, 12, 0, 0);
            cellLayout.childAlignment = TextAnchor.MiddleCenter;
            cellLayout.childControlWidth = true;
            cellLayout.childControlHeight = true;
            cellLayout.childForceExpandWidth = false;
            cellLayout.childForceExpandHeight = false;

            var cellBackground = cellRect.gameObject.AddComponent<Image>();
            cellBackground.color = new Color(0.14f, 0.16f, 0.22f, 1f);
            cellBackground.raycastTarget = false;

            CreateRowText(cellRect, "Label", label, 24f, FontStyles.Bold, SectionHeaderColor, TextAlignmentOptions.MidlineLeft);
            return CreateRowText(cellRect, "Value", defaultValue, 28f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineRight);
        }

        private void BuildEquipmentSection(RectTransform scrollContentRect)
        {
            CreateSectionHeader(scrollContentRect, "EquipmentHeader", "Equipment");

            var equipmentPanel = CreateRect("EquipmentPanel", scrollContentRect);
            AddVerticalSectionLayout(equipmentPanel, spacing: 6f);

            _headEquipmentText = CreateEquipmentRow(equipmentPanel, "HeadRow", "Head");
            _bodyEquipmentText = CreateEquipmentRow(equipmentPanel, "BodyRow", "Body");
            _weaponEquipmentText = CreateEquipmentRow(equipmentPanel, "WeaponRow", "Weapon");
            _mountEquipmentText = CreateEquipmentRow(equipmentPanel, "MountRow", "Mount");
            cardPowerSummaryText = _headEquipmentText;
        }

        private TMP_Text CreateEquipmentRow(RectTransform parent, string rowName, string slotLabel)
        {
            var rowRect = CreateRect(rowName, parent);
            AddFixedHeight(rowRect, 40f);

            var rowLayout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.padding = new RectOffset(4, 4, 0, 0);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            CreateRowText(rowRect, "SlotLabel", slotLabel, 22f, FontStyles.Bold, SlotLabelColor, TextAlignmentOptions.MidlineLeft)
                .gameObject.AddComponent<LayoutElement>().preferredWidth = 88f;

            var valueText = CreateRowText(rowRect, "Value", "None", 22f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
            valueText.richText = true;
            valueText.enableWordWrapping = false;

            var valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;

            return valueText;
        }

        private void BuildSkillsSection(RectTransform scrollContentRect)
        {
            CreateSectionHeader(scrollContentRect, "SkillsHeader", "Skills");

            cardSkillsSummaryText = CreateCenteredText(
                scrollContentRect,
                "SkillsSummaryText",
                "No active skills",
                24f,
                FontStyles.Normal,
                Color.white);
            cardSkillsSummaryText.alignment = TextAlignmentOptions.TopLeft;
            cardSkillsSummaryText.enableWordWrapping = true;
            AddMinHeight(cardSkillsSummaryText.rectTransform, 48f);
        }

        private void BuildButtonRow(RectTransform cardRect)
        {
            var buttonRow = CreateRect("ButtonRow", cardRect);
            AddFixedHeight(buttonRow, ButtonRowHeight);

            var horizontal = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 16f;
            horizontal.padding = new RectOffset(0, 0, 8, 0);
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;

            backEditButton = CreateActionButton(buttonRow, "BackEditButton", "Back / Edit", BackButtonColor);
            startBattleButton = CreateActionButton(buttonRow, "StartBattleButton", "Start Battle", StartButtonColor);
        }

        private static Button CreateActionButton(RectTransform parent, string buttonName, string label, Color color)
        {
            var buttonRect = CreateRect(buttonName, parent);
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.highlightedColor = color * 1.12f;
            colors.pressedColor = color * 0.78f;
            button.colors = colors;

            var labelRect = CreateRect("Label", buttonRect);
            StretchFull(labelRect);

            var labelText = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 28f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;

            return button;
        }

        private void UpdateEquipmentSummary(PlayerFighter fighter)
        {
            SetEquipmentRow(_headEquipmentText, fighter.head);
            SetEquipmentRow(_bodyEquipmentText, fighter.body);
            SetEquipmentRow(_weaponEquipmentText, fighter.weapon);
            SetEquipmentRow(_mountEquipmentText, fighter.mount);
        }

        private static void SetEquipmentRow(TMP_Text valueText, EquipmentItemSO item)
        {
            if (valueText == null)
            {
                return;
            }

            valueText.richText = true;

            if (item == null)
            {
                valueText.text = "None";
                return;
            }

            var rarityHex = ColorUtility.ToHtmlStringRGB(item.GetRarityColor());
            valueText.text =
                $"{item.itemName}  <color=#{rarityHex}>{item.GetRarityDisplayName()}</color>";
        }

        private void UpdateSkillsSummary(PlayerFighter fighter)
        {
            if (cardSkillsSummaryText == null || fighter == null)
            {
                return;
            }

            var builder = new StringBuilder();
            AppendActiveSkill(builder, fighter.head);
            AppendActiveSkill(builder, fighter.body);
            AppendActiveSkill(builder, fighter.weapon);
            AppendActiveSkill(builder, fighter.mount);

            cardSkillsSummaryText.text = builder.Length == 0 ? "No active skills" : builder.ToString().TrimEnd();
        }

        private static void AppendActiveSkill(StringBuilder builder, EquipmentItemSO item)
        {
            if (item?.skill == null || item.skill.skillType == SkillType.None)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(item.GetSkillDisplayName());
        }

        private void UpdateRarityBadge(PlayerFighter fighter)
        {
            if (cardRarityBadgeText == null)
            {
                return;
            }

            var bestItem = GetHighestRarityItem(fighter);
            if (bestItem == null)
            {
                cardRarityBadgeText.text = string.Empty;
                cardRarityBadgeText.gameObject.SetActive(false);
                return;
            }

            cardRarityBadgeText.gameObject.SetActive(true);
            var rarityHex = ColorUtility.ToHtmlStringRGB(bestItem.GetRarityColor());
            cardRarityBadgeText.text =
                $"<color=#{rarityHex}>{bestItem.GetRarityDisplayName()} {bestItem.equipmentType}</color>";
        }

        private static EquipmentItemSO GetHighestRarityItem(PlayerFighter fighter)
        {
            EquipmentItemSO best = null;
            foreach (var item in new EquipmentItemSO[] { fighter.head, fighter.body, fighter.weapon, fighter.mount })
            {
                if (item == null)
                {
                    continue;
                }

                if (best == null || item.rarity > best.rarity)
                {
                    best = item;
                }
            }

            return best;
        }

        private static void UpdatePreviewImage(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.preserveAspect = true;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void ClearLegacyContent()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void ApplyPanelLayout()
        {
            if (_panelRect == null)
            {
                _panelRect = transform as RectTransform;
            }

            if (_panelRect == null)
            {
                return;
            }

            StretchFull(_panelRect);
            ApplySafeAreaInsets();
        }

        private void ApplySafeAreaInsets()
        {
            if (_safeAreaRect == null)
            {
                _safeAreaRect = transform.Find("SafeArea") as RectTransform;
            }

            if (_safeAreaRect == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                StretchFull(_safeAreaRect);
                return;
            }

            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                StretchFull(_safeAreaRect);
                return;
            }

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= canvas.pixelRect.width;
            anchorMin.y /= canvas.pixelRect.height;
            anchorMax.x /= canvas.pixelRect.width;
            anchorMax.y /= canvas.pixelRect.height;

            _safeAreaRect.anchorMin = anchorMin;
            _safeAreaRect.anchorMax = anchorMax;
            _safeAreaRect.offsetMin = new Vector2(SafeAreaInset, SafeAreaInset);
            _safeAreaRect.offsetMax = new Vector2(-SafeAreaInset, -SafeAreaInset);
        }

        private static void CreateSectionHeader(RectTransform parent, string objectName, string label)
        {
            var header = CreateCenteredText(parent, objectName, label, 22f, FontStyles.Bold, SectionHeaderColor);
            header.alignment = TextAlignmentOptions.MidlineLeft;
            AddFixedHeight(header.rectTransform, 28f);
        }

        private static TMP_Text CreateCenteredText(
            RectTransform parent,
            string objectName,
            string defaultText,
            float fontSize,
            FontStyles fontStyle,
            Color color)
        {
            var lineObject = new GameObject(objectName, typeof(RectTransform));
            var rect = lineObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            var text = lineObject.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_Text CreateRowText(
            RectTransform parent,
            string objectName,
            string defaultText,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment)
        {
            var lineObject = new GameObject(objectName, typeof(RectTransform));
            var rect = lineObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            var text = lineObject.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void AddVerticalSectionLayout(RectTransform section, float spacing)
        {
            var layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void AddFixedHeight(RectTransform rect, float height)
        {
            var layoutElement = rect.gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
        }

        private static void AddMinHeight(RectTransform rect, float height)
        {
            var layoutElement = rect.gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = height;
        }

        private static void SetText(TMP_Text textField, string value)
        {
            if (textField != null)
            {
                textField.text = value;
            }
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateRect(string objectName, RectTransform parent)
        {
            var rectObject = new GameObject(objectName, typeof(RectTransform));
            var rect = rectObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
