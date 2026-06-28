using System.Collections;
using System.Collections.Generic;
using Crownsfall.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Reusable UI rig that displays one fighter using stacked equipment sprites.
    /// Visual layers only — no battle logic, no HUD stats. Any screen (Battle, Character Builder, etc.)
    /// can call Display() with a PlayerFighter to show the same layered look.
    ///
    /// Expected hierarchy (created by Tools → Fighter Tools → Setup Battle Scene Production UI):
    ///
    /// FighterRig                          ← this component lives on the root RectTransform
    /// ├── MountAnchor                     ← offset moves the whole mount layer
    /// │   └── MountImage                  ← UnityEngine.UI.Image (equipment sprite)
    /// ├── BodyAnchor
    /// │   └── BodyImage
    /// ├── WeaponAnchor
    /// │   └── WeaponImage
    /// ├── HeadAnchor
    /// │   └── HeadImage
    /// ├── CrownAnchor                     ← optional crown above the head
    /// │   └── CrownImage
    /// ├── DamageAnchor                    ← empty; future floating damage numbers
    /// ├── HealthBarAnchor                 ← empty; future per-fighter health bar on rig
    /// └── NameAnchor                      ← empty; future name label on rig
    /// </summary>
    public class FighterRig : MonoBehaviour
    {
        [Header("Equipment Anchors (offsets applied here)")]
        [SerializeField] private RectTransform mountAnchor;
        [SerializeField] private RectTransform bodyAnchor;
        [SerializeField] private RectTransform weaponAnchor;
        [SerializeField] private RectTransform headAnchor;
        [SerializeField] private RectTransform crownAnchor;

        [Header("Equipment Images (sprites assigned here)")]
        [SerializeField] private Image mountImage;
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image weaponImage;
        [SerializeField] private Image headImage;
        [SerializeField] private Image crownImage;

        [Header("Future UI Anchors")]
        [Tooltip("Parent for floating damage/heal numbers spawned by FloatingCombatTextSpawner.")]
        [SerializeField] private RectTransform damageAnchor;

        [Tooltip("Parent for a health bar attached to this fighter — not used yet (BattleHUD owns bars today).")]
        [SerializeField] private RectTransform healthBarAnchor;

        [Tooltip("Parent for a name label above/below the fighter — not used yet.")]
        [SerializeField] private RectTransform nameAnchor;

        /// <summary>
        /// Empty anchor above the fighter — parent for floating damage/heal numbers.
        /// Created by Tools → Fighter Tools → Setup Battle Scene Production UI.
        /// </summary>
        public RectTransform DamageAnchor => damageAnchor;

        [Header("Layer Offsets (anchoredPosition on each anchor, in UI pixels)")]
        [Tooltip("How far the mount sits below the body center.")]
        public Vector2 mountOffset = new Vector2(0f, -80f);

        [Tooltip("Body stays at the rig center (usually 0, 0).")]
        public Vector2 bodyOffset = Vector2.zero;

        [Tooltip("Weapon offset to the side of the body.")]
        public Vector2 weaponOffset = new Vector2(45f, 10f);

        [Tooltip("Head sits above the body.")]
        public Vector2 headOffset = new Vector2(0f, 75f);

        [Tooltip("Crown sits above the head.")]
        public Vector2 crownOffset = new Vector2(0f, 100f);

        private bool _faceRight = true;
        private bool _crownVisible;

        /// <summary>
        /// How long sprite layers stay white during a hit flash.
        /// </summary>
        private const float FlashDuration = 0.12f;

        /// <summary>
        /// Tracks the running flash so a new hit can restart cleanly.
        /// </summary>
        private Coroutine _flashCoroutine;

        /// <summary>
        /// Rest scale captured when a scale punch starts (preserves facing flip on X).
        /// </summary>
        private Vector3 _scalePunchRestScale = Vector3.one;

        /// <summary>
        /// Images flashed during the current/restarted hit reaction.
        /// </summary>
        private Image[] _flashImages;

        /// <summary>
        /// Colors to restore after the flash (includes enemy tint, crown, etc.).
        /// </summary>
        private Color[] _flashOriginalColors;

        /// <summary>
        /// Runs once when the rig loads. Configures images and positions each anchor.
        /// </summary>
        private void Awake()
        {
            ConfigureImage(mountImage);
            ConfigureImage(bodyImage);
            ConfigureImage(weaponImage);
            ConfigureImage(headImage);
            ConfigureImage(crownImage);
            ApplyOffsets();
            HideCrown();
        }

        /// <summary>
        /// Puts equipment icon sprites on each layer for a wave enemy.
        /// Convenience wrapper — combat logic should still use EnemyFighter directly.
        /// </summary>
        public void DisplayEnemy(EnemyFighter enemy)
        {
            Display(enemy?.ToDisplayFighter());
        }

        /// <summary>
        /// Puts equipment icon sprites on each layer. Missing gear hides that layer cleanly.
        /// Pass null to clear the rig.
        /// </summary>
        public void Display(PlayerFighter fighter)
        {
            ApplyOffsets();

            if (fighter == null)
            {
                ClearAllLayers();
                return;
            }

            SetLayer(mountImage, fighter.mount?.icon);
            SetLayer(bodyImage, fighter.body?.icon);
            SetLayer(weaponImage, fighter.weapon?.icon);
            SetLayer(headImage, fighter.head?.icon);

            // PlayerFighter has no crown slot yet — keep crown hidden unless ShowCrown() is called.
            if (!_crownVisible)
            {
                SetLayer(crownImage, null);
            }
        }

        /// <summary>
        /// Shows the crown layer. Assign a crown sprite on CrownImage in the Inspector, or extend
        /// Display() later when PlayerFighter gains a crown slot.
        /// </summary>
        public void ShowCrown()
        {
            _crownVisible = true;

            if (crownImage != null && crownImage.sprite != null)
            {
                crownImage.enabled = true;
            }
        }

        /// <summary>
        /// Hides the crown layer regardless of any assigned sprite.
        /// </summary>
        public void HideCrown()
        {
            _crownVisible = false;
            SetLayer(crownImage, null);
        }

        /// <summary>
        /// Flips the fighter left or right by negating localScale.x on this rig root.
        /// Player typically faces right; enemy faces left.
        /// </summary>
        public void SetFacing(bool faceRight)
        {
            _faceRight = faceRight;

            var scale = transform.localScale;
            var magnitudeX = Mathf.Abs(scale.x);
            scale.x = magnitudeX * (faceRight ? 1f : -1f);
            transform.localScale = scale;
        }

        /// <summary>
        /// Tints every equipment layer (e.g. reddish for the enemy Training Dummy).
        /// Pass Color.white to restore original sprite colors.
        /// </summary>
        public void SetColorTint(Color tint)
        {
            ApplyTint(mountImage, tint);
            ApplyTint(bodyImage, tint);
            ApplyTint(weaponImage, tint);
            ApplyTint(headImage, tint);
            ApplyTint(crownImage, tint);
        }

        /// <summary>
        /// Brief white flash on all visible equipment Images when this fighter takes damage.
        /// Fire-and-forget — starts a coroutine internally and does not block callers.
        /// If already flashing, stops the previous flash, restores colors, then starts fresh.
        /// </summary>
        public void FlashDamage()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
                RestoreFlashColors();
            }

            _flashCoroutine = StartCoroutine(FlashDamageRoutine(FlashDuration));
        }

        /// <summary>
        /// Two quick white flashes for critical hits — caller can yield return this coroutine.
        /// </summary>
        public IEnumerator FlashDamageCritical()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
                RestoreFlashColors();
            }

            // Shorter per-flash duration so the double hit reads as one sharp burst.
            const float critFlashDuration = 0.1f;
            const float gapBetweenFlashes = 0.04f;

            yield return FlashDamageRoutine(critFlashDuration);
            yield return new WaitForSeconds(gapBetweenFlashes);
            yield return FlashDamageRoutine(critFlashDuration);
        }

        /// <summary>
        /// Brief scale pop on the rig root (e.g. enemy squashes outward on a crit).
        /// Multiplies current localScale uniformly so facing flip on X is preserved.
        /// </summary>
        public IEnumerator ScalePunch(float peakScaleMultiplier = 1.12f, float duration = 0.15f)
        {
            _scalePunchRestScale = transform.localScale;

            if (duration <= 0f || peakScaleMultiplier <= 1f)
            {
                yield break;
            }

            var peakScale = _scalePunchRestScale * peakScaleMultiplier;
            var halfDuration = duration * 0.5f;
            var elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(_scalePunchRestScale, peakScale, t);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(peakScale, _scalePunchRestScale, t);
                yield return null;
            }

            transform.localScale = _scalePunchRestScale;
        }

        /// <summary>
        /// Moves each equipment anchor to its configured offset so parts stack correctly.
        /// Call after changing offsets at runtime, or from the Inspector via a custom editor later.
        /// </summary>
        public void ApplyOffsets()
        {
            SetAnchorPosition(mountAnchor, mountOffset);
            SetAnchorPosition(bodyAnchor, bodyOffset);
            SetAnchorPosition(weaponAnchor, weaponOffset);
            SetAnchorPosition(headAnchor, headOffset);
            SetAnchorPosition(crownAnchor, crownOffset);
        }

        /// <summary>
        /// Clears all sprites and hides every equipment layer.
        /// </summary>
        public void ClearAllLayers()
        {
            SetLayer(mountImage, null);
            SetLayer(bodyImage, null);
            SetLayer(weaponImage, null);
            SetLayer(headImage, null);
            SetLayer(crownImage, null);
            _crownVisible = false;
        }

        /// <summary>
        /// Turns on preserveAspect and disables raycasts so taps pass through to buttons behind.
        /// </summary>
        private static void ConfigureImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        /// <summary>
        /// Assigns a sprite to one layer. Hides the Image when sprite is null.
        /// </summary>
        private static void SetLayer(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void SetAnchorPosition(RectTransform anchor, Vector2 offset)
        {
            if (anchor == null)
            {
                return;
            }

            anchor.anchoredPosition = offset;
        }

        private static void ApplyTint(Image image, Color tint)
        {
            if (image == null)
            {
                return;
            }

            image.color = tint;
        }

        /// <summary>
        /// Collects every Image under this rig, stores each current color, flashes white, then restores.
        /// </summary>
        private IEnumerator FlashDamageRoutine(float flashDuration)
        {
            CacheFlashImagesAndColors();

            if (_flashImages == null || _flashImages.Length == 0)
            {
                _flashCoroutine = null;
                yield break;
            }

            for (var i = 0; i < _flashImages.Length; i++)
            {
                _flashImages[i].color = Color.white;
            }

            yield return new WaitForSeconds(flashDuration);

            RestoreFlashColors();
            _flashCoroutine = null;
        }

        /// <summary>
        /// Finds all child Images with an assigned sprite so empty/hidden layers are skipped safely.
        /// </summary>
        private void CacheFlashImagesAndColors()
        {
            var allImages = GetComponentsInChildren<Image>(includeInactive: true);
            var validImages = new List<Image>();

            foreach (var image in allImages)
            {
                if (image == null || image.sprite == null)
                {
                    continue;
                }

                validImages.Add(image);
            }

            _flashImages = validImages.ToArray();
            _flashOriginalColors = new Color[_flashImages.Length];

            for (var i = 0; i < _flashImages.Length; i++)
            {
                _flashOriginalColors[i] = _flashImages[i].color;
            }
        }

        /// <summary>
        /// Puts each flashed Image back to the color it had when the flash started.
        /// </summary>
        private void RestoreFlashColors()
        {
            if (_flashImages == null || _flashOriginalColors == null)
            {
                return;
            }

            var count = Mathf.Min(_flashImages.Length, _flashOriginalColors.Length);

            for (var i = 0; i < count; i++)
            {
                if (_flashImages[i] == null)
                {
                    continue;
                }

                _flashImages[i].color = _flashOriginalColors[i];
            }
        }
    }
}
