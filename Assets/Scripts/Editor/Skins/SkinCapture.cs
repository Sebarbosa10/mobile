using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class SkinCapture
{
    public const int ThumbnailWidth = 956;
    public const int ThumbnailHeight = 380;
    public const int IconSize = 280;

    [MenuItem("Tools/Capture Theme Thumbnails")]
    public static void CaptureThemeThumbnailsMenu() =>
        RunInTempScene(report => CaptureThemeThumbnails(LoadDatabase(), report));

    [MenuItem("Tools/Skins/Capture Ball Icons")]
    public static void CaptureBallIconsMenu() =>
        RunInTempScene(report => CaptureBallIcons(LoadDatabase(), report));

    private static SkinDatabase LoadDatabase() =>
        AssetDatabase.LoadAssetAtPath<SkinDatabase>(SkinAssets.DatabasePath);

    public static void RunInTempScene(System.Action<StringBuilder> work)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string previous = SceneManager.GetActiveScene().path;
        StringBuilder report = new();

        try
        {
            work(report);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }

        Debug.Log("[Skins]\n" + report);
    }

    public static void CaptureThemeThumbnails(SkinDatabase db, StringBuilder report)
    {
        if (db == null)
        {
            report.AppendLine("  ! falta la SkinDatabase: corré Tools > Skins > Build All");
            return;
        }

        SkinAssets.EnsureFolders();

        foreach (BackgroundTheme theme in db.themes)
        {
            if (theme == null)
                continue;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLight();
            BuildThemeStage(theme);

            Camera camera = CreateCamera(theme.HorizonColor);
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1.5f, -2.5f), Quaternion.Euler(-4.9f, 0f, 0f));
            camera.fieldOfView = 30f;

            string path = $"{SkinAssets.ThumbnailsFolder}/{theme.id}.png";
            Render(camera, ThumbnailWidth, ThumbnailHeight, path);
            ImportThumbnail(path);

            theme.thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            EditorUtility.SetDirty(theme);
            report.AppendLine($"  miniatura {theme.displayName} → {path}");
        }
    }

    private static void BuildThemeStage(BackgroundTheme theme)
    {
        if (theme.environmentPrefab == null)
        {
            Instantiate(CourtAssets.LoadPrefab(ThemeApplier.EnvironmentName));
        }
        else
        {
            GameObject court = Instantiate(CourtAssets.LoadPrefab(ThemeApplier.CourtName));
            Instantiate(theme.environmentPrefab);

            if (court != null)
                ThemeApplier.ApplyFloor(court.GetComponentInChildren<MeshRenderer>(), theme);
        }

        GameObject hoop = Instantiate(CourtAssets.LoadPrefab("Hoop"));

        if (hoop != null)
        {
            hoop.transform.position = CourtLayout.RimCenter;
            hoop.GetComponent<HoopVisual>()?.SnapPoleToGround();
        }
    }

    public static void CaptureBallIcons(SkinDatabase db, StringBuilder report)
    {
        if (db == null)
        {
            report.AppendLine("  ! falta la SkinDatabase: corré Tools > Skins > Build All");
            return;
        }

        SkinAssets.EnsureFolders();
        Color card = CourtMaterials.Hex("#2A2140");

        foreach (BallSkin skin in db.balls)
        {
            if (skin == null || skin.prefab == null)
                continue;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLight();

            GameObject ball = Instantiate(skin.prefab);
            Bounds bounds = new(Vector3.zero, Vector3.one);

            foreach (Renderer r in ball.GetComponentsInChildren<Renderer>())
                if (r.GetComponentInParent<BallSparkles>() == null)
                    bounds.Encapsulate(r.bounds);

            Camera camera = CreateCamera(card);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.04f;
            camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.center.y, -5f), Quaternion.identity);

            string path = $"{SkinAssets.IconsFolder}/{skin.id}.png";
            Render(camera, IconSize, IconSize, path);
            ImportIcon(path);

            skin.icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EditorUtility.SetDirty(skin);
            report.AppendLine($"  ícono {skin.displayName} → {path}");
        }
    }

    private static GameObject Instantiate(GameObject prefab) =>
        prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : null;

    private static void SetupLight()
    {
        Light sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = CourtLayout.LightRotation;
        sun.color = CourtMaterials.LuzSol;
        sun.intensity = 1f;
        sun.shadows = LightShadows.None;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = CourtMaterials.Ambiente;
    }

    private static Camera CreateCamera(Color background)
    {
        Camera camera = new GameObject("CaptureCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 400f;
        camera.allowHDR = false;

        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.renderShadows = false;

        return camera;
    }

    private static void Render(Camera camera, int width, int height, string path)
    {
        RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            antiAliasing = 8
        };

        camera.aspect = width / (float)height;
        camera.targetTexture = target;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;

        Texture2D image = new(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        RenderTexture.active = previous;
        camera.targetTexture = null;

        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        target.Release();
        Object.DestroyImmediate(target);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void ImportThumbnail(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 1024;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void ImportIcon(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }
}
