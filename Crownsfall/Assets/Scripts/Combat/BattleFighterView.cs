using Crownsfall.Characters;
using UnityEngine;

namespace Crownsfall.Combat
{
    /// <summary>
    /// Shows a fighter in the battle scene using four stacked SpriteRenderers.
    /// Layer order (back to front): Mount, Body, Weapon, Head.
    /// Wire each renderer in the Inspector or use Tools → Fighter Tools → Setup Battle Scene.
    /// </summary>
    public class BattleFighterView : MonoBehaviour
    {
        // Sorting orders keep layers in the correct draw order (lower = drawn first / behind).
        // Mount is farthest back; Head draws on top of Body; Weapon sits in front of Body.
        private const int MountSortingOrder = 0;
        private const int BodySortingOrder = 1;
        private const int WeaponSortingOrder = 2;
        private const int HeadSortingOrder = 3;

        [Header("Sprite Layers (back to front)")]
        [SerializeField] private SpriteRenderer mountRenderer;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private SpriteRenderer headRenderer;

        /// <summary>
        /// Runs once when this view is created. Sets draw order and centers child sprites.
        /// </summary>
        private void Awake()
        {
            ApplySortingOrders();
            CenterSpriteLayersAtOrigin();
        }

        /// <summary>
        /// Updates all four sprite layers from the given fighter's equipment icons.
        /// Missing equipment hides that layer instead of showing a broken sprite.
        /// </summary>
        public void DisplayFighter(PlayerFighter fighter)
        {
            if (fighter == null)
            {
                ClearAllLayers();
                return;
            }

            SetLayer(mountRenderer, fighter.mount?.icon, MountSortingOrder);
            SetLayer(bodyRenderer, fighter.body?.icon, BodySortingOrder);
            SetLayer(weaponRenderer, fighter.weapon?.icon, WeaponSortingOrder);
            SetLayer(headRenderer, fighter.head?.icon, HeadSortingOrder);
        }

        /// <summary>
        /// Tints every visible layer (e.g. red for the enemy placeholder).
        /// Pass Color.white to reset to the sprite's original colors.
        /// </summary>
        public void SetColorTint(Color tint)
        {
            ApplyTint(mountRenderer, tint);
            ApplyTint(bodyRenderer, tint);
            ApplyTint(weaponRenderer, tint);
            ApplyTint(headRenderer, tint);
        }

        /// <summary>
        /// Clears all sprites and hides every layer.
        /// </summary>
        public void ClearAllLayers()
        {
            SetLayer(mountRenderer, null, MountSortingOrder);
            SetLayer(bodyRenderer, null, BodySortingOrder);
            SetLayer(weaponRenderer, null, WeaponSortingOrder);
            SetLayer(headRenderer, null, HeadSortingOrder);
        }

        /// <summary>
        /// Sets sorting orders once so layers always draw Mount → Body → Weapon → Head.
        /// </summary>
        private void ApplySortingOrders()
        {
            if (mountRenderer != null)
            {
                mountRenderer.sortingOrder = MountSortingOrder;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.sortingOrder = BodySortingOrder;
            }

            if (weaponRenderer != null)
            {
                weaponRenderer.sortingOrder = WeaponSortingOrder;
            }

            if (headRenderer != null)
            {
                headRenderer.sortingOrder = HeadSortingOrder;
            }
        }

        /// <summary>
        /// Keeps each sprite child at local (0, 0, 0) so BattleManager scaling shrinks/grows from the center.
        /// </summary>
        private void CenterSpriteLayersAtOrigin()
        {
            CenterLayerTransform(mountRenderer);
            CenterLayerTransform(bodyRenderer);
            CenterLayerTransform(weaponRenderer);
            CenterLayerTransform(headRenderer);
        }

        /// <summary>
        /// Moves one layer's transform to the parent's origin without changing world position at edit time.
        /// </summary>
        private static void CenterLayerTransform(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.localPosition = Vector3.zero;
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

        /// <summary>
        /// Applies a color tint to a single renderer when it exists.
        /// </summary>
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
