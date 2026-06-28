using System.Collections;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace Crownsfall.Combat.UI

{

    /// <summary>

    /// Which animation and styling path FloatingCombatText uses after spawn.

    /// </summary>

    public enum FloatingTextMode

    {

        Damage,

        Heal,

        Crit,

        SkillPopup

    }



    /// <summary>

    /// One floating damage/heal/crit/skill label that rises and fades out, then destroys itself.

    /// Spawned as a child of FloatingTextLayer by FloatingCombatTextSpawner.

    /// </summary>

    public class FloatingCombatText : MonoBehaviour

    {

        private const float DefaultScale = 1.2f;

        private const float DefaultFontSize = 64f;

        private const float CritFontSize = 80f;

        private const float SkillPopupFontSize = 70f;

        private const float MinFloatDuration = 1.2f;

        private const float SkillPopupDuration = 0.8f;

        private const float SkillPopupScaleStart = 0.7f;

        private const float SkillPopupScalePeak = 1.2f;

        private const float SkillPopupScaleSettle = 1.0f;

        private const float SkillPopupScaleUpDuration = 0.15f;

        private const float SkillPopupScaleSettleDuration = 0.1f;



        [Header("References")]

        [SerializeField] private TextMeshProUGUI label;



        [Header("Animation")]

        [Tooltip("How long the text floats upward before it is destroyed.")]

        [SerializeField] private float floatDuration = MinFloatDuration;



        [Tooltip("How many UI pixels the text moves upward over floatDuration.")]

        [SerializeField] private float floatDistance = 60f;



        [Tooltip("Optional horizontal drift so stacked numbers do not sit on the exact same spot.")]

        [SerializeField] private float horizontalDrift = 8f;



        private RectTransform _rectTransform;

        private Color _startColor;



        private void Awake()

        {

            if (label == null)

            {

                label = GetComponentInChildren<TextMeshProUGUI>();

            }



            _rectTransform = transform as RectTransform;

        }



        /// <summary>

        /// Sets the visible text and tint, then starts the float-and-fade animation.

        /// </summary>

        public void Initialize(string text, Color color)

        {

            if (label == null)

            {

                Debug.LogWarning("[FloatingCombatText] Missing TextMeshProUGUI reference.");

                Destroy(gameObject);

                return;

            }



            if (_rectTransform == null)

            {

                _rectTransform = transform as RectTransform;

            }



            if (_rectTransform != null && _rectTransform.localScale == Vector3.one)

            {

                _rectTransform.localScale = Vector3.one * DefaultScale;

            }



            label.text = text;

            label.fontSize = DefaultFontSize;

            label.fontStyle = FontStyles.Bold;

            EnsureShadow(label);



            var visibleColor = color;

            visibleColor.a = 1f;

            label.color = visibleColor;

            _startColor = visibleColor;



            if (_rectTransform != null && horizontalDrift > 0f)

            {

                var startPos = _rectTransform.anchoredPosition;

                startPos.x += Random.Range(-horizontalDrift, horizontalDrift);

                _rectTransform.anchoredPosition = startPos;

            }



            StopAllCoroutines();

            StartCoroutine(AnimateAndDestroy());

        }



        /// <summary>

        /// Critical hit popup: "CRIT!" on its own line above the damage number, larger yellow bold text.

        /// </summary>

        public void InitializeCrit(string damageLine, Color color)

        {

            if (label == null)

            {

                Debug.LogWarning("[FloatingCombatText] Missing TextMeshProUGUI reference.");

                Destroy(gameObject);

                return;

            }



            if (_rectTransform == null)

            {

                _rectTransform = transform as RectTransform;

            }



            if (_rectTransform != null && _rectTransform.localScale == Vector3.one)

            {

                _rectTransform.localScale = Vector3.one * DefaultScale;

            }



            label.text = $"CRIT!\n{damageLine}";

            label.fontSize = CritFontSize;

            label.fontStyle = FontStyles.Bold;

            EnsureShadow(label);



            var visibleColor = color;

            visibleColor.a = 1f;

            label.color = visibleColor;

            _startColor = visibleColor;



            if (_rectTransform != null && horizontalDrift > 0f)

            {

                var startPos = _rectTransform.anchoredPosition;

                startPos.x += Random.Range(-horizontalDrift, horizontalDrift);

                _rectTransform.anchoredPosition = startPos;

            }



            StopAllCoroutines();

            StartCoroutine(AnimateAndDestroy());

        }



        /// <summary>

        /// Large bold skill activation popup (CRITICAL!, BURN!, SHIELD!, etc.) with a punchy scale-in.

        /// </summary>

        public void InitializeSkillPopup(string text)

        {

            if (label == null)

            {

                Debug.LogWarning("[FloatingCombatText] Missing TextMeshProUGUI reference.");

                Destroy(gameObject);

                return;

            }



            if (_rectTransform == null)

            {

                _rectTransform = transform as RectTransform;

            }



            label.text = text;

            label.fontSize = SkillPopupFontSize;

            label.fontStyle = FontStyles.Bold;

            EnsureOutline(label);



            _startColor = Color.white;

            label.color = _startColor;



            if (_rectTransform != null)

            {

                _rectTransform.localScale = Vector3.one * SkillPopupScaleStart;

            }



            StopAllCoroutines();

            StartCoroutine(AnimateSkillPopup());

        }



        /// <summary>

        /// Moves the label upward while fading alpha to zero, then removes this instance.

        /// </summary>

        private IEnumerator AnimateAndDestroy()

        {

            if (_rectTransform == null || label == null)

            {

                Destroy(gameObject);

                yield break;

            }



            var startPos = _rectTransform.anchoredPosition;

            var endPos = startPos + new Vector2(0f, floatDistance);

            var duration = Mathf.Max(MinFloatDuration, floatDuration);

            var elapsed = 0f;



            while (elapsed < duration)

            {

                elapsed += Time.deltaTime;

                var t = Mathf.Clamp01(elapsed / duration);



                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);



                var fadedColor = _startColor;

                fadedColor.a = Mathf.Lerp(_startColor.a, 0f, t);

                label.color = fadedColor;



                yield return null;

            }



            Destroy(gameObject);

        }



        /// <summary>

        /// Skill popup: quick scale punch (0.7 → 1.2 → 1.0), then float up and fade over 0.8s total.

        /// </summary>

        private IEnumerator AnimateSkillPopup()

        {

            if (_rectTransform == null || label == null)

            {

                Destroy(gameObject);

                yield break;

            }



            var startPos = _rectTransform.anchoredPosition;

            var endPos = startPos + new Vector2(0f, floatDistance);

            var scaleSettleEnd = SkillPopupScaleUpDuration + SkillPopupScaleSettleDuration;

            var floatPhaseDuration = SkillPopupDuration - scaleSettleEnd;

            var elapsed = 0f;



            while (elapsed < SkillPopupDuration)

            {

                elapsed += Time.deltaTime;



                if (elapsed <= SkillPopupScaleUpDuration)

                {

                    var t = elapsed / SkillPopupScaleUpDuration;

                    _rectTransform.localScale = Vector3.one * Mathf.Lerp(SkillPopupScaleStart, SkillPopupScalePeak, t);

                    label.color = _startColor;

                }

                else if (elapsed <= scaleSettleEnd)

                {

                    var t = (elapsed - SkillPopupScaleUpDuration) / SkillPopupScaleSettleDuration;

                    _rectTransform.localScale = Vector3.one * Mathf.Lerp(SkillPopupScalePeak, SkillPopupScaleSettle, t);

                    label.color = _startColor;

                }

                else

                {

                    var t = Mathf.Clamp01((elapsed - scaleSettleEnd) / floatPhaseDuration);

                    _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);



                    var fadedColor = _startColor;

                    fadedColor.a = Mathf.Lerp(1f, 0f, t);

                    label.color = fadedColor;

                }



                yield return null;

            }



            Destroy(gameObject);

        }



        private static void EnsureShadow(TextMeshProUGUI textLabel)

        {

            if (textLabel.GetComponent<Shadow>() == null)

            {

                var shadow = textLabel.gameObject.AddComponent<Shadow>();

                shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);

                shadow.effectDistance = new Vector2(2f, -2f);

            }

        }



        /// <summary>

        /// Black TMP outline so large white skill text stays readable on busy backgrounds.

        /// </summary>

        private static void EnsureOutline(TextMeshProUGUI textLabel)

        {

            textLabel.outlineWidth = 0.2f;

            textLabel.outlineColor = Color.black;

        }

    }

}


