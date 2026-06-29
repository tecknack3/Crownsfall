using System.Collections;
using System.Text;
using Crownsfall.Characters;
using Crownsfall.Combat;
using TMPro;
using UnityEngine;
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

        private static readonly Color DefaultBackdropColor = new Color(0.04f, 0.05f, 0.1f, 0.92f);
        private static readonly Color TitleGoldColor = new Color(0.92f, 0.88f, 0.72f, 1f);
        private static readonly Color CardColor = new Color(0.08f, 0.09f, 0.15f, 0.98f);
        private static readonly Color ContinueButtonColor = new Color(0.18f, 0.62f, 0.36f, 1f);

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text wavesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private TMP_Text damageDealtText;
        [SerializeField] private TMP_Text damageTakenText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text skillsText;
        [SerializeField] private Button continueButton;

        private RectTransform _panelRect;
        private Image _backdropImage;
        private bool _continueClicked;
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
            SetText(wavesText, $"Waves Cleared: {data?.WavesCleared ?? 0} / {data?.TotalWaves ?? 0}");
            SetText(scoreText, $"Final Score: {data?.FinalScore ?? 0}");
            SetText(goldText, $"Gold Earned: {data?.GoldEarned ?? 0}");
            SetText(xpText, $"XP Earned: {data?.XpEarned ?? 0}");
            SetText(damageDealtText, $"Damage Dealt: {data?.DamageDealt ?? 0}");
            SetText(damageTakenText, $"Damage Taken: {data?.DamageTaken ?? 0}");
            SetText(durationText, $"Battle Time: {FormatDuration(data?.BattleDurationSeconds ?? 0f)}");
            SetText(skillsText, FormatSkillsSection(data));

            _continueClicked = false;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            ApplyPanelLayout();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }

            StopAllCoroutines();
            StartCoroutine(FadeIn());

            if (CombatDebug.TracePresentation)
            {
                Debug.Log("Battle Summary screen shown.");
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
            if (CombatDebug.TracePresentation)
            {
                Debug.Log("Battle Summary Continue clicked — loading CharacterBuilder.");
            }

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
            _backdropImage.raycastTarget = true;

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            contentRect = CreateRect("SummaryContent", _panelRect);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(720f, 920f);

            var cardImage = contentRect.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            cardImage.raycastTarget = true;

            var layout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 40);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = CreateLine(contentRect, "TitleText", "VICTORY", 48f, FontStyles.Bold, TitleGoldColor);
            playerNameText = CreateLine(contentRect, "PlayerNameText", "Fighter", 34f, FontStyles.Bold, Color.white);
            wavesText = CreateLine(contentRect, "WavesText", "Waves Cleared: 0 / 0", 28f, FontStyles.Normal, Color.white);
            scoreText = CreateLine(contentRect, "ScoreText", "Final Score: 0", 28f, FontStyles.Normal, Color.white);
            goldText = CreateLine(contentRect, "GoldText", "Gold Earned: 0", 26f, FontStyles.Normal, new Color(1f, 0.84f, 0.2f, 1f));
            xpText = CreateLine(contentRect, "XpText", "XP Earned: 0", 26f, FontStyles.Normal, new Color(0.55f, 0.85f, 1f, 1f));
            damageDealtText = CreateLine(contentRect, "DamageDealtText", "Damage Dealt: 0", 26f, FontStyles.Normal, Color.white);
            damageTakenText = CreateLine(contentRect, "DamageTakenText", "Damage Taken: 0", 26f, FontStyles.Normal, Color.white);
            durationText = CreateLine(contentRect, "DurationText", "Battle Time: 0:00", 26f, FontStyles.Normal, Color.white);
            skillsText = CreateLine(contentRect, "SkillsText", "Skills", 24f, FontStyles.Normal, new Color(0.85f, 0.85f, 0.9f, 1f));
            continueButton = CreateContinueButton(contentRect, "ContinueButton", "Continue");
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < FadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private void HideImmediate()
        {
            _continueClicked = false;
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

            _panelRect.anchorMin = Vector2.zero;
            _panelRect.anchorMax = Vector2.one;
            _panelRect.pivot = new Vector2(0.5f, 0.5f);
            _panelRect.anchoredPosition = Vector2.zero;
            _panelRect.offsetMin = Vector2.zero;
            _panelRect.offsetMax = Vector2.zero;
        }

        private void WireContinueButton()
        {
            if (continueButton == null)
            {
                return;
            }

            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
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

            if (contentRect == null)
            {
                contentRect = transform.Find("SummaryContent") as RectTransform;
            }

            if (titleText == null)
            {
                titleText = transform.Find("SummaryContent/TitleText")?.GetComponent<TMP_Text>();
            }

            if (playerNameText == null)
            {
                playerNameText = transform.Find("SummaryContent/PlayerNameText")?.GetComponent<TMP_Text>();
            }

            if (wavesText == null)
            {
                wavesText = transform.Find("SummaryContent/WavesText")?.GetComponent<TMP_Text>();
            }

            if (scoreText == null)
            {
                scoreText = transform.Find("SummaryContent/ScoreText")?.GetComponent<TMP_Text>();
            }

            if (goldText == null)
            {
                goldText = transform.Find("SummaryContent/GoldText")?.GetComponent<TMP_Text>();
            }

            if (xpText == null)
            {
                xpText = transform.Find("SummaryContent/XpRewardText")?.GetComponent<TMP_Text>()
                    ?? transform.Find("SummaryContent/XpText")?.GetComponent<TMP_Text>();
            }

            if (damageDealtText == null)
            {
                damageDealtText = transform.Find("SummaryContent/DamageDealtText")?.GetComponent<TMP_Text>();
            }

            if (damageTakenText == null)
            {
                damageTakenText = transform.Find("SummaryContent/DamageTakenText")?.GetComponent<TMP_Text>();
            }

            if (durationText == null)
            {
                durationText = transform.Find("SummaryContent/DurationText")?.GetComponent<TMP_Text>();
            }

            if (skillsText == null)
            {
                skillsText = transform.Find("SummaryContent/SkillsText")?.GetComponent<TMP_Text>();
            }

            if (continueButton == null)
            {
                continueButton = transform.Find("SummaryContent/ContinueButton")?.GetComponent<Button>();
            }

            if (!_uiBuilt && contentRect == null)
            {
                BuildDefaultUiHierarchy();
            }
        }

        private static string FormatSkillsSection(BattleSummaryData data)
        {
            if (data?.SkillActivations == null || data.SkillActivations.Count == 0)
            {
                return "Skills: None triggered";
            }

            var builder = new StringBuilder("Skills:");
            AppendSkillLine(builder, "Critical Strike", data, SkillType.CriticalStrike);
            AppendSkillLine(builder, "Life Steal", data, SkillType.LifeSteal);
            AppendSkillLine(builder, "Shield", data, SkillType.Shield);
            return builder.ToString();
        }

        private static void AppendSkillLine(StringBuilder builder, string label, BattleSummaryData data, SkillType skillType)
        {
            if (data.SkillActivations.TryGetValue(skillType, out var count) && count > 0)
            {
                builder.Append('\n').Append("  ").Append(label).Append(": ").Append(count);
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

        private static RectTransform CreateRect(string objectName, RectTransform parent)
        {
            var rectObject = new GameObject(objectName, typeof(RectTransform));
            var rect = rectObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static TMP_Text CreateLine(
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
            layoutElement.preferredHeight = fontSize + 18f;
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
            layoutElement.preferredHeight = 88f;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelObject.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 30f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;

            return button;
        }
    }
}
