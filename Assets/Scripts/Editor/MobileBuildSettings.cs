using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class MobileBuildSettings
{
    private const string ProductName = "Hoop Rush";
    private const string CompanyName = "Sebarbosa";
    private const string AndroidIdentifier = "com.sebarbosa.hooprush";
    private const string ApkPath = "Builds/HoopRush.apk";
    private const string AabPath = "Builds/HoopRush.aab";

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

        if (!PlayerSettings.enableFrameTimingStats)
        {
            PlayerSettings.enableFrameTimingStats = true;
            changed = true;
        }

        changed |= ApplyAppIdentity();
        changed |= BuildAddressablesWithPlayer();
        changed |= DisableShadows();

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[MobileBuildSettings] Frame pacing, orientación vertical y sombras de URP apagadas (las del juego son pintadas).");
        }
    }

    private static bool ApplyAppIdentity()
    {
        bool changed = false;

        if (PlayerSettings.productName != ProductName)
        {
            PlayerSettings.productName = ProductName;
            changed = true;
        }

        if (PlayerSettings.companyName != CompanyName)
        {
            PlayerSettings.companyName = CompanyName;
            changed = true;
        }

        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != AndroidIdentifier)
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidIdentifier);
            changed = true;
        }

        return changed;
    }

    // Sin esto los bancos de Wwise pueden no entrar en el APK si la preferencia del editor está apagada
    private static bool BuildAddressablesWithPlayer()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null || settings.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
            return false;

        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
        EditorUtility.SetDirty(settings);

        return true;
    }

    [MenuItem("Tools/Build/Android APK")]
    private static void BuildAndroidApk()
    {
        if (MonetizationConfig.UsingTestAds)
            Debug.Log("[MobileBuildSettings] Build con anuncios de prueba de Google.");

        Build(ApkPath, appBundle: false);
    }

    // Para subir a Google Play: exige IDs reales de anuncios y una keystore propia
    [MenuItem("Tools/Build/Android AAB (publicación)")]
    private static void BuildAndroidBundle()
    {
        if (MonetizationConfig.UsingTestAds)
        {
            Debug.LogError("[MobileBuildSettings] MonetizationConfig todavía usa IDs de prueba: no se puede publicar así.");
            return;
        }

        if (!PlayerSettings.Android.useCustomKeystore)
        {
            Debug.LogError("[MobileBuildSettings] Falta la keystore de firma: Project Settings > Player > Publishing Settings.");
            return;
        }

        Build(AabPath, appBundle: true);
    }

    private static void Build(string path, bool appBundle)
    {
        Directory.CreateDirectory("Builds");

        bool previous = EditorUserBuildSettings.buildAppBundle;
        EditorUserBuildSettings.buildAppBundle = appBundle;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android
        });

        EditorUserBuildSettings.buildAppBundle = previous;

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[MobileBuildSettings] Build lista: {Path.GetFullPath(path)}");
            EditorUtility.RevealInFinder(path);
        }
        else
        {
            Debug.LogError($"[MobileBuildSettings] La build falló: {report.summary.result}, {report.summary.totalErrors} errores.");
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
