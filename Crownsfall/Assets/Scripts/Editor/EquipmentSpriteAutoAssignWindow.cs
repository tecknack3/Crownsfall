using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crownsfall.Characters;
using UnityEditor;
using UnityEngine;

namespace Crownsfall.Editor
{
    public class EquipmentSpriteAutoAssignWindow : EditorWindow
    {
        private const int MinKeywordLength = 3;

        [MenuItem("Tools/Auto Assign/Equipment Sprites")]
        public static void ShowWindow()
        {
            GetWindow<EquipmentSpriteAutoAssignWindow>("Equipment Sprites");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Matches ScriptableObjects to PNG sprites using cleaned name containment and keyword overlap " +
                "(case-insensitive; ignores numbers, spaces, underscores, and special characters).",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Auto Assign Sprites", GUILayout.Height(30)))
            {
                AutoAssignSprites();
            }
        }

        private static void AutoAssignSprites()
        {
            int assigned = 0;
            int failed = 0;

            assigned += ProcessCategory<HeadSO>("HeadSO", "Assets/ScriptableObjects/Heads", "Assets/Art/Heads", ref failed);
            assigned += ProcessCategory<BodySO>("BodySO", "Assets/ScriptableObjects/Bodies", "Assets/Art/Bodies", ref failed);
            assigned += ProcessCategory<WeaponSO>("WeaponSO", "Assets/ScriptableObjects/Weapons", "Assets/Art/Weapons", ref failed);
            assigned += ProcessCategory<MountSO>("MountSO", "Assets/ScriptableObjects/Mounts", "Assets/Art/Mounts", ref failed);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Equipment Sprites] Done. Assigned: {assigned}, Failed: {failed}");
        }

        private static int ProcessCategory<T>(
            string typeName,
            string soFolder,
            string artFolder,
            ref int failed) where T : EquipmentItemSO
        {
            int assigned = 0;

            if (!AssetDatabase.IsValidFolder(soFolder))
            {
                Debug.LogWarning($"[Equipment Sprites] SO folder not found: {soFolder}");
                return 0;
            }

            var spriteEntries = BuildSpriteEntries(artFolder);
            var guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { soFolder });

            if (guids.Length == 0)
            {
                Debug.Log($"[Equipment Sprites] No {typeName} assets found in {soFolder}");
                return 0;
            }

            if (spriteEntries.Count == 0)
            {
                Debug.LogWarning($"[Equipment Sprites] No PNG sprites found in {artFolder}");
            }

            foreach (var guid in guids)
            {
                var soPath = AssetDatabase.GUIDToAssetPath(guid);
                var equipment = AssetDatabase.LoadAssetAtPath<T>(soPath);

                if (equipment == null)
                    continue;

                var soFileName = Path.GetFileNameWithoutExtension(soPath);
                var soCleaned = CleanName(soFileName);
                var soKeywords = ExtractKeywords(soFileName);

                Debug.Log(
                    $"[Equipment Sprites] Attempt: {soPath}\n" +
                    $"  SO cleaned: '{soCleaned}', keywords: [{FormatKeywordList(soKeywords)}]");

                var candidates = new List<MatchCandidate>();

                foreach (var entry in spriteEntries)
                {
                    var score = ComputeMatchScore(soCleaned, soKeywords, entry.CleanedName, entry.Keywords);
                    var reason = DescribeMatch(soCleaned, soKeywords, entry.CleanedName, entry.Keywords, score);

                    Debug.Log(
                        $"  PNG: {entry.Path}\n" +
                        $"    cleaned: '{entry.CleanedName}', keywords: [{FormatKeywordList(entry.Keywords)}]\n" +
                        $"    {reason}");

                    if (score > 0)
                    {
                        candidates.Add(new MatchCandidate
                        {
                            Path = entry.Path,
                            CleanedName = entry.CleanedName,
                            Score = score
                        });
                    }
                }

                if (candidates.Count == 0)
                {
                    Debug.LogWarning(
                        $"[Equipment Sprites] FAILED: {soPath} — no matching sprite in {artFolder} " +
                        $"(SO cleaned: '{soCleaned}', keywords: [{FormatKeywordList(soKeywords)}])");
                    failed++;
                    continue;
                }

                var bestScore = candidates.Max(c => c.Score);
                var bestCandidates = candidates
                    .Where(c => c.Score == bestScore)
                    .OrderBy(c => c.Path)
                    .ToList();

                if (bestCandidates.Count > 1)
                {
                    Debug.LogWarning(
                        $"[Equipment Sprites] Multiple sprites match '{soFileName}' with score {bestScore}: " +
                        $"{string.Join(", ", bestCandidates.Select(c => c.Path))}. Using first.");
                }

                var spritePath = bestCandidates[0].Path;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                if (sprite == null)
                {
                    Debug.LogWarning($"[Equipment Sprites] FAILED: {soPath} — could not load sprite at {spritePath}");
                    failed++;
                    continue;
                }

                equipment.icon = sprite;
                EditorUtility.SetDirty(equipment);
                Debug.Log(
                    $"[Equipment Sprites] SUCCESS: {soPath} ← {spritePath} " +
                    $"(score: {bestScore}, SO cleaned: '{soCleaned}', PNG cleaned: '{bestCandidates[0].CleanedName}')");
                assigned++;
            }

            return assigned;
        }

        private static List<SpriteEntry> BuildSpriteEntries(string artFolder)
        {
            var entries = new List<SpriteEntry>();

            if (!AssetDatabase.IsValidFolder(artFolder))
            {
                Debug.LogWarning($"[Equipment Sprites] Art folder not found: {artFolder}");
                return entries;
            }

            var guids = AssetDatabase.FindAssets("", new[] { artFolder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                entries.Add(new SpriteEntry
                {
                    Path = path,
                    CleanedName = CleanName(fileName),
                    Keywords = ExtractKeywords(fileName)
                });
            }

            return entries;
        }

        /// <summary>
        /// Lowercase and keep only letters a-z.
        /// </summary>
        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            name = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();

            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (c >= 'a' && c <= 'z')
                    sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Split on _, spaces, and -; strip numbers/special chars from each token; lowercase letters only.
        /// Also includes the full cleaned name when it is a meaningful token.
        /// </summary>
        private static List<string> ExtractKeywords(string name)
        {
            var keywords = new HashSet<string>();

            if (string.IsNullOrEmpty(name))
                return new List<string>();

            name = Path.GetFileNameWithoutExtension(name);
            var tokens = Regex.Split(name, @"[_\s\-]+");

            foreach (var token in tokens)
            {
                var cleaned = CleanName(token);
                if (cleaned.Length >= MinKeywordLength)
                    keywords.Add(cleaned);
            }

            var fullCleaned = CleanName(name);
            if (fullCleaned.Length >= MinKeywordLength)
                keywords.Add(fullCleaned);

            return keywords.OrderBy(k => k).ToList();
        }

        private static int ComputeMatchScore(
            string soCleaned,
            IReadOnlyList<string> soKeywords,
            string pngCleaned,
            IReadOnlyList<string> pngKeywords)
        {
            if (string.IsNullOrEmpty(soCleaned) || string.IsNullOrEmpty(pngCleaned))
                return 0;

            int score = 0;

            if (soCleaned.Contains(pngCleaned) || pngCleaned.Contains(soCleaned))
                score++;

            foreach (var kw in soKeywords)
            {
                if (kw.Length >= MinKeywordLength && pngCleaned.Contains(kw))
                    score++;
            }

            foreach (var kw in pngKeywords)
            {
                if (kw.Length >= MinKeywordLength && soCleaned.Contains(kw))
                    score++;
            }

            return score;
        }

        private static string DescribeMatch(
            string soCleaned,
            IReadOnlyList<string> soKeywords,
            string pngCleaned,
            IReadOnlyList<string> pngKeywords,
            int score)
        {
            if (score <= 0)
                return "no match";

            var reasons = new List<string>();

            if (soCleaned.Contains(pngCleaned))
                reasons.Add($"SO cleaned name contains PNG cleaned name '{pngCleaned}'");

            if (pngCleaned.Contains(soCleaned))
                reasons.Add($"PNG cleaned name contains SO cleaned name '{soCleaned}'");

            foreach (var kw in soKeywords)
            {
                if (kw.Length >= MinKeywordLength && pngCleaned.Contains(kw))
                    reasons.Add($"SO keyword '{kw}' found in PNG cleaned name");
            }

            foreach (var kw in pngKeywords)
            {
                if (kw.Length >= MinKeywordLength && soCleaned.Contains(kw))
                    reasons.Add($"PNG keyword '{kw}' found in SO cleaned name");
            }

            return $"score: {score} — {string.Join("; ", reasons)}";
        }

        private static string FormatKeywordList(IReadOnlyList<string> keywords)
        {
            return keywords == null || keywords.Count == 0
                ? string.Empty
                : string.Join(", ", keywords);
        }

        private struct SpriteEntry
        {
            public string Path;
            public string CleanedName;
            public List<string> Keywords;
        }

        private struct MatchCandidate
        {
            public string Path;
            public string CleanedName;
            public int Score;
        }
    }
}
