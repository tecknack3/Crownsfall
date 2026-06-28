using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crownsfall.Characters;
using UnityEditor;
using UnityEngine;

namespace Crownsfall.Editor
{
    public static class EquipmentImporter
    {
        private struct CategoryResult
        {
            public int Created;
            public int Updated;
            public int Errors;
        }

        private struct ApplyStatsResult
        {
            public int Applied;
            public int Skipped;
        }

        [MenuItem("Tools/Fighter Tools/Import All Equipment")]
        public static void ImportAllEquipment()
        {
            int totalErrors = 0;

            var bodies = ProcessCategory<BodySO>(
                "Bodies",
                "Assets/Art/Bodies",
                "Assets/ScriptableObjects/Bodies",
                EquipmentType.Body,
                defaultAttack: 2,
                defaultDefense: 12,
                defaultSpeed: 0,
                ref totalErrors);

            var heads = ProcessCategory<HeadSO>(
                "Heads",
                "Assets/Art/Heads",
                "Assets/ScriptableObjects/Heads",
                EquipmentType.Head,
                defaultAttack: 1,
                defaultDefense: 3,
                defaultSpeed: 1,
                ref totalErrors);

            var weapons = ProcessCategory<WeaponSO>(
                "Weapons",
                "Assets/Art/Weapons",
                "Assets/ScriptableObjects/Weapons",
                EquipmentType.Weapon,
                defaultAttack: 10,
                defaultDefense: 0,
                defaultSpeed: 2,
                ref totalErrors);

            var mounts = ProcessCategory<MountSO>(
                "Mounts",
                "Assets/Art/Mounts",
                "Assets/ScriptableObjects/Mounts",
                EquipmentType.Mount,
                defaultAttack: 0,
                defaultDefense: 2,
                defaultSpeed: 8,
                ref totalErrors);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"Bodies Created : {bodies.Created}\n" +
                $"Bodies Updated : {bodies.Updated}\n" +
                $"Heads Created : {heads.Created}\n" +
                $"Heads Updated : {heads.Updated}\n" +
                $"Weapons Created : {weapons.Created}\n" +
                $"Weapons Updated : {weapons.Updated}\n" +
                $"Mounts Created : {mounts.Created}\n" +
                $"Mounts Updated : {mounts.Updated}\n" +
                $"Mounts Errors : {mounts.Errors}\n" +
                $"Total Errors : {totalErrors}");
        }

        [MenuItem("Tools/Fighter Tools/Apply Default Equipment Stats")]
        public static void ApplyDefaultEquipmentStats()
        {
            var bodies = ApplyDefaultStatsInFolder<BodySO>(
                "Bodies",
                "Assets/ScriptableObjects/Bodies",
                defaultAttack: 2,
                defaultDefense: 12,
                defaultSpeed: 0);

            var heads = ApplyDefaultStatsInFolder<HeadSO>(
                "Heads",
                "Assets/ScriptableObjects/Heads",
                defaultAttack: 1,
                defaultDefense: 3,
                defaultSpeed: 1);

            var weapons = ApplyDefaultStatsInFolder<WeaponSO>(
                "Weapons",
                "Assets/ScriptableObjects/Weapons",
                defaultAttack: 10,
                defaultDefense: 0,
                defaultSpeed: 2);

            var mounts = ApplyDefaultStatsInFolder<MountSO>(
                "Mounts",
                "Assets/ScriptableObjects/Mounts",
                defaultAttack: 0,
                defaultDefense: 2,
                defaultSpeed: 8);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Equipment Default Stats]\n" +
                $"Bodies Applied : {bodies.Applied}\n" +
                $"Bodies Skipped (already had stats) : {bodies.Skipped}\n" +
                $"Heads Applied : {heads.Applied}\n" +
                $"Heads Skipped (already had stats) : {heads.Skipped}\n" +
                $"Weapons Applied : {weapons.Applied}\n" +
                $"Weapons Skipped (already had stats) : {weapons.Skipped}\n" +
                $"Mounts Applied : {mounts.Applied}\n" +
                $"Mounts Skipped (already had stats) : {mounts.Skipped}");
        }

        /// <summary>
        /// Ensures every EquipmentItemSO has a skill object defaulting to None.
        /// Run once after adding the skill field to existing assets.
        /// </summary>
        [MenuItem("Tools/Fighter Tools/Initialize Equipment Skills")]
        public static void InitializeEquipmentSkills()
        {
            var initialized = 0;
            var alreadySet = 0;

            var guids = AssetDatabase.FindAssets("t:EquipmentItemSO");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EquipmentItemSO>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                if (asset.skill == null)
                {
                    asset.skill = new EquipmentSkill { skillType = SkillType.None };
                    EditorUtility.SetDirty(asset);
                    initialized++;
                }
                else
                {
                    alreadySet++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Initialize Equipment Skills]\n" +
                $"Initialized (was null): {initialized}\n" +
                $"Already had skill data: {alreadySet}");
        }

        private static ApplyStatsResult ApplyDefaultStatsInFolder<T>(
            string categoryName,
            string soFolder,
            int defaultAttack,
            int defaultDefense,
            int defaultSpeed) where T : EquipmentItemSO
        {
            var result = new ApplyStatsResult();

            if (!AssetDatabase.IsValidFolder(soFolder))
            {
                Debug.LogError($"[Equipment Default Stats] {categoryName}: SO folder not found: {soFolder}");
                return result;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { soFolder });

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset == null)
                    continue;

                if (asset.attack == 0 && asset.defense == 0 && asset.speed == 0)
                {
                    asset.attack = defaultAttack;
                    asset.defense = defaultDefense;
                    asset.speed = defaultSpeed;
                    EditorUtility.SetDirty(asset);
                    result.Applied++;
                }
                else
                {
                    result.Skipped++;
                }
            }

            return result;
        }

        private static string GetIdPrefix(EquipmentType equipmentType)
        {
            switch (equipmentType)
            {
                case EquipmentType.Body:
                    return "BODY_";
                case EquipmentType.Head:
                    return "HEAD_";
                case EquipmentType.Weapon:
                    return "WEAPON_";
                case EquipmentType.Mount:
                    return "MOUNT_";
                default:
                    throw new ArgumentOutOfRangeException(nameof(equipmentType), equipmentType, null);
            }
        }

        private static int FindHighestIdNumber<T>(string soFolder, string idPrefix) where T : EquipmentItemSO
        {
            var max = 0;
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { soFolder });

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset == null || string.IsNullOrEmpty(asset.id))
                    continue;

                if (!asset.id.StartsWith(idPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var suffix = asset.id.Substring(idPrefix.Length);
                if (int.TryParse(suffix, out var number) && number > max)
                    max = number;
            }

            return max;
        }

        private static string SnakeCaseToTitleCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return snakeCase;

            var parts = snakeCase.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length == 0)
                    continue;

                parts[i] = char.ToUpperInvariant(part[0]) + part.Substring(1);
            }

            return string.Join(" ", parts);
        }

        private static Sprite LoadSprite(string pngPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite != null)
                return sprite;

            return AssetDatabase.LoadAllAssetsAtPath(pngPath)
                .OfType<Sprite>()
                .FirstOrDefault();
        }

        private static CategoryResult ProcessCategory<T>(
            string categoryName,
            string artFolder,
            string soFolder,
            EquipmentType equipmentType,
            int defaultAttack,
            int defaultDefense,
            int defaultSpeed,
            ref int totalErrors) where T : EquipmentItemSO
        {
            var result = new CategoryResult();

            if (!AssetDatabase.IsValidFolder(artFolder))
            {
                Debug.LogError($"[Equipment Importer] {categoryName}: Art folder not found: {artFolder}");
                result.Errors++;
                totalErrors++;
                return result;
            }

            if (!AssetDatabase.IsValidFolder(soFolder))
            {
                Debug.LogError($"[Equipment Importer] {categoryName}: SO folder not found: {soFolder}");
                result.Errors++;
                totalErrors++;
                return result;
            }

            var idPrefix = GetIdPrefix(equipmentType);
            var nextIdNumber = FindHighestIdNumber<T>(soFolder, idPrefix);

            var pngPaths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("", new[] { artFolder }))
            {
                var pngPath = AssetDatabase.GUIDToAssetPath(guid);
                if (pngPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    pngPaths.Add(pngPath);
            }

            pngPaths.Sort((a, b) =>
                string.Compare(
                    Path.GetFileNameWithoutExtension(a),
                    Path.GetFileNameWithoutExtension(b),
                    StringComparison.OrdinalIgnoreCase));

            foreach (var pngPath in pngPaths)
            {
                var fileName = Path.GetFileNameWithoutExtension(pngPath);
                if (string.IsNullOrEmpty(fileName))
                {
                    Debug.LogError($"[Equipment Importer] {categoryName}: Invalid filename for PNG: {pngPath}");
                    result.Errors++;
                    totalErrors++;
                    continue;
                }

                var assetPath = $"{soFolder}/{fileName}.asset";

                var sprite = LoadSprite(pngPath);
                if (sprite == null)
                {
                    Debug.LogError($"[Equipment Importer] {categoryName}: Could not load sprite from {pngPath}");
                    result.Errors++;
                    totalErrors++;
                    continue;
                }

                var existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                var wrongTypeAsset = existingAsset == null
                    ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath)
                    : null;

                if (wrongTypeAsset != null)
                {
                    Debug.LogError(
                        $"[Equipment Importer] {categoryName}: Asset at {assetPath} exists but is not a {typeof(T).Name}.");
                    result.Errors++;
                    totalErrors++;
                    continue;
                }

                var itemSo = existingAsset;
                var isNew = itemSo == null;

                if (isNew)
                {
                    try
                    {
                        itemSo = ScriptableObject.CreateInstance<T>();
                        AssetDatabase.CreateAsset(itemSo, assetPath);
                        itemSo.attack = defaultAttack;
                        itemSo.defense = defaultDefense;
                        itemSo.speed = defaultSpeed;
                        itemSo.rarity = EquipmentRarity.Common;
                        itemSo.skill = new EquipmentSkill { skillType = SkillType.None };
                        result.Created++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[Equipment Importer] {categoryName}: Error creating asset at {assetPath}: {ex.Message}");
                        result.Errors++;
                        totalErrors++;
                        continue;
                    }
                }
                else
                {
                    result.Updated++;
                }

                if (string.IsNullOrEmpty(itemSo.id))
                {
                    nextIdNumber++;
                    itemSo.id = $"{idPrefix}{nextIdNumber:D3}";
                }

                itemSo.itemName = SnakeCaseToTitleCase(fileName);
                itemSo.equipmentType = equipmentType;
                itemSo.icon = sprite;
                EditorUtility.SetDirty(itemSo);
            }

            return result;
        }
    }
}
