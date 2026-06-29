using Crownsfall.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Crownsfall.Editor
{
    /// <summary>
    /// Builds the anchor → Image hierarchy expected by <see cref="FighterRig"/> on a selected GameObject.
    /// Reuses existing direct children with matching names; never deletes anything.
    /// </summary>
    public static class FighterRigHierarchyBuilder
    {
        // Default layer sizes (match BattleSceneSetup production layout).
        private const float MountBodyImageSize = 350f;
        private const float WeaponImageSize = 250f;
        private const float HeadImageSize = 275f;
        private const float CrownImageSize = 200f;

        [MenuItem("Tools/Fighter Tools/Build Fighter Rig Hierarchy")]
        public static void BuildFromMenu()
        {
            BuildOnSelection();
        }

        /// <summary>
        /// Entry point: validates selection, ensures FighterRig + child hierarchy, wires references.
        /// </summary>
        public static void BuildOnSelection()
        {
            var selected = Selection.activeGameObject;
            if (!TryGetRigRoot(selected, out var rigRect, out var errorMessage))
            {
                EditorUtility.DisplayDialog("Build Fighter Rig Hierarchy", errorMessage, "OK");
                return;
            }

            Undo.SetCurrentGroupName("Build Fighter Rig Hierarchy");
            var undoGroup = Undo.GetCurrentGroup();

            var rig = selected.GetComponent<FighterRig>();
            if (rig == null)
            {
                rig = Undo.AddComponent<FighterRig>(selected);
            }

            // Equipment anchors — each can hold one Image child for a sprite layer.
            var mountAnchor = GetOrCreateAnchor(rigRect, "MountAnchor");
            var bodyAnchor = GetOrCreateAnchor(rigRect, "BodyAnchor");
            var weaponAnchor = GetOrCreateAnchor(rigRect, "WeaponAnchor");
            var legAnchor = GetOrCreateAnchor(rigRect, "LegAnchor");
            var headAnchor = GetOrCreateAnchor(rigRect, "HeadAnchor");
            var crownAnchor = GetOrCreateAnchor(rigRect, "CrownAnchor");

            // Future HUD attachment points — empty RectTransforms only.
            var damageAnchor = GetOrCreateAnchor(rigRect, "DamageAnchor");
            var healthBarAnchor = GetOrCreateAnchor(rigRect, "HealthBarAnchor");
            var nameAnchor = GetOrCreateAnchor(rigRect, "NameAnchor");

            var mountImage = GetOrCreateEquipmentImage(mountAnchor, "MountImage", MountBodyImageSize);
            var bodyImage = GetOrCreateEquipmentImage(bodyAnchor, "BodyImage", MountBodyImageSize);
            var weaponImage = GetOrCreateEquipmentImage(weaponAnchor, "WeaponImage", WeaponImageSize);
            var headImage = GetOrCreateEquipmentImage(headAnchor, "HeadImage", HeadImageSize);
            var crownImage = GetOrCreateEquipmentImage(crownAnchor, "CrownImage", CrownImageSize, hiddenByDefault: true);

            WireFighterRig(
                rig,
                mountAnchor,
                bodyAnchor,
                weaponAnchor,
                legAnchor,
                headAnchor,
                crownAnchor,
                mountImage,
                bodyImage,
                weaponImage,
                headImage,
                crownImage,
                damageAnchor,
                healthBarAnchor,
                nameAnchor);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                $"FighterRig hierarchy built on \"{selected.name}\". " +
                "Assign sprites on each Image, or call Display() at runtime.",
                selected);
        }

        /// <summary>
        /// Validates selection: must be an in-scene GameObject with a RectTransform (UI under Canvas).
        /// </summary>
        private static bool TryGetRigRoot(GameObject selected, out RectTransform rigRect, out string errorMessage)
        {
            rigRect = null;
            errorMessage = null;

            if (selected == null)
            {
                errorMessage =
                    "Select a GameObject in the Hierarchy first.\n\n" +
                    "That object will receive (or already has) a FighterRig component, " +
                    "plus MountAnchor, BodyAnchor, WeaponAnchor, and the other child anchors.";
                return false;
            }

            if (!selected.scene.IsValid())
            {
                errorMessage =
                    "The selection is not part of an open scene.\n\n" +
                    "Open a scene, select the FighterRig root under your Canvas, then run this tool again.";
                return false;
            }

            rigRect = selected.GetComponent<RectTransform>();
            if (rigRect == null)
            {
                errorMessage =
                    "The selected GameObject needs a RectTransform.\n\n" +
                    "Create it under a Canvas (UI → Panel or empty UI object), select that root, then run this tool again.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds a direct child by name, or creates a centered empty anchor RectTransform.
        /// FighterRig.ApplyOffsets() moves anchoredPosition at runtime — start at zero here.
        /// </summary>
        private static RectTransform GetOrCreateAnchor(RectTransform parent, string anchorName)
        {
            var existing = FindDirectChild(parent.transform, anchorName);
            if (existing != null)
            {
                if (existing is RectTransform existingRect)
                {
                    return existingRect;
                }

                Debug.LogWarning(
                    $"FighterRigHierarchyBuilder: \"{anchorName}\" exists on \"{parent.name}\" but is not a RectTransform. " +
                    "Rename or remove it, then run Build Fighter Rig Hierarchy again.",
                    existing);
                return null;
            }

            var anchorObject = new GameObject(anchorName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(anchorObject, "Create Fighter Rig Anchor");
            anchorObject.transform.SetParent(parent, false);

            var anchor = anchorObject.GetComponent<RectTransform>();
            anchor.anchorMin = new Vector2(0.5f, 0.5f);
            anchor.anchorMax = new Vector2(0.5f, 0.5f);
            anchor.pivot = new Vector2(0.5f, 0.5f);
            anchor.anchoredPosition = Vector2.zero;
            anchor.localPosition = Vector3.zero;
            anchor.sizeDelta = Vector2.zero;

            return anchor;
        }

        /// <summary>
        /// Finds or creates a UI Image under an equipment anchor.
        /// </summary>
        private static Image GetOrCreateEquipmentImage(
            RectTransform anchor,
            string imageName,
            float size,
            bool hiddenByDefault = false)
        {
            if (anchor == null)
            {
                return null;
            }

            var existing = FindDirectChild(anchor.transform, imageName);
            Image image;

            if (existing != null)
            {
                image = existing.GetComponent<Image>();
                if (image == null)
                {
                    image = Undo.AddComponent<Image>(existing.gameObject);
                }

                var existingRect = existing as RectTransform;
                if (existingRect != null && existingRect.sizeDelta == Vector2.zero)
                {
                    Undo.RecordObject(existingRect, "Configure Fighter Rig Image");
                    ConfigureImageRect(existingRect, size);
                }
            }
            else
            {
                var imageObject = new GameObject(imageName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(imageObject, "Create Fighter Rig Image");
                imageObject.transform.SetParent(anchor, false);

                var imageRect = imageObject.GetComponent<RectTransform>();
                ConfigureImageRect(imageRect, size);

                image = Undo.AddComponent<Image>(imageObject);
                image.color = Color.white;
            }

            Undo.RecordObject(image, "Configure Fighter Rig Image");
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (hiddenByDefault)
            {
                image.enabled = false;
            }

            return image;
        }

        /// <summary>
        /// Center-anchored square size for one equipment sprite layer.
        /// </summary>
        private static void ConfigureImageRect(RectTransform imageRect, float size)
        {
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.localPosition = Vector3.zero;
            imageRect.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// Assigns anchor and Image references on FighterRig via SerializedObject
        /// (same field names as in FighterRig.cs).
        /// </summary>
        private static void WireFighterRig(
            FighterRig rig,
            RectTransform mountAnchor,
            RectTransform bodyAnchor,
            RectTransform weaponAnchor,
            RectTransform legAnchor,
            RectTransform headAnchor,
            RectTransform crownAnchor,
            Image mountImage,
            Image bodyImage,
            Image weaponImage,
            Image headImage,
            Image crownImage,
            RectTransform damageAnchor,
            RectTransform healthBarAnchor,
            RectTransform nameAnchor)
        {
            var serialized = new SerializedObject(rig);
            serialized.FindProperty("mountAnchor").objectReferenceValue = mountAnchor;
            serialized.FindProperty("bodyAnchor").objectReferenceValue = bodyAnchor;
            serialized.FindProperty("weaponAnchor").objectReferenceValue = weaponAnchor;
            serialized.FindProperty("legAnchor").objectReferenceValue = legAnchor;
            serialized.FindProperty("headAnchor").objectReferenceValue = headAnchor;
            serialized.FindProperty("crownAnchor").objectReferenceValue = crownAnchor;
            serialized.FindProperty("mountImage").objectReferenceValue = mountImage;
            serialized.FindProperty("bodyImage").objectReferenceValue = bodyImage;
            serialized.FindProperty("weaponImage").objectReferenceValue = weaponImage;
            serialized.FindProperty("headImage").objectReferenceValue = headImage;
            serialized.FindProperty("crownImage").objectReferenceValue = crownImage;
            serialized.FindProperty("damageAnchor").objectReferenceValue = damageAnchor;
            serialized.FindProperty("healthBarAnchor").objectReferenceValue = healthBarAnchor;
            serialized.FindProperty("nameAnchor").objectReferenceValue = nameAnchor;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(rig);
        }

        /// <summary>
        /// Looks only at immediate children — does not search deeper in the hierarchy.
        /// </summary>
        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
