using System.Collections;
using System.Text;
using Crownsfall.Characters;
using Crownsfall.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Crownsfall.Combat.UI
{
    /// <summary>
    /// Post-victory summary overlay (Wave 3 only). Builds its own UI at runtime when not wired in the scene.
    /// </summary>
    public class BattleSummaryUI : MonoBehaviour
    {
        private const string CharacterBuilderSceneName = "CharacterBuilder";
        private const float FadeInDuration = 0.35f;
        private const float ContinueButtonMinHeight = 56f;
        /// <summary>Above FloatingCombatTextSpawner layer (999) so summary receives touches on mobile.</summary>
        private const int SummaryCanvasSortingOrder = 1000;

        private static readonly Color DefaultBackdropColor = new Color(0.04f, 0.05f, 0.1f, 0.92f);
        private static readonly Color TitleGoldColor = new Color(0.92f, 0.88f, 0.72f, 1f);
        private static readonly Color CardColor = new Color(0.08f, 0.09f, 0.15f, 0.98f);
        private static readonly Color ContinueButtonColor = new Color(0.18f, 0.62f, 0.36f, 1f);
        private static readonly Color SectionHeaderColor = new Color(0.78f, 0.8f, 0.88f, 1f);
        private static readonly Color LabelColor = new Color(0.72f, 0.74f, 0.8f, 1f);
        private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color GoldHighlightColor = new Color(1f, 0.84f, 0.2f, 1f);
        private static readonly Color XpHighlightColor = new Color(0.45f, 0.9f, 0.5f, 1f);
        private static readonly Color AbilityLineColor = new Color(0.88f, 0.88f, 0.93f, 1f);

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text wavesText;
        [SerializeField] private TMP_Text highestWaveText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text damageDealtText;
        [SerializeField] private TMP_Text damageTakenText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text skillsText;
        [SerializeField] private Button continueButton;

        private RectTransform _panelRect;
        private RectTransform _safeAreaRect;
        private RectTransform _scrollViewportRect;
        private ScrollRect _scrollRect;
        private Image _backdropImage;
        private Canvas _overlayCanvas;
        private GraphicRaycaster _overlayRaycaster;
        private ContinueButtonClickReceiver _continueClickReceiver;
        private bool _continueClicked;
        private bool _continueHandling;
        private bool _uiBuilt;

        private void Awake()
        {
            EnsureReferences();
            WireContinueButton();
            HideImmediate();
        }

        /// <summary>
        /// Creates a runtime Battle Summary panel under the battle Canvas when none exists in the scene.
        /// </summary>
        public static BattleSummaryUI CreateRuntimePanel(RectTransform canvasRect)
        {
            if (canvasRect == null)
            {
                return null;
            }

            var panelObject = new GameObject("BattleSummaryPanel", typeof(RectTransform));
            panelObject.transform.SetParent(canvasRect, false);

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var summaryUi = panelObject.AddComponent<BattleSummaryUI>();
            summaryUi.BuildDefaultUiHierarchy();
            summaryUi.HideImmediate();
            return summaryUi;
        }

        /// <summary>
        /// Shows the summary with a simple fade-in.
        /// </summary>
        public void ShowSummary(BattleSummaryData data)
        {
            EnsureReferences();

            SetText(titleText, "VICTORY");
            SetText(playerNameText, data?.PlayerName ?? string.Empty);
            SetStatValue(wavesText, $"{data?.WavesCleared ?? 0} / {data?.TotalWaves ?? 0}");
            SetStatValue(highestWaveText, $"{data?.HighestWave ?? data?.WavesCleared ?? 0}");
            SetStatValue(scoreText, $"{data?.FinalScore ?? 0}");
            SetStatValue(goldText, $"{data?.GoldEarned ?? 0}", GoldHighlightColor);
            SetStatValue(xpText, $"{data?.XpEarned ?? 0}", XpHighlightColor);
            SetStatValue(damageDealtText, $"{data?.DamageDealt ?? 0}");
            SetStatValue(damageTakenText, $"{data?.DamageTaken ?? 0}");
            SetStatValue(durationText, FormatDuration(data?.BattleDurationSeconds ?? 0f));
            SetText(skillsText, FormatAbilitiesSection(data));

            _continueClicked = false;
            _continueHandling = false;
            gameObject.SetActive(true);
            EnsureTopOverlayCanvas();
            transform.SetAsLastSibling();
            ApplyPanelLayout();
            Canvas.ForceUpdateCanvases();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            WireContinueButton();

            if (continueButton != null)
            {
                continueButton.interactable = true;
                continueButton.transform.SetAsLastSibling();
            }

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }

            StopAllCoroutines();
            StartCoroutine(FadeInBackdrop());

            Debug.Log("[BattleSummaryUI] ShowSummary opened");

            if (EventSystem.current == null)
            {
                Debug.LogWarning("[BattleSummaryUI] No EventSystem in scene — Continue button will not receive taps.");
            }
        }

        /// <summary>
        /// Hides the summary panel immediately.
        /// </summary>
        public void Hide()
        {
            HideImmediate();
        }

        /// <summary>
        /// Yields until Continue is clicked.
        /// </summary>
        public IEnumerator WaitForContinue()
        {
            while (!_continueClicked)
            {
                yield return null;
            }

            _continueClicked = false;
        }

        public void OnContinueClicked()
        {
            HandleContinueTapped("button");
        }

        private void HandleContinueTapped(string source)
        {
            if (_continueHandling)
            {
                return;
            }

            _continueHandling = true;
            Debug.Log($"[BattleSummaryUI] Continue clicked ({source})");

            _continueClicked = true;
            SceneManager.LoadScene(CharacterBuilderSceneName);
        }

        /// <summary>
        /// Builds the default overlay hierarchy when the panel was created at runtime.
        /// </summary>
        public void BuildDefaultUiHierarchy()
        {
            if (_uiBuilt)
            {
                return;
            }

            _uiBuilt = true;
            _panelRect = transform as RectTransform;

            _backdropImage = gameObject.GetComponent<Image>();
            if (_backdropImage == null)
            {
                _backdropImage = gameObject.AddComponent<Image>();
            }

            _backdropImage.color = DefaultBackdropColor;
            _backdropImage.raycastTarget = false;

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _safeAreaRect = CreateRect("SafeArea", _panelRect);
            StretchFull(_safeAreaRect);

            var cardRect = CreateRect("SummaryCard", _safeAreaRect);
            StretchFull(cardRect);

            var cardImage = cardRect.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            cardImage.raycastTarget = false;

            var cardLayout = cardRect.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(24, 24, 24, 24);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            var scrollViewRect = CreateRect("ScrollView", cardRect);
            var scrollLayoutElement = scrollViewRect.gameObject.AddComponent<LayoutElement>();
            scrollLayoutElement.flexibleHeight = 1f;
            scrollLayoutElement.minHeight = 240f;

            _scrollViewportRect = CreateRect("Viewport", scrollViewRect);
            StretchFull(_scrollViewportRect);
            _scrollViewportRect.gameObject.AddComponent<RectMask2D>();

            contentRect = CreateRect("ScrollContent", _scrollViewportRect);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(16, 16, 8, 8);
            contentLayout.spacing = 10f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect = scrollViewRect.gameObject.AddComponent<ScrollRect>();
            _scrollRect.content = contentRect;
            _scrollRect.viewport = _scrollViewportRect;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 24f;

            titleText = CreateCenteredLine(contentRect, "TitleText", "VICTORY", 52f, FontStyles.Bold, TitleGoldColor);
            playerNameText = CreateCenteredLine(contentRect, "PlayerNameText", "Fighter", 34f, FontStyles.Bold, Color.white);
            CreateDivider(contentRect, "HeaderDivider");
            CreateSectionHeader(contentRect, "BattleResultsHeader", "Battle Results");
            wavesText = CreateStatRow(contentRect, "WavesRow", "Waves Cleared:", 26f);
            highestWaveText = CreateStatRow(contentRect, "HighestWaveRow", "Highest Wave:", 26f);
            scoreText = CreateStatRow(contentRect, "ScoreRow", "Final Score:", 26f);
            goldText = CreateStatRow(contentRect, "GoldRow", "Gold Earned:", 26f);
            xpText = CreateStatRow(contentRect, "XpRow", "XP Earned:", 26f);
            damageDealtText = CreateStatRow(contentRect, "DamageDealtRow", "Damage Dealt:", 26f);
            damageTakenText = CreateStatRow(contentRect, "DamageTakenRow", "Damage Taken:", 26f);
            durationText = CreateStatRow(contentRect, "DurationRow", "Battle Time:", 26f);
            CreateDivider(contentRect, "AbilitiesDivider");
            CreateSectionHeader(contentRect, "AbilitiesHeader", "Abilities Triggered");
            skillsText = CreateCenteredLine(contentRect, "AbilitiesText", "None triggered", 24f, FontStyles.Normal, AbilityLineColor);
            skillsText.alignment = TextAlignmentOptions.TopLeft;
            skillsText.enableWordWrapping = true;

            continueButton = CreateContinueButton(cardRect, "ContinueButton", "Continue");
            continueButton.transform.SetAsLastSibling();
            WireContinueButton();
        }

        /// <summary>
        /// Fades only the backdrop tint so CanvasGroup stays at alpha 1 for reliable mobile input.
        /// </summary>
        private IEnumerator FadeInBackdrop()
        {
            if (_backdropImage == null)
            {
                yield break;
            }

            var targetColor = _backdropImage.color;
            var startColor = targetColor;
            startColor.a = 0f;
            _backdropImage.color = startColor;

            var elapsed = 0f;
            while (elapsed < FadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / FadeInDuration);
                _backdropImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            _backdropImage.color = targetColor;
        }

        /// <summary>
        /// Puts this panel on its own overlay canvas above FloatingTextLayer (sort 999).
        /// </summary>
        private void EnsureTopOverlayCanvas()
        {
            _overlayCanvas = GetComponent<Canvas>();
            if (_overlayCanvas == null)
            {
                _overlayCanvas = gameObject.AddComponent<Canvas>();
            }

            _overlayCanvas.overrideSorting = true;
            _overlayCanvas.sortingOrder = SummaryCanvasSortingOrder;
            _overlayCanvas.enabled = true;

            _overlayRaycaster = GetComponent<GraphicRaycaster>();
            if (_overlayRaycaster == null)
            {
                _overlayRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            _overlayRaycaster.enabled = true;
        }

        private void HideImmediate()
        {
            _continueClicked = false;
            _continueHandling = false;
            StopAllCoroutines();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
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

            StretchFull(_safeAreaRect);

            var safeArea = Screen.safeArea;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
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
            _safeAreaRect.offsetMin = new Vector2(16f, 16f);
            _safeAreaRect.offsetMax = new Vector2(-16f, -16f);
        }

        private void WireContinueButton()
        {
            if (continueButton == null)
            {
                Debug.LogWarning("[BattleSummaryUI] WireContinueButton failed — continueButton is null.");
                return;
            }

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);

            _continueClickReceiver = continueButton.GetComponent<ContinueButtonClickReceiver>();
            if (_continueClickReceiver == null)
            {
                _continueClickReceiver = continueButton.gameObject.AddComponent<ContinueButtonClickReceiver>();
            }

            _continueClickReceiver.Initialize(this);
            Debug.Log("[BattleSummaryUI] WireContinueButton succeeded");
        }

        private void EnsureReferences()
        {
            if (_panelRect == null)
            {
                _panelRect = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_backdropImage == null)
            {
                _backdropImage = GetComponent<Image>();
            }

            if (_safeAreaRect == null)
            {
                _safeAreaRect = transform.Find("SafeArea") as RectTransform;
            }

            if (contentRect == null)
            {
                contentRect = transform.Find("SafeArea/SummaryCard/ScrollView/Viewport/ScrollContent") as RectTransform
                    ?? transform.Find("SummaryContent") as RectTransform;
            }

            if (_scrollRect == null)
            {
                _scrollRect = transform.Find("SafeArea/SummaryCard/ScrollView")?.GetComponent<ScrollRect>();
            }

            if (_scrollViewportRect == null && _scrollRect != null)
            {
                _scrollViewportRect = _scrollRect.viewport;
            }

            if (titleText == null)
            {
                titleText = FindText("TitleText");
            }

            if (playerNameText == null)
            {
                playerNameText = FindText("PlayerNameText");
            }

            if (wavesText == null)
            {
                wavesText = FindStatValue("WavesRow", "WavesText");
            }

            if (highestWaveText == null)
            {
                highestWaveText = FindStatValue("HighestWaveRow", "HighestWaveText");
            }

            if (scoreText == null)
            {
                scoreText = FindStatValue("ScoreRow", "ScoreText");
            }

            if (goldText == null)
            {
                goldText = FindStatValue("GoldRow", "GoldText");
            }

            if (xpText == null)
            {
                xpText = FindStatValue("XpRow", "XpRewardText")
                    ?? FindStatValue("XpRow", "XpText");
            }

            if (damageDealtText == null)
            {
                damageDealtText = FindStatValue("DamageDealtRow", "DamageDealtText");
            }

            if (damageTakenText == null)
            {
                damageTakenText = FindStatValue("DamageTakenRow", "DamageTakenText");
            }

            if (durationText == null)
            {
                durationText = FindStatValue("DurationRow", "DurationText");
            }

            if (skillsText == null)
            {
                skillsText = transform.Find("SafeArea/SummaryCard/ScrollView/Viewport/ScrollContent/AbilitiesText")?.GetComponent<TMP_Text>()
                    ?? transform.Find("SummaryContent/SkillsText")?.GetComponent<TMP_Text>();
            }

            if (continueButton == null)
            {
                continueButton = transform.Find("SafeArea/SummaryCard/ContinueButton")?.GetComponent<Button>()
                    ?? transform.Find("SummaryContent/ContinueButton")?.GetComponent<Button>();
            }

            if (!_uiBuilt && contentRect == null)
            {
                BuildDefaultUiHierarchy();
            }

            if (_backdropImage != null)
            {
                _backdropImage.raycastTarget = false;
            }

            var cardImage = transform.Find("SafeArea/SummaryCard")?.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.raycastTarget = false;
            }

            WireContinueButton();
        }

        private TMP_Text FindText(string objectName)
        {
            return transform.Find($"SafeArea/SummaryCard/ScrollView/Viewport/ScrollContent/{objectName}")?.GetComponent<TMP_Text>()
                ?? transform.Find($"SummaryContent/{objectName}")?.GetComponent<TMP_Text>();
        }

        private TMP_Text FindStatValue(string rowName, string legacyObjectName)
        {
            return transform.Find($"SafeArea/SummaryCard/ScrollView/Viewport/ScrollContent/{rowName}/Value")?.GetComponent<TMP_Text>()
                ?? transform.Find($"SummaryContent/{legacyObjectName}")?.GetComponent<TMP_Text>()
                ?? transform.Find($"SummaryContent/{rowName}")?.GetComponent<TMP_Text>();
        }

        private static string FormatAbilitiesSection(BattleSummaryData data)
        {
            if (data?.SkillActivations == null || data.SkillActivations.Count == 0)
            {
                return "None triggered";
            }

            var builder = new StringBuilder();
            AppendAbilityLine(builder, "Life Steal", data, SkillType.LifeSteal);
            AppendAbilityLine(builder, "Shield", data, SkillType.Shield);
            AppendAbilityLine(builder, "Critical Strike", data, SkillType.CriticalStrike);
            return builder.Length == 0 ? "None triggered" : builder.ToString().TrimEnd();
        }

        private static void AppendAbilityLine(StringBuilder builder, string label, BattleSummaryData data, SkillType skillType)
        {
            if (data.SkillActivations.TryGetValue(skillType, out var count) && count > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(label).Append(" \u00d7").Append(count);
            }
        }

        private static string FormatDuration(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            var minutes = totalSeconds / 60;
            var remainingSeconds = totalSeconds % 60;
            return $"{minutes}:{remainingSeconds:00}";
        }

        private static void SetText(TMP_Text textField, string value)
        {
            if (textField != null)
            {
                textField.text = value;
            }
        }

        private static void SetStatValue(TMP_Text valueText, string value, Color? valueColor = null)
        {
            if (valueText == null)
            {
                return;
            }

            valueText.text = value;
            if (valueColor.HasValue)
            {
                valueText.color = valueColor.Value;
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

        private static TMP_Text CreateCenteredLine(
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

            var layoutElement = lineObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = fontSize + 20f;
            return text;
        }

        private static void CreateSectionHeader(RectTransform parent, string objectName, string label)
        {
            var header = CreateCenteredLine(parent, objectName, label, 22f, FontStyles.Bold, SectionHeaderColor);
            header.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateDivider(RectTransform parent, string objectName)
        {
            var dividerRect = CreateRect(objectName, parent);
            var image = dividerRect.gameObject.AddComponent<Image>();
            image.color = DividerColor;
            image.raycastTarget = false;

            var layoutElement = dividerRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 2f;
            layoutElement.minHeight = 2f;
        }

        private static TMP_Text CreateStatRow(RectTransform parent, string objectName, string label, float fontSize)
        {
            var rowRect = CreateRect(objectName, parent);
            var rowLayout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(4, 4, 0, 0);
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            var rowElement = rowRect.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = fontSize + 16f;

            var labelText = CreateRowText(rowRect, "Label", label, fontSize, FontStyles.Normal, LabelColor, TextAlignmentOptions.MidlineLeft);
            var valueText = CreateRowText(rowRect, "Value", "0", fontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineRight);

            var labelElement = labelText.gameObject.AddComponent<LayoutElement>();
            labelElement.flexibleWidth = 1f;
            labelElement.preferredWidth = 0f;

            var valueElement = valueText.gameObject.AddComponent<LayoutElement>();
            valueElement.flexibleWidth = 1f;
            valueElement.preferredWidth = 0f;

            return valueText;
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

        private static Button CreateContinueButton(RectTransform parent, string objectName, string label)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = ContinueButtonColor;
            image.raycastTarget = true;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = Mathf.Max(ContinueButtonMinHeight, 64f);
            layoutElement.minHeight = ContinueButtonMinHeight;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            StretchFull(labelRect);

            var labelText = labelObject.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 30f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;

            return button;
        }

        /// <summary>
        /// Mobile fallback when ScrollRect or overlay sorting prevents Button.onClick from firing.
        /// </summary>
        private sealed class ContinueButtonClickReceiver : MonoBehaviour, IPointerClickHandler
        {
            private BattleSummaryUI _owner;

            public void Initialize(BattleSummaryUI owner)
            {
                _owner = owner;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                _owner?.HandleContinueTapped("pointerClick");
            }
        }
    }
}
