using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class MobileBuildSettings
{
    static MobileBuildSettings()
    {
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        bool changed = false;

        if (!PlayerSettings.Android.optimizedFramePacing)
        {
            PlayerSettings.Android.optimizedFramePacing = true;
            changed = true;
        }

        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            changed = true;
        }

        changed |= DisableShadows();

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[MobileBuildSettings] Frame pacing, orientación vertical y sombras de URP apagadas (las del juego son pintadas).");
        }
    }

    private static bool DisableShadows()
    {
        bool changed = false;
        HashSet<UniversalRenderPipelineAsset> assets = new();

        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset main)
            assets.Add(main);

        for (int i = 0; i < QualitySettings.names.Length; i++)
            if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset level)
                assets.Add(level);

        foreach (UniversalRenderPipelineAsset asset in assets)
        {
            SerializedObject so = new(asset);

            foreach (string property in new[] { "m_MainLightShadowsSupported", "m_SoftShadowsSupported", "m_AdditionalLightShadowsSupported" })
            {
                SerializedProperty flag = so.FindProperty(property);

                if (flag != null && flag.boolValue)
                {
                    flag.boolValue = false;
                    changed = true;
                }
            }

            if (so.ApplyModifiedPropertiesWithoutUndo())
                EditorUtility.SetDirty(asset);
        }

        return changed;
    }
}
