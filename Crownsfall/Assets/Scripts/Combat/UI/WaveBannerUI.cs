using System.Collections;

using TMPro;

using UnityEngine;

using UnityEngine.UI;

namespace Crownsfall.Combat.UI

{

    /// <summary>

    /// Centered wave intro banner shown before each wave's combat begins.

    /// BattleManager yields ShowWaveBanner so fighters stay idle until the animation finishes.

    /// </summary>

    public class WaveBannerUI : MonoBehaviour

    {

        private static readonly Vector2 PanelSize = new Vector2(520f, 180f);

        private static readonly Color BackdropColor = new Color(0.04f, 0.05f, 0.1f, 0.85f);

        private static readonly Color TitleColor = Color.white;

        private static readonly Color SubtitleColor = Color.white;

        private static readonly Color BossTitleColor = new Color(0.95f, 0.9f, 0.72f, 1f);

        private static readonly Color BossSubtitleColor = new Color(0.95f, 0.85f, 0.45f, 1f);



        private const float TitleFontSize = 52f;

        private const float SubtitleFontSize = 32f;



        [Header("References")]

        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private RectTransform contentRect;

        [SerializeField] private TMP_Text titleText;

        [SerializeField] private TMP_Text subtitleText;



        [Header("Timing")]

        [Tooltip("Seconds to fade in (alpha 0→1, scale startScale→1).")]

        [SerializeField] private float fadeInDuration = 0.25f;



        [Tooltip("Seconds to hold the banner at full visibility before fading out.")]

        [SerializeField] private float holdDuration = 0.75f;



        [Tooltip("Seconds to fade out (alpha 1→0).")]

        [SerializeField] private float fadeOutDuration = 0.25f;



        [Header("Scale")]

        [Tooltip("Starting local scale during fade in (0.85 = 85% size).")]

        [SerializeField] private float startScale = 0.85f;



        [Tooltip("Optional shrink at fade out (multiplied on content scale). 1 = no shrink.")]

        [SerializeField] private float fadeOutScaleMultiplier = 0.95f;



        private RectTransform _panelRect;

        private Image _backdropImage;

        private Vector3 _restScale = Vector3.one;



        private void Awake()

        {

            EnsureReferences();

            ApplyPanelLayout();

            HideImmediate();

        }



        /// <summary>

        /// Shows the wave banner with fade/scale animation, then hides the panel.

        /// Yield this from BattleManager so combat waits until the banner finishes.

        /// </summary>

        public IEnumerator ShowWaveBanner(int waveNumber, string enemyName, bool isBoss)

        {

            EnsureReferences();



            if (titleText == null && subtitleText == null)

            {

                Debug.LogWarning("[WaveBannerUI] Missing TMP references — skipping banner.");

                yield break;

            }



            Debug.Log($"Showing Wave Banner: Wave {waveNumber} / {enemyName}");



            transform.SetAsLastSibling();

            gameObject.SetActive(true);

            ApplyPanelLayout();

            ApplyBackdropStyle();

            ApplyBannerText(waveNumber, enemyName, isBoss);

            ApplyTextStyles(isBoss);



            if (canvasGroup != null)

            {

                canvasGroup.alpha = 0f;

                canvasGroup.blocksRaycasts = false;

                canvasGroup.interactable = false;

            }



            if (contentRect != null)

            {

                contentRect.localScale = _restScale * startScale;

            }



            // Fade in: alpha 0→1, scale startScale→1.

            yield return AnimateFadeAndScale(0f, 1f, startScale, 1f, fadeInDuration);



            // Hold at full visibility so the player can read the wave info.

            if (holdDuration > 0f)

            {

                yield return new WaitForSeconds(holdDuration);

            }



            // Fade out: alpha 1→0, optional slight shrink.

            yield return AnimateFadeAndScale(1f, 0f, 1f, fadeOutScaleMultiplier, fadeOutDuration);



            Debug.Log("Wave Banner Finished");

            HideImmediate();

        }



        /// <summary>

        /// Instantly hides the banner (alpha 0, panel disabled). Safe to call at battle start.

        /// </summary>

        public void HideImmediate()

        {

            if (canvasGroup != null)

            {

                canvasGroup.alpha = 0f;

                canvasGroup.blocksRaycasts = false;

                canvasGroup.interactable = false;

            }



            if (contentRect != null)

            {

                contentRect.localScale = _restScale;

            }



            gameObject.SetActive(false);

        }



        /// <summary>

        /// Centers the panel on screen at 520×180. Safe to call from editor setup and at show time.

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



            _panelRect.anchorMin = new Vector2(0.5f, 0.5f);

            _panelRect.anchorMax = new Vector2(0.5f, 0.5f);

            _panelRect.pivot = new Vector2(0.5f, 0.5f);

            _panelRect.anchoredPosition = Vector2.zero;

            _panelRect.sizeDelta = PanelSize;



            if (contentRect != null)

            {

                contentRect.anchorMin = Vector2.zero;

                contentRect.anchorMax = Vector2.one;

                contentRect.pivot = new Vector2(0.5f, 0.5f);

                contentRect.anchoredPosition = Vector2.zero;

                contentRect.offsetMin = Vector2.zero;

                contentRect.offsetMax = Vector2.zero;

            }

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

                var contentTransform = transform.Find("WaveBannerContent");

                if (contentTransform != null)

                {

                    contentRect = contentTransform as RectTransform;

                }

            }



            if (titleText == null)

            {

                titleText = transform.Find("WaveBannerContent/TitleText")?.GetComponent<TMP_Text>();

            }



            if (subtitleText == null)

            {

                subtitleText = transform.Find("WaveBannerContent/SubtitleText")?.GetComponent<TMP_Text>();

            }



            if (contentRect != null)

            {

                _restScale = contentRect.localScale;

            }

        }



        private void ApplyBackdropStyle()

        {

            if (_backdropImage == null)

            {

                return;

            }



            _backdropImage.color = BackdropColor;

            _backdropImage.raycastTarget = false;

        }



        /// <summary>

        /// Sets title/subtitle copy for a regular wave or a boss wave.

        /// </summary>

        private void ApplyBannerText(int waveNumber, string enemyName, bool isBoss)

        {

            var displayName = string.IsNullOrEmpty(enemyName) ? "Enemy" : enemyName;



            if (isBoss)

            {

                SetText(titleText, "BOSS WAVE");

                SetText(subtitleText, $"BOSS: {displayName}");

            }

            else

            {

                SetText(titleText, $"Wave {waveNumber}");

                SetText(subtitleText, displayName);

            }

        }



        private void ApplyTextStyles(bool isBoss)

        {

            if (titleText != null)

            {

                titleText.fontSize = TitleFontSize;

                titleText.color = isBoss ? BossTitleColor : TitleColor;

            }



            if (subtitleText != null)

            {

                subtitleText.fontSize = SubtitleFontSize;

                subtitleText.color = isBoss ? BossSubtitleColor : SubtitleColor;

            }

        }



        /// <summary>

        /// Lerps CanvasGroup alpha and content localScale over duration seconds.

        /// </summary>

        private IEnumerator AnimateFadeAndScale(

            float startAlpha,

            float endAlpha,

            float startScaleMultiplier,

            float endScaleMultiplier,

            float duration)

        {

            if (duration <= 0f)

            {

                if (canvasGroup != null)

                {

                    canvasGroup.alpha = endAlpha;

                }



                if (contentRect != null)

                {

                    contentRect.localScale = _restScale * endScaleMultiplier;

                }



                yield break;

            }



            var elapsed = 0f;



            while (elapsed < duration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);



                if (canvasGroup != null)

                {

                    canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

                }



                if (contentRect != null)

                {

                    var scale = Mathf.Lerp(startScaleMultiplier, endScaleMultiplier, t);

                    contentRect.localScale = _restScale * scale;

                }



                yield return null;

            }



            if (canvasGroup != null)

            {

                canvasGroup.alpha = endAlpha;

            }



            if (contentRect != null)

            {

                contentRect.localScale = _restScale * endScaleMultiplier;

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
