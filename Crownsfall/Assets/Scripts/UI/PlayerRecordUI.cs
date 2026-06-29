using Crownsfall.Combat;
using Crownsfall.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.UI
{
    /// <summary>
    /// Compact personal-best card for the Character Builder screen.
    /// Builds a premium 2×2 stats grid at runtime when no panel is wired in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerRecordUI : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.14f, 0.98f);
        private static readonly Color BorderColor = new Color(0.32f, 0.28f, 0.18f, 0.55f);
        private static readonly Color TitleGold = new Color(0.92f, 0.78f, 0.38f, 1f);
        private static readonly Color LabelColor = new Color(0.58f, 0.62f, 0.7f, 1f);
        private static readonly Color ValueColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        private static readonly Color FooterColor = new Color(0.72f, 0.76f, 0.82f, 1f);
        private static readonly Color EmptyTitleColor = new Color(0.78f, 0.8f, 0.86f, 1f);
        private static readonly Color EmptySubtitleColor = new Color(0.58f, 0.62f, 0.7f, 1f);

        private const float PanelPreferredHeight = 200f;

        private TMP_Text _filledHeaderText;
        private TMP_Text _emptyTitleText;
        private TMP_Text _emptySubtitleText;

        private TMP_Text _scoreValueText;
        private TMP_Text _waveValueText;
        private TMP_Text _goldValueText;
        private TMP_Text _xpValueText;
        private TMP_Text _lastFighterText;
        private GameObject _statsGridRoot;
        private GameObject _emptyStateRoot;

        private bool _uiBuilt;

        /// <summary>
        /// Ensures the record card exists under the given parent, inserted below the title.
        /// </summary>
        public static PlayerRecordUI EnsureOnCanvas(Transform layoutRoot, Transform insertAfter = null)
        {
            if (layoutRoot == null)
            {
                return null;
            }

            var existing = layoutRoot.Find("PlayerRecordPanel");
            PlayerRecordUI recordUi;

            if (existing != null)
            {
                recordUi = existing.GetComponent<PlayerRecordUI>();
                if (recordUi == null)
                {
                    recordUi = existing.gameObject.AddComponent<PlayerRecordUI>();
                }
            }
            else
            {
                var panelObject = new GameObject("PlayerRecordPanel", typeof(RectTransform));
                panelObject.transform.SetParent(layoutRoot, false);

                var insertIndex = insertAfter != null ? insertAfter.GetSiblingIndex() + 1 : 1;
                panelObject.transform.SetSiblingIndex(insertIndex);

                recordUi = panelObject.AddComponent<PlayerRecordUI>();
            }

            recordUi.EnsureBuilt();
            return recordUi;
        }

        /// <summary>
        /// Builds or repairs the card hierarchy. Safe to call multiple times.
        /// </summary>
        public void EnsureBuilt()
        {
            if (_uiBuilt && _filledHeaderText != null && _statsGridRoot != null)
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

            EnsureBorderAndBackground();

            var verticalLayout = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(4, 4, 4, 4);
            verticalLayout.spacing = 0f;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            var innerContent = EnsureInnerContent();
            BuildFilledState(innerContent);
            BuildEmptyState(innerContent);
        }

        /// <summary>
        /// Fills the card from saved run data.
        /// </summary>
        public void Display(LocalRunResultData data, bool hasRecord)
        {
            EnsureBuilt();

            if (!hasRecord || data == null)
            {
                ShowEmptyState();
            }
            else
            {
                ShowFilledState(data);
            }

            if (CombatDebug.TracePresentation || CombatDebug.LocalProgressDebug)
            {
                Debug.Log(
                    $"PlayerRecordUI.Display: hasRecord={hasRecord}, " +
                    $"bestScore={data?.BestScore ?? 0}, lastFighter={data?.LastFighterName}.");
            }
        }

        private void EnsureBorderAndBackground()
        {
            var panelImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            panelImage.color = BorderColor;
            panelImage.raycastTarget = false;
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
            innerLayout.padding = new RectOffset(20, 20, 16, 16);
            innerLayout.spacing = 12f;
            innerLayout.childAlignment = TextAnchor.UpperCenter;
            innerLayout.childControlWidth = true;
            innerLayout.childControlHeight = true;
            innerLayout.childForceExpandWidth = true;
            innerLayout.childForceExpandHeight = false;

            var innerElement = innerObject.AddComponent<LayoutElement>();
            innerElement.flexibleWidth = 1f;

            return innerObject.GetComponent<RectTransform>();
        }

        private void BuildFilledState(RectTransform innerContent)
        {
            _statsGridRoot = innerContent.Find("FilledState")?.gameObject;
            if (_statsGridRoot != null)
            {
                CacheFilledStateReferences(_statsGridRoot.transform);
                return;
            }

            _statsGridRoot = new GameObject("FilledState", typeof(RectTransform));
            _statsGridRoot.transform.SetParent(innerContent, false);

            var filledLayout = _statsGridRoot.AddComponent<VerticalLayoutGroup>();
            filledLayout.spacing = 12f;
            filledLayout.childAlignment = TextAnchor.UpperCenter;
            filledLayout.childControlWidth = true;
            filledLayout.childControlHeight = true;
            filledLayout.childForceExpandWidth = true;
            filledLayout.childForceExpandHeight = false;

            _filledHeaderText = CreateText(
                _statsGridRoot.transform,
                "HeaderText",
                "BEST RUN",
                24f,
                FontStyles.Bold,
                TitleGold,
                TextAlignmentOptions.Center,
                32f);

            var gridRoot = new GameObject("StatsGrid", typeof(RectTransform));
            gridRoot.transform.SetParent(_statsGridRoot.transform, false);

            var gridLayout = gridRoot.AddComponent<VerticalLayoutGroup>();
            gridLayout.spacing = 8f;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.childControlWidth = true;
            gridLayout.childControlHeight = true;
            gridLayout.childForceExpandWidth = true;
            gridLayout.childForceExpandHeight = false;

            var gridElement = gridRoot.AddComponent<LayoutElement>();
            gridElement.preferredHeight = 128f;
            gridElement.flexibleWidth = 1f;

            var topRow = CreateStatRow(gridRoot.transform, "TopRow");
            var bottomRow = CreateStatRow(gridRoot.transform, "BottomRow");

            _scoreValueText = CreateStatCell(topRow, "ScoreCell", "Score");
            _waveValueText = CreateStatCell(topRow, "WaveCell", "Wave");
            _goldValueText = CreateStatCell(bottomRow, "GoldCell", "Gold");
            _xpValueText = CreateStatCell(bottomRow, "XpCell", "XP");

            _lastFighterText = CreateText(
                _statsGridRoot.transform,
                "LastFighterText",
                "Last Fighter: —",
                22f,
                FontStyles.Normal,
                FooterColor,
                TextAlignmentOptions.Center,
                30f);
        }

        private void BuildEmptyState(RectTransform innerContent)
        {
            _emptyStateRoot = innerContent.Find("EmptyState")?.gameObject;
            if (_emptyStateRoot != null)
            {
                _emptyTitleText = _emptyStateRoot.transform.Find("EmptyTitle")?.GetComponent<TMP_Text>();
                _emptySubtitleText = _emptyStateRoot.transform.Find("EmptySubtitle")?.GetComponent<TMP_Text>();
                return;
            }

            _emptyStateRoot = new GameObject("EmptyState", typeof(RectTransform));
            _emptyStateRoot.transform.SetParent(innerContent, false);

            var emptyLayout = _emptyStateRoot.AddComponent<VerticalLayoutGroup>();
            emptyLayout.spacing = 8f;
            emptyLayout.childAlignment = TextAnchor.MiddleCenter;
            emptyLayout.childControlWidth = true;
            emptyLayout.childControlHeight = true;
            emptyLayout.childForceExpandWidth = true;
            emptyLayout.childForceExpandHeight = false;
            emptyLayout.padding = new RectOffset(0, 0, 24, 24);

            var emptyElement = _emptyStateRoot.AddComponent<LayoutElement>();
            emptyElement.preferredHeight = 140f;

            _emptyTitleText = CreateText(
                _emptyStateRoot.transform,
                "EmptyTitle",
                "No battles yet",
                26f,
                FontStyles.Bold,
                EmptyTitleColor,
                TextAlignmentOptions.Center,
                36f);

            _emptySubtitleText = CreateText(
                _emptyStateRoot.transform,
                "EmptySubtitle",
                "Create your fighter and start your first run.",
                22f,
                FontStyles.Normal,
                EmptySubtitleColor,
                TextAlignmentOptions.Center,
                0f);
        }

        private void CacheFilledStateReferences(Transform filledRoot)
        {
            _filledHeaderText = filledRoot.Find("HeaderText")?.GetComponent<TMP_Text>();
            _scoreValueText = filledRoot.Find("StatsGrid/TopRow/ScoreCell/ValueText")?.GetComponent<TMP_Text>();
            _waveValueText = filledRoot.Find("StatsGrid/TopRow/WaveCell/ValueText")?.GetComponent<TMP_Text>();
            _goldValueText = filledRoot.Find("StatsGrid/BottomRow/GoldCell/ValueText")?.GetComponent<TMP_Text>();
            _xpValueText = filledRoot.Find("StatsGrid/BottomRow/XpCell/ValueText")?.GetComponent<TMP_Text>();
            _lastFighterText = filledRoot.Find("LastFighterText")?.GetComponent<TMP_Text>();

            var emptyRoot = transform.Find("InnerContent/EmptyState");
            if (emptyRoot != null)
            {
                _emptyTitleText = emptyRoot.Find("EmptyTitle")?.GetComponent<TMP_Text>();
                _emptySubtitleText = emptyRoot.Find("EmptySubtitle")?.GetComponent<TMP_Text>();
            }
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
            rowElement.preferredHeight = 56f;
            rowElement.flexibleWidth = 1f;
            return row.transform;
        }

        private static TMP_Text CreateStatCell(Transform parent, string cellName, string label)
        {
            var cell = new GameObject(cellName, typeof(RectTransform));
            cell.transform.SetParent(parent, false);

            var cellLayoutElement = cell.AddComponent<LayoutElement>();
            cellLayoutElement.flexibleWidth = 1f;
            cellLayoutElement.preferredHeight = 56f;

            var cellLayout = cell.AddComponent<VerticalLayoutGroup>();
            cellLayout.spacing = 2f;
            cellLayout.childAlignment = TextAnchor.MiddleCenter;
            cellLayout.childControlWidth = true;
            cellLayout.childControlHeight = true;
            cellLayout.childForceExpandWidth = true;
            cellLayout.childForceExpandHeight = false;

            CreateText(cell.transform, "LabelText", label, 18f, FontStyles.Normal, LabelColor, TextAlignmentOptions.Center, 22f);
            return CreateText(cell.transform, "ValueText", "—", 28f, FontStyles.Bold, ValueColor, TextAlignmentOptions.Center, 34f);
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

            if (preferredHeight > 0f)
            {
                var layoutElement = textObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = preferredHeight;
                layoutElement.flexibleWidth = 1f;
            }
            else
            {
                var layoutElement = textObject.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1f;
            }

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

        private void ShowEmptyState()
        {
            if (_statsGridRoot != null)
            {
                _statsGridRoot.SetActive(false);
            }

            if (_emptyStateRoot != null)
            {
                _emptyStateRoot.SetActive(true);
            }

            if (_emptyTitleText != null)
            {
                _emptyTitleText.text = "No battles yet";
                _emptyTitleText.color = EmptyTitleColor;
            }

            if (_emptySubtitleText != null)
            {
                _emptySubtitleText.text = "Create your fighter and start your first run.";
                _emptySubtitleText.color = EmptySubtitleColor;
            }
        }

        private void ShowFilledState(LocalRunResultData data)
        {
            if (_emptyStateRoot != null)
            {
                _emptyStateRoot.SetActive(false);
            }

            if (_statsGridRoot != null)
            {
                _statsGridRoot.SetActive(true);
            }

            if (_filledHeaderText != null)
            {
                _filledHeaderText.text = "BEST RUN";
                _filledHeaderText.color = TitleGold;
            }

            if (_scoreValueText != null)
            {
                _scoreValueText.text = data.BestScore.ToString();
            }

            if (_waveValueText != null)
            {
                _waveValueText.text = data.BestWave.ToString();
            }

            if (_goldValueText != null)
            {
                _goldValueText.text = data.BestGold.ToString();
            }

            if (_xpValueText != null)
            {
                _xpValueText.text = data.BestXp.ToString();
            }

            if (_lastFighterText != null)
            {
                _lastFighterText.text = $"Last Fighter: {FormatFighterName(data.LastFighterName)}";
            }
        }

        private static string FormatFighterName(string fighterName)
        {
            return string.IsNullOrEmpty(fighterName) ? "Unknown Fighter" : fighterName;
        }
    }
}
