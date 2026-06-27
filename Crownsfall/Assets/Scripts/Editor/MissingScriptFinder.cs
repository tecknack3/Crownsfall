using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crownsfall.Editor
{
    /// <summary>
    /// Scans the active scene hierarchy for GameObjects with missing MonoBehaviour scripts.
    /// Read-only: reports results to the Console and selects the first affected object.
    /// </summary>
    public static class MissingScriptFinder
    {
        [MenuItem("Tools/Debug/Find Missing Scripts")]
        public static void FindMissingScripts()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                Debug.LogWarning("No active scene is loaded.");
                return;
            }

            var objectsWithMissingScripts = new List<GameObject>();

            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                ScanHierarchy(root.transform, objectsWithMissingScripts);
            }

            if (objectsWithMissingScripts.Count == 0)
            {
                Debug.Log("No missing scripts found in scene.");
                return;
            }

            foreach (GameObject go in objectsWithMissingScripts)
            {
                string hierarchyPath = GetHierarchyPath(go.transform);
                Debug.Log(
                    $"Missing script on: \"{go.name}\" at path: {hierarchyPath}",
                    go);
            }

            Selection.activeGameObject = objectsWithMissingScripts[0];
        }

        private static void ScanHierarchy(Transform transform, List<GameObject> results)
        {
            GameObject gameObject = transform.gameObject;

            if (HasMissingScript(gameObject))
            {
                results.Add(gameObject);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                ScanHierarchy(transform.GetChild(i), results);
            }
        }

        private static bool HasMissingScript(GameObject gameObject)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0)
            {
                return true;
            }

            // Fallback: null entries in GetComponents indicate missing scripts.
            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new List<string>();
            Transform current = transform;

            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
    }
}
