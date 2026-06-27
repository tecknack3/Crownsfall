using Crownsfall.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Shows one fighter on the battle screen using stacked UI Image layers.
    /// Each layer is a RectTransform child (MountImage, BodyImage, WeaponImage, HeadImage, CrownImage).
    /// BattleManager calls DisplayFighter to put equipment icons on the rig.
    /// </summary>
    public class FighterRig : MonoBehaviour
    {
        [Header("Image Layers (UI children)")]
        [Tooltip("Back layer — mount sprite sits behind the body.")]
        [SerializeField] private Image mountImage;

        [Tooltip("Main body layer.")]
        [SerializeField] private Image bodyImage;

        [Tooltip("Weapon held in front of the body.")]
        [SerializeField] private Image weaponImage;

        [Tooltip("Head draws on top of the body.")]
        [SerializeField] private Image headImage;

        [Tooltip("Optional crown above the head. Hidden when no sprite is assigned.")]
        [SerializeField] private Image crownImage;

        [Header("Layer Offsets (anchoredPosition in UI pixels)")]
        [Tooltip("How far the mount sits below the body center.")]
        public Vector2 mountOffset = new Vector2(0f, -80f);

        [Tooltip("Body stays at the rig center.")]
        public Vector2 bodyOffset = new Vector2(0f, 0f);

        [Tooltip("Weapon offset to the side of the body.")]
        public Vector2 weaponOffset = new Vector2(45f, 10f);

        [Tooltip("Head sits above the body.")]
        public Vector2 headOffset = new Vector2(0f, 75f);

        [Tooltip("Crown sits above the head.")]
        public Vector2 crownOffset = new Vector2(0f, 100f);

        // True = facing right (player). False = facing left (enemy).
        private bool _faceRight = true;

        /// <summary>
        /// Runs once when the rig loads. Sets preserveAspect on each Image and applies offsets.
        /// </summary>
        private void Awake()
        {
            ConfigureImage(mountImage);
            ConfigureImage(bodyImage);
            ConfigureImage(weaponImage);
            ConfigureImage(headImage);
            ConfigureImage(crownImage);
            ApplyLayerOffsets();
        }

        /// <summary>
        /// Puts equipment icon sprites on each layer. Missing gear hides that layer cleanly.
        /// </summary>
        public void DisplayFighter(PlayerFighter fighter)
        {
            ApplyLayerOffsets();

            if (fighter == null)
            {
                ClearAllLayers();
                return;
            }

            SetLayer(mountImage, fighter.mount?.icon);
            SetLayer(bodyImage, fighter.body?.icon);
            SetLayer(weaponImage, fighter.weapon?.icon);
            SetLayer(headImage, fighter.head?.icon);

            // PlayerFighter has no crown slot yet — hide crown unless we add one later.
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
        /// Tints every visible layer (e.g. reddish for the enemy Training Dummy).
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
        /// Moves each layer child to its configured offset so parts stack correctly.
        /// </summary>
        public void ApplyLayerOffsets()
        {
            SetLayerAnchoredPosition(mountImage, mountOffset);
            SetLayerAnchoredPosition(bodyImage, bodyOffset);
            SetLayerAnchoredPosition(weaponImage, weaponOffset);
            SetLayerAnchoredPosition(headImage, headOffset);
            SetLayerAnchoredPosition(crownImage, crownOffset);
        }

        /// <summary>
        /// Clears all sprites and hides every layer.
        /// </summary>
        public void ClearAllLayers()
        {
            SetLayer(mountImage, null);
            SetLayer(bodyImage, null);
            SetLayer(weaponImage, null);
            SetLayer(headImage, null);
            SetLayer(crownImage, null);
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

        private static void SetLayerAnchoredPosition(Image image, Vector2 offset)
        {
            if (image == null)
            {
                return;
            }

            var rect = image.rectTransform;
            rect.anchoredPosition = offset;
        }

        private static void ApplyTint(Image image, Color tint)
        {
            if (image == null)
            {
                return;
            }

            image.color = tint;
        }
    }
}
