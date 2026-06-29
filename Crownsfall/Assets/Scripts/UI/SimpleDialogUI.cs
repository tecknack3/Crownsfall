using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.UI
{
    /// <summary>
    /// Lightweight modal alert with a single dismiss button. Follows the same overlay
    /// pattern as RewardScreenUI and BattleSummaryUI (backdrop + centered card).
    /// </summary>
    public class SimpleDialogUI : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.04f, 0.05f, 0.1f, 0.88f);
        private static readonly Color CardColor = new Color(0.08f, 0.09f, 0.15f, 0.98f);
        private static readonly Color TitleColor = new Color(0.92f, 0.88f, 0.72f, 1f);
        private static readonly Color MessageColor = new Color(0.88f, 0.88f, 0.93f, 1f);
        private static readonly Color ButtonColor = new Color(0.18f, 0.62f, 0.36f, 1f);

        private const float DialogCanvasSortingOrder = 1100f;
        private const float TouchButtonMinHeight = 56f;

        private CanvasGroup _canvasGroup;
        private TMP_Text _titleText;
        private TMP_Text _messageText;
        private Button _confirmButton;
        private Action _onDismiss;
        private bool _uiBuilt;

        /// <summary>
        /// Shows a modal dialog under the given canvas transform.
        /// </summary>
        public static void Show(
            Transform canvasTransform,
            string title,
            string message,
            string buttonLabel,
            Action onDismiss)
        {
            if (canvasTransform == null)
            {
                Debug.LogWarning("[SimpleDialogUI] Cannot show dialog — canvas transform is null.");
                onDismiss?.Invoke();
                return;
            }

            Transform dialogTransform = canvasTransform.Find("SimpleDialogUI");
            SimpleDialogUI dialog;

            if (dialogTransform == null)
            {
                var dialogObject = new GameObject("SimpleDialogUI", typeof(RectTransform));
                dialogObject.transform.SetParent(canvasTransform, false);
                dialog = dialogObject.AddComponent<SimpleDialogUI>();
            }
            else
            {
                dialog = dialogTransform.GetComponent<SimpleDialogUI>();
                if (dialog == null)
                {
                    dialog = dialogTransform.gameObject.AddComponent<SimpleDialogUI>();
                }
            }

            dialog.Present(title, message, buttonLabel, onDismiss);
        }

        private void Present(string title, string message, string buttonLabel, Action onDismiss)
        {
            EnsureUiBuilt();
            _onDismiss = onDismiss;

            if (_titleText != null)
            {
                _titleText.text = title ?? string.Empty;
            }

            if (_messageText != null)
            {
                _messageText.text = message ?? string.Empty;
            }

            if (_confirmButton != null)
            {
                var buttonLabelText = _confirmButton.GetComponentInChildren<TMP_Text>();
                if (buttonLabelText != null)
                {
                    buttonLabelText.text = buttonLabel ?? "OK";
                }
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        private void EnsureUiBuilt()
        {
            if (_uiBuilt)
            {
                return;
            }

            _uiBuilt = true;

            var panelRect = transform as RectTransform;
            StretchFull(panelRect);

            var backdrop = gameObject.AddComponent<Image>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var overlayCanvas = gameObject.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = (int)DialogCanvasSortingOrder;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var cardRect = CreateRect("DialogCard", panelRect);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(720f, 420f);

            var cardImage = cardRect.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            cardImage.raycastTarget = true;

            var cardLayout = cardRect.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(32, 32, 32, 32);
            cardLayout.spacing = 20f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            _titleText = CreateText(cardRect, "TitleText", string.Empty, 40f, FontStyles.Bold, TitleColor);
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            _messageText = CreateText(cardRect, "MessageText", string.Empty, 28f, FontStyles.Normal, MessageColor);
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.enableWordWrapping = true;
            var messageLayout = _messageText.gameObject.AddComponent<LayoutElement>();
            messageLayout.flexibleHeight = 1f;
            messageLayout.minHeight = 120f;

            _confirmButton = CreateButton(cardRect, "ConfirmButton", "OK");
            _confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void OnConfirmClicked()
        {
            HideImmediate();

            var callback = _onDismiss;
            _onDismiss = null;
            callback?.Invoke();
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

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static void StretchFull(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static TMP_Text CreateText(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            FontStyles fontStyle,
            Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateButton(RectTransform parent, string name, string label)
        {
            var buttonRect = CreateRect(name, parent);
            var layoutElement = buttonRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = TouchButtonMinHeight;
            layoutElement.minHeight = TouchButtonMinHeight;

            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = ButtonColor;

            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var labelText = CreateText(buttonRect, "Label", label, 30f, FontStyles.Bold, Color.white);
            labelText.alignment = TextAlignmentOptions.Center;
            StretchFull(labelText.rectTransform);

            return button;
        }
    }
}
