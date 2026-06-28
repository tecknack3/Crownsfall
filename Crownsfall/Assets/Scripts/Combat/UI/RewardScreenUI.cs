using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.Combat.UI
{
    /// <summary>
    /// Post-wave reward overlay shown after each enemy defeat. BattleManager yields WaitForContinue
    /// before advancing to the next wave or victory flow. Gold/XP are display-only in v1.
    /// </summary>
    public class RewardScreenUI : MonoBehaviour
    {
        private static readonly Color DefaultBackdropColor = new Color(0.04f, 0.05f, 0.1f, 0.88f);

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text waveClearedText;
        [SerializeField] private TMP_Text goldRewardText;
        [SerializeField] private TMP_Text xpRewardText;
        [SerializeField] private Button continueButton;

        private RectTransform _panelRect;
        private Image _backdropImage;
        private bool _continueClicked;

        private void Awake()
        {
            EnsureReferences();
            WireContinueButton();
            HideImmediate();
        }

        /// <summary>
        /// Shows the reward panel with wave-cleared and gold/XP amounts (display only).
        /// </summary>
        public void ShowReward(int waveNumber, int goldAmount, int xpAmount)
        {
            EnsureReferences();

            SetText(titleText, "VICTORY");
            SetText(waveClearedText, $"Wave {waveNumber} Cleared");
            SetText(goldRewardText, $"+{goldAmount} Gold");
            SetText(xpRewardText, $"+{xpAmount} XP");

            _continueClicked = false;

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            ApplyPanelLayout();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }

            Debug.Log("Reward Screen Shown");
        }

        /// <summary>
        /// Hides the reward panel immediately.
        /// </summary>
        public void Hide()
        {
            HideImmediate();
        }

        /// <summary>
        /// Yields until the player taps Continue, then resets the click flag.
        /// </summary>
        public IEnumerator WaitForContinue()
        {
            while (!_continueClicked)
            {
                yield return null;
            }

            _continueClicked = false;
        }

        /// <summary>
        /// Wired to the Continue button from Awake and editor setup.
        /// </summary>
        public void OnContinueClicked()
        {
            Debug.Log("Reward Continue Clicked");
            _continueClicked = true;
        }

        /// <summary>
        /// Instantly hides the panel (alpha 0, disabled). Safe at battle start.
        /// </summary>
        public void HideImmediate()
        {
            _continueClicked = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Centers the full-screen overlay. Safe to call from editor setup and at show time.
        /// </summary>
        public void ApplyPanelLayout()
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
                var contentTransform = transform.Find("RewardContent");
                if (contentTransform != null)
                {
                    contentRect = contentTransform as RectTransform;
                }
            }

            if (titleText == null)
            {
                titleText = transform.Find("RewardContent/TitleText")?.GetComponent<TMP_Text>();
            }

            if (waveClearedText == null)
            {
                waveClearedText = transform.Find("RewardContent/WaveClearedText")?.GetComponent<TMP_Text>();
            }

            if (goldRewardText == null)
            {
                goldRewardText = transform.Find("RewardContent/GoldRewardText")?.GetComponent<TMP_Text>();
            }

            if (xpRewardText == null)
            {
                xpRewardText = transform.Find("RewardContent/XpRewardText")?.GetComponent<TMP_Text>();
            }

            if (continueButton == null)
            {
                continueButton = transform.Find("RewardContent/ContinueButton")?.GetComponent<Button>();
            }

            if (_backdropImage != null && _backdropImage.color.a <= 0f)
            {
                _backdropImage.color = DefaultBackdropColor;
            }
        }

        private static void SetText(TMP_Text textField, string value)
        {
            if (textField != null)
            {
                textField.text = value;
            }
        }
    }
}
