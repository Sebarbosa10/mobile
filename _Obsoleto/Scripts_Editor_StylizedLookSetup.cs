using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Stylized Look > Aplicar a las escenas
///
///  1. Agrega StylizedRendererFeature (con sus dos shaders) a todos los
///     URP Renderers que usan las calidades del proyecto.
///  2. En DummyLab y Challenges pone un objeto "StylizedLook" (los
///     ajustes del look) y BallCloseUpScale en la pelota.
///
/// Se puede correr las veces que haga falta: lo que ya está no se toca.
/// La primera vez corre solo, apenas compila.
/// </summary>
[InitializeOnLoad]
public static class StylizedLookSetup
{
    private const string PosterizeShaderPath = "Assets/Art/PostFX/Posterize.shader";
    private const string StylizedShaderPath = "Assets/Art/PostFX/StylizedPost.shader";
    private const string LookObjectName = "StylizedLook";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/DummyLab.unity",
        "Assets/Scenes/Challenges.unity"
    };

    private static string DoneKey => "mobile.StylizedLookSetup.v1:" + Application.dataPath;

    static StylizedLookSetup()
    {
        if (!EditorPrefs.GetBool(DoneKey, false))
            EditorApplication.delayCall += AutoRun;
    }

    private static void AutoRun()
    {
        if (EditorPrefs.GetBool(DoneKey, false))
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AutoRun;
            return;
        }

        Run();
    }

    [MenuItem("Tools/Stylized Look/Aplicar a las escenas")]
    public static void Run()
    {
        Shader posterize = AssetDatabase.LoadAssetAtPath<Shader>(PosterizeShaderPath);
        Shader stylized = AssetDatabase.LoadAssetAtPath<Shader>(StylizedShaderPath);

        if (posterize == null || stylized == null)
        {
            Debug.LogError($"[Stylized Look] Faltan los shaders en {PosterizeShaderPath} / {StylizedShaderPath}.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        StringBuilder report = new();
        string previousScene = SceneManager.GetActiveScene().path;

        try
        {
            SetupRenderers(posterize, stylized, report);

            foreach (string path in ScenePaths)
                SetupScene(path, report);

            AssetDatabase.SaveAssets();
            EditorPrefs.SetBool(DoneKey, true);
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene) && System.IO.File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        Debug.Log("[Stylized Look]\n" + report);
    }

    // ------------------------------------------------------------ Renderers

    private static void SetupRenderers(Shader posterize, Shader stylized, StringBuilder report)
    {
        HashSet<ScriptableRendererData> renderers = new();

        foreach (UniversalRenderPipelineAsset asset in PipelineAssets())
        {
            SerializedProperty list = new SerializedObject(asset).FindProperty("m_RendererDataList");

            for (int i = 0; list != null && i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableRendererData data)
                    renderers.Add(data);
        }

        foreach (ScriptableRendererData data in renderers)
        {
            StylizedRendererFeature feature = data.rendererFeatures.OfType<StylizedRendererFeature>().FirstOrDefault();

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<StylizedRendererFeature>();
                feature.name = "StylizedLook";
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

                SerializedObject dataSo = new(data);
                SerializedProperty features = dataSo.FindProperty("m_RendererFeatures");
                SerializedProperty map = dataSo.FindProperty("m_RendererFeatureMap");
                int index = features.arraySize;
                features.InsertArrayElementAtIndex(index);
                features.GetArrayElementAtIndex(index).objectReferenceValue = feature;
                map.InsertArrayElementAtIndex(index);
                map.GetArrayElementAtIndex(index).longValue = localId;
                dataSo.ApplyModifiedPropertiesWithoutUndo();

                report.AppendLine($"  + feature en {data.name}");
            }

            SerializedObject featureSo = new(feature);
            featureSo.FindProperty("posterizeShader").objectReferenceValue = posterize;
            featureSo.FindProperty("stylizedShader").objectReferenceValue = stylized;
            featureSo.ApplyModifiedPropertiesWithoutUndo();

            data.SetDirty();
            EditorUtility.SetDirty(data);
        }
    }

    private static IEnumerable<UniversalRenderPipelineAsset> PipelineAssets()
    {
        HashSet<UniversalRenderPipelineAsset> assets = new();

        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset main)
            assets.Add(main);

        for (int i = 0; i < QualitySettings.names.Length; i++)
            if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset level)
                assets.Add(level);

        return assets;
    }

    // -------------------------------------------------------------- Escenas

    private static void SetupScene(string path, StringBuilder report)
    {
        if (!System.IO.File.Exists(path))
        {
            report.AppendLine($"  ! no existe {path}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        report.AppendLine($"{scene.name}:");

        if (FindAll<StylizedLook>(scene).Count == 0)
        {
            GameObject look = new(LookObjectName);
            SceneManager.MoveGameObjectToScene(look, scene);
            look.AddComponent<StylizedLook>();
            report.AppendLine("  + StylizedLook");
        }

        BallLauncher launcher = FindAll<BallLauncher>(scene).FirstOrDefault();

        if (launcher == null)
        {
            report.AppendLine("  ! no hay BallLauncher (pelota)");
        }
        else if (!launcher.TryGetComponent(out BallCloseUpScale _))
        {
            BallCloseUpScale closeUp = launcher.gameObject.AddComponent<BallCloseUpScale>();
            BallColor art = launcher.GetComponentInChildren<BallColor>(true);

            SerializedObject so = new(closeUp);
            so.FindProperty("art").objectReferenceValue = art != null ? art.transform : null;
            so.FindProperty("shadow").objectReferenceValue = FindAll<BallBlobShadow>(scene).FirstOrDefault();
            so.ApplyModifiedPropertiesWithoutUndo();

            report.AppendLine("  + BallCloseUpScale en la pelota");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static List<T> FindAll<T>(Scene scene) where T : Component
    {
        List<T> found = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<T>(true));
        return found;
    }
}
