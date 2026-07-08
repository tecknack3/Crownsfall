using Crownsfall.Combat;
using Crownsfall.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.UI
{
    /// <summary>
    /// Compact player profile card for the Character Builder screen (above Best Run).
    /// Builds its layout at runtime when no panel is wired in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerProfileUI : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.14f, 0.98f);
        private static readonly Color BorderColor = new Color(0.22f, 0.32f, 0.28f, 0.55f);
        private static readonly Color TitleColor = new Color(0.72f, 0.88f, 0.78f, 1f);
        private static readonly Color LabelColor = new Color(0.58f, 0.62f, 0.7f, 1f);
        private static readonly Color ValueColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        private static readonly Color XpColor = new Color(0.45f, 0.9f, 0.5f, 1f);

        private const float PanelPreferredHeight = 168f;

        private TMP_Text _headerText;
        private TMP_Text _levelValueText;
        private TMP_Text _xpValueText;
        private TMP_Text _goldValueText;
        private TMP_Text _waveValueText;
        private bool _uiBuilt;

        /// <summary>
        /// Ensures the profile card exists under the given parent, inserted below the title.
        /// </summary>
        public static PlayerProfileUI EnsureOnCanvas(Transform layoutRoot, Transform insertAfter = null)
        {
            if (layoutRoot == null)
            {
                return null;
            }

            var existing = layoutRoot.Find("PlayerProfilePanel");
            PlayerProfileUI profileUi;

            if (existing != null)
            {
                profileUi = existing.GetComponent<PlayerProfileUI>();
                if (profileUi == null)
                {
                    profileUi = existing.gameObject.AddComponent<PlayerProfileUI>();
                }
            }
            else
            {
                var panelObject = new GameObject("PlayerProfilePanel", typeof(RectTransform));
                panelObject.transform.SetParent(layoutRoot, false);

                var insertIndex = insertAfter != null ? insertAfter.GetSiblingIndex() + 1 : 1;
                panelObject.transform.SetSiblingIndex(insertIndex);

                profileUi = panelObject.AddComponent<PlayerProfileUI>();
            }

            profileUi.EnsureBuilt();
            return profileUi;
        }

        /// <summary>
        /// Builds or repairs the card hierarchy. Safe to call multiple times.
        /// </summary>
        public void EnsureBuilt()
        {
            if (_uiBuilt && _levelValueText != null)
            {
                return;
            }

            _uiBuilt = true;

            var panelRect = transform as RectTransform;
            if (panelRect == null)
            {
                panelRect = gameObject.AddComponent<RectTransform>();
            }

            var layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = PanelPreferredHeight;
            layoutElement.flexibleWidth = 1f;

            var panelImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            panelImage.color = BorderColor;
            panelImage.raycastTarget = false;

            var innerContent = EnsureInnerContent();
            BuildContent(innerContent);
        }

        /// <summary>
        /// Fills the card from the persistent player profile.
        /// </summary>
        public void Display(PlayerProfile profile)
        {
            EnsureBuilt();

            if (profile == null)
            {
                profile = PlayerProfile.CreateDefault();
            }

            if (_headerText != null)
            {
                _headerText.text = string.IsNullOrEmpty(profile.PlayerName)
                    ? "PLAYER PROFILE"
                    : profile.PlayerName.ToUpperInvariant();
            }

            if (_levelValueText != null)
            {
                _levelValueText.text = profile.Level.ToString();
            }

            if (_xpValueText != null)
            {
                _xpValueText.text = $"{profile.CurrentXp} / {profile.XpRequiredForNextLevel} XP";
            }

            if (_goldValueText != null)
            {
                _goldValueText.text = profile.TotalGold.ToString();
            }

            if (_waveValueText != null)
            {
                _waveValueText.text = profile.HighestWave.ToString();
            }

            if (CombatDebug.TracePresentation || CombatDebug.LocalProgressDebug)
            {
                Debug.Log(
                    $"PlayerProfileUI.Display: level={profile.Level}, " +
                    $"xp={profile.CurrentXp}/{profile.XpRequiredForNextLevel}, gold={profile.TotalGold}.");
            }
        }

        private RectTransform EnsureInnerContent()
        {
            var existing = transform.Find("InnerContent") as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var innerObject = new GameObject("InnerContent", typeof(RectTransform));
            innerObject.transform.SetParent(transform, false);

            var innerImage = innerObject.AddComponent<Image>();
            innerImage.color = PanelColor;
            innerImage.raycastTarget = false;

            var innerLayout = innerObject.AddComponent<VerticalLayoutGroup>();
            innerLayout.padding = new RectOffset(20, 20, 14, 14);
            innerLayout.spacing = 10f;
            innerLayout.childAlignment = TextAnchor.UpperCenter;
            innerLayout.childControlWidth = true;
            innerLayout.childControlHeight = true;
            innerLayout.childForceExpandWidth = true;
            innerLayout.childForceExpandHeight = false;

            var innerElement = innerObject.AddComponent<LayoutElement>();
            innerElement.flexibleWidth = 1f;

            return innerObject.GetComponent<RectTransform>();
        }

        private void BuildContent(RectTransform innerContent)
        {
            var contentRoot = innerContent.Find("Content");
            if (contentRoot != null)
            {
                CacheReferences(contentRoot);
                return;
            }

            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(innerContent, false);

            var contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            _headerText = CreateText(
                contentObject.transform,
                "HeaderText",
                "PLAYER PROFILE",
                22f,
                FontStyles.Bold,
                TitleColor,
                TextAlignmentOptions.Center,
                28f);

            var topRow = CreateStatRow(contentObject.transform, "TopRow");
            _levelValueText = CreateStatCell(topRow, "LevelCell", "Level");
            _xpValueText = CreateStatCell(topRow, "XpCell", "XP", XpColor);

            var bottomRow = CreateStatRow(contentObject.transform, "BottomRow");
            _goldValueText = CreateStatCell(bottomRow, "GoldCell", "Total Gold");
            _waveValueText = CreateStatCell(bottomRow, "WaveCell", "Highest Wave");
        }

        private void CacheReferences(Transform contentRoot)
        {
            _headerText = contentRoot.Find("HeaderText")?.GetComponent<TMP_Text>();
            _levelValueText = contentRoot.Find("TopRow/LevelCell/ValueText")?.GetComponent<TMP_Text>();
            _xpValueText = contentRoot.Find("TopRow/XpCell/ValueText")?.GetComponent<TMP_Text>();
            _goldValueText = contentRoot.Find("BottomRow/GoldCell/ValueText")?.GetComponent<TMP_Text>();
            _waveValueText = contentRoot.Find("BottomRow/WaveCell/ValueText")?.GetComponent<TMP_Text>();
        }

        private static Transform CreateStatRow(Transform parent, string rowName)
        {
            var row = new GameObject(rowName, typeof(RectTransform));
            row.transform.SetParent(parent, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            var rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 52f;
            rowElement.flexibleWidth = 1f;
            return row.transform;
        }

        private static TMP_Text CreateStatCell(
            Transform parent,
            string cellName,
            string label,
            Color? valueColor = null)
        {
            var cell = new GameObject(cellName, typeof(RectTransform));
            cell.transform.SetParent(parent, false);

            var cellLayoutElement = cell.AddComponent<LayoutElement>();
            cellLayoutElement.flexibleWidth = 1f;
            cellLayoutElement.preferredHeight = 52f;

            var cellLayout = cell.AddComponent<VerticalLayoutGroup>();
            cellLayout.spacing = 2f;
            cellLayout.childAlignment = TextAnchor.MiddleCenter;
            cellLayout.childControlWidth = true;
            cellLayout.childControlHeight = true;
            cellLayout.childForceExpandWidth = true;
            cellLayout.childForceExpandHeight = false;

            CreateText(
                cell.transform,
                "LabelText",
                label,
                16f,
                FontStyles.Normal,
                LabelColor,
                TextAlignmentOptions.Center,
                20f);

            return CreateText(
                cell.transform,
                "ValueText",
                "—",
                24f,
                FontStyles.Bold,
                valueColor ?? ValueColor,
                TextAlignmentOptions.Center,
                30f);
        }

        private static TMP_Text CreateText(
            Transform parent,
            string objectName,
            string text,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment,
            float preferredHeight)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                var existingText = existing.GetComponent<TMP_Text>();
                if (existingText != null)
                {
                    existingText.fontSize = fontSize;
                    existingText.fontStyle = fontStyle;
                    existingText.color = color;
                    existingText.alignment = alignment;
                    existingText.text = text;
                    return existingText;
                }
            }

            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var layoutElement = textObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 1f;

            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }
    }
}
