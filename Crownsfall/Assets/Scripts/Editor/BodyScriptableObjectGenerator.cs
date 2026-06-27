using System.IO;
using Crownsfall.Characters;
using UnityEditor;
using UnityEngine;

namespace Crownsfall.Editor
{
    public static class BodyScriptableObjectGenerator
    {
        private const string ArtFolder = "Assets/Art/Bodies";
        private const string OutputFolder = "Assets/ScriptableObjects/Bodies";

        [MenuItem("Tools/Fighter Tools/Generate Body ScriptableObjects")]
        public static void GenerateBodyScriptableObjects()
        {
            int generated = 0;
            int updated = 0;
            int skipped = 0;
            int errors = 0;

            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                Debug.LogError($"[Body SO Generator] Art folder not found: {ArtFolder}");
                PrintSummary(0, 0, 0, 1);
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                Debug.LogError($"[Body SO Generator] Output folder not found: {OutputFolder}");
                PrintSummary(0, 0, 0, 1);
                return;
            }

            var guids = AssetDatabase.FindAssets("", new[] { ArtFolder });

            foreach (var guid in guids)
            {
                var pngPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!pngPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(pngPath);
                if (string.IsNullOrEmpty(fileName))
                {
                    Debug.LogError($"[Body SO Generator] Invalid filename for PNG: {pngPath}");
                    errors++;
                    continue;
                }

                var assetPath = $"{OutputFolder}/{fileName}.asset";

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Body SO Generator] Skipped: could not load sprite from {pngPath}");
                    skipped++;
                    continue;
                }

                var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                BodySO bodySo = existingAsset as BodySO;

                if (existingAsset != null && bodySo == null)
                {
                    Debug.LogError(
                        $"[Body SO Generator] Error: asset at {assetPath} exists but is not a BodySO.");
                    errors++;
                    continue;
                }

                if (bodySo == null)
                {
                    try
                    {
                        bodySo = ScriptableObject.CreateInstance<BodySO>();
                        AssetDatabase.CreateAsset(bodySo, assetPath);
                        generated++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError(
                            $"[Body SO Generator] Error creating asset at {assetPath}: {ex.Message}");
                        errors++;
                        continue;
                    }
                }
                else
                {
                    updated++;
                }

                bodySo.itemName = fileName;
                bodySo.icon = sprite;
                EditorUtility.SetDirty(bodySo);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PrintSummary(generated, updated, skipped, errors);
        }

        private static void PrintSummary(int generated, int updated, int skipped, int errors)
        {
            Debug.Log(
                $"[Body SO Generator] Done.\n" +
                $"Generated: {generated}\n" +
                $"Updated: {updated}\n" +
                $"Skipped: {skipped}\n" +
                $"Errors: {errors}");
        }
    }
}
