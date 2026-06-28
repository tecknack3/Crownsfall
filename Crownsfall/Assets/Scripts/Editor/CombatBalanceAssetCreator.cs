using Crownsfall.Combat;
using UnityEditor;
using UnityEngine;

namespace Crownsfall.Editor
{
    public static class CombatBalanceAssetCreator
    {
        private const string AssetPath = "Assets/ScriptableObjects/CombatBalance.asset";

        [MenuItem("Tools/Fighter Tools/Create Combat Balance Asset")]
        public static void CreateCombatBalanceAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CombatBalanceSO>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log($"[Combat Balance] Asset already exists at {AssetPath}. Selected in Project window.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            var balance = ScriptableObject.CreateInstance<CombatBalanceSO>();
            AssetDatabase.CreateAsset(balance, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = balance;
            EditorGUIUtility.PingObject(balance);
            Debug.Log($"[Combat Balance] Created default asset at {AssetPath}. Assign it to BattleManager → Combat Balance.");
        }
    }
}
