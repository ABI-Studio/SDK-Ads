using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace SDK
{
    [InitializeOnLoad]

    public class ABIEditorConfig
    {
        static ABIEditorConfig()
        {
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() != null)
                {
                    foreach (var a in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(ScriptOrder)))
                    {
                        var currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                        var newOrder = ((ScriptOrder)a).order;
                        if (currentOrder != newOrder)
                            MonoImporter.SetExecutionOrder(monoScript, newOrder);
                    }
                }
            }
        }
    }

    public class DefineSymbolsAdder
    {
        private const string MAX_MEDIATION_SYMBOL = "UNITY_AD_MAX";
        private const string IRONSOURCE_MEDIATION_SYMBOL = "UNITY_AD_IRONSOURCE";
            
        private static void ActiveAdMaxMediation()
        {
            string defineSymbol = MAX_MEDIATION_SYMBOL; 

            string currentDefineSymbols =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string[] defineSymbols = currentDefineSymbols.Split(';');
            List<string> defineSymbolList = new List<string>(defineSymbols);
            defineSymbolList.Remove(IRONSOURCE_MEDIATION_SYMBOL);
            currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
            if (!currentDefineSymbols.Contains(defineSymbol))
            {
                currentDefineSymbols += ";" + defineSymbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    currentDefineSymbols);
            }
        }
        private static void ActiveAdIronsourceMediation()
        {
            string defineSymbol = IRONSOURCE_MEDIATION_SYMBOL; 

            string currentDefineSymbols =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string[] defineSymbols = currentDefineSymbols.Split(';');
            List<string> defineSymbolList = new List<string>(defineSymbols);
            defineSymbolList.Remove(MAX_MEDIATION_SYMBOL);
            currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
            if (!currentDefineSymbols.Contains(defineSymbol))
            {
                currentDefineSymbols += ";" + defineSymbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                    currentDefineSymbols);
            }
        }
        
        [MenuItem("SDK Setup/Setup Ads Mediation")]
        static void OpenMaxAdConfig()
        {
            AddDefineSymbol("UNITY_AD_ADMOB");
            string directory = "Assets/ABIAdsConfig/";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                AssetDatabase.CreateFolder("Assets", "ABIAdsConfig");
            }
            string assetName = "SDKAdsSetup.asset";
            string assetPath = $"{directory}{assetName}";
            SDKSetup selectedScriptableObject = AssetDatabase.LoadAssetAtPath<SDKSetup>(assetPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<SDKSetup>();
                AssetDatabase.CreateAsset(selectedScriptableObject, assetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
        }
        
        [MenuItem("SDK Setup/Watch Video Reward Type")]
        static void OpenRewardAdsPlacementConfig()
        {
            string directory = "Assets/ABIAdsConfig/";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                AssetDatabase.CreateFolder("Assets", "ABIAdsConfig");
            }
            
            string assetName = "RewardAdsPlacementConfig.asset";
            string assetPath = $"{directory}{assetName}";
            RewardAdsPlacementConfig selectedScriptableObject = AssetDatabase.LoadAssetAtPath<RewardAdsPlacementConfig>(assetPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<RewardAdsPlacementConfig>();
                AssetDatabase.CreateAsset(selectedScriptableObject, assetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
        }
        private static void AddDefineSymbol(string defineSymbol)
        {
            string currentDefineSymbols =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string[] defineSymbols = currentDefineSymbols.Split(';');
            List<string> defineSymbolList = new List<string>(defineSymbols);
            currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
            if (currentDefineSymbols.Contains(defineSymbol)) return;
            currentDefineSymbols += ";" + defineSymbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                currentDefineSymbols);
        }
        
    }
}