using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Shows one fighter in the battle scene using four stacked SpriteRenderer layers.
    /// Each layer is a child GameObject (MountLayer, BodyLayer, WeaponLayer, HeadLayer).
    /// BattleManager places this view in world space; UI lives separately on a Canvas.
    /// </summary>
    public class BattleFighterView : MonoBehaviour
    {
        // Draw order: lower sorting order = drawn first (behind). Mount back, Head front.
        private const int MountSortingOrder = 0;
        private const int BodySortingOrder = 1;
        private const int WeaponSortingOrder = 2;
        private const int HeadSortingOrder = 3;

        [Header("Sprite Layers (child GameObjects)")]
        [Tooltip("Back layer — mount sprite sits behind the body.")]
        [SerializeField] private Transform mountLayer;

        [Tooltip("Main body layer.")]
        [SerializeField] private Transform bodyLayer;

        [Tooltip("Weapon held in front of the body.")]
        [SerializeField] private Transform weaponLayer;

        [Tooltip("Head draws on top of everything else.")]
        [SerializeField] private Transform headLayer;

        [Header("Layer Offsets (local space)")]
        [Tooltip("How far the mount sits below the body center.")]
        public Vector3 mountOffset = new Vector3(0f, -0.8f, 0f);

        [Tooltip("Body stays at the fighter root center.")]
        public Vector3 bodyOffset = new Vector3(0f, 0f, 0f);

        [Tooltip("Head sits above the body.")]
        public Vector3 headOffset = new Vector3(0f, 0.75f, 0f);

        [Tooltip("Weapon offset to the side of the body.")]
        public Vector3 weaponOffset = new Vector3(0.45f, 0.1f, 0f);

        [Header("Scale")]
        [Tooltip("Extra multiplier on top of BattleManager fighterScale. Leave at 1 unless you need a tweak.")]
        public float overallScale = 1f;

        // Cached SpriteRenderers — filled in Awake from the layer transforms.
        private SpriteRenderer _mountRenderer;
        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _weaponRenderer;
        private SpriteRenderer _headRenderer;

        // True = facing right (player). False = facing left (enemy).
        private bool _faceRight = true;

        /// <summary>
        /// Runs once when the view loads. Finds renderers, sets draw order, and applies offsets.
        /// </summary>
        private void Awake()
        {
            CacheRenderers();
            ApplySortingOrders();
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

            SetLayer(_mountRenderer, fighter.mount?.icon, MountSortingOrder);
            SetLayer(_bodyRenderer, fighter.body?.icon, BodySortingOrder);
            SetLayer(_weaponRenderer, fighter.weapon?.icon, WeaponSortingOrder);
            SetLayer(_headRenderer, fighter.head?.icon, HeadSortingOrder);
        }

        /// <summary>
        /// Flips the fighter left or right by negating localScale.x.
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
        /// Tints every visible layer (e.g. reddish for the enemy dummy).
        /// Pass Color.white to restore original sprite colors.
        /// </summary>
        public void SetColorTint(Color tint)
        {
            ApplyTint(_mountRenderer, tint);
            ApplyTint(_bodyRenderer, tint);
            ApplyTint(_weaponRenderer, tint);
            ApplyTint(_headRenderer, tint);
        }

        /// <summary>
        /// Multiplies the scale BattleManager assigns by overallScale, keeping facing direction.
        /// </summary>
        public void ApplyBaseScale(Vector3 baseScale)
        {
            var scaled = baseScale * overallScale;
            scaled.x = Mathf.Abs(scaled.x) * (_faceRight ? 1f : -1f);
            transform.localScale = scaled;
        }

        /// <summary>
        /// Moves each layer child to its configured offset so parts stack correctly.
        /// </summary>
        public void ApplyLayerOffsets()
        {
            SetLayerLocalPosition(mountLayer, mountOffset);
            SetLayerLocalPosition(bodyLayer, bodyOffset);
            SetLayerLocalPosition(weaponLayer, weaponOffset);
            SetLayerLocalPosition(headLayer, headOffset);
        }

        /// <summary>
        /// Clears all sprites and hides every layer.
        /// </summary>
        public void ClearAllLayers()
        {
            SetLayer(_mountRenderer, null, MountSortingOrder);
            SetLayer(_bodyRenderer, null, BodySortingOrder);
            SetLayer(_weaponRenderer, null, WeaponSortingOrder);
            SetLayer(_headRenderer, null, HeadSortingOrder);
        }

        /// <summary>
        /// Grabs SpriteRenderer from each layer GameObject (created by the setup tool).
        /// </summary>
        private void CacheRenderers()
        {
            _mountRenderer = GetRendererOnLayer(mountLayer);
            _bodyRenderer = GetRendererOnLayer(bodyLayer);
            _weaponRenderer = GetRendererOnLayer(weaponLayer);
            _headRenderer = GetRendererOnLayer(headLayer);
        }

        /// <summary>
        /// Sets sorting orders once so layers always draw Mount → Body → Weapon → Head.
        /// </summary>
        private void ApplySortingOrders()
        {
            if (_mountRenderer != null)
            {
                _mountRenderer.sortingOrder = MountSortingOrder;
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.sortingOrder = BodySortingOrder;
            }

            if (_weaponRenderer != null)
            {
                _weaponRenderer.sortingOrder = WeaponSortingOrder;
            }

            if (_headRenderer != null)
            {
                _headRenderer.sortingOrder = HeadSortingOrder;
            }
        }

        private static SpriteRenderer GetRendererOnLayer(Transform layer)
        {
            return layer != null ? layer.GetComponent<SpriteRenderer>() : null;
        }

        private static void SetLayerLocalPosition(Transform layer, Vector3 offset)
        {
            if (layer == null)
            {
                return;
            }

            layer.localPosition = offset;
        }

        /// <summary>
        /// Assigns a sprite to one layer. Hides the renderer when sprite is null.
        /// </summary>
        private static void SetLayer(SpriteRenderer renderer, Sprite sprite, int sortingOrder)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sprite = sprite;
            renderer.enabled = sprite != null;
            renderer.sortingOrder = sortingOrder;
        }

        private static void ApplyTint(SpriteRenderer renderer, Color tint)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.color = tint;
        }
    }
}
