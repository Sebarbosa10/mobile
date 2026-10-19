using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SkinAssets
{
    public const string ArtRoot = "Assets/Art/Skins";
    public const string MeshesFolder = ArtRoot + "/Meshes";
    public const string MaterialsFolder = ArtRoot + "/Materials";
    public const string PrefabsFolder = ArtRoot + "/Prefabs";
    public const string TexturesFolder = ArtRoot + "/Textures";

    public const string DataRoot = "Assets/Data/Skins";
    public const string BallsFolder = DataRoot + "/Balls";
    public const string ThemesFolder = DataRoot + "/Themes";
    public const string IconsFolder = DataRoot + "/Icons";
    public const string ThumbnailsFolder = DataRoot + "/Thumbnails";
    public const string ResourcesFolder = DataRoot + "/Resources";
    public const string DatabasePath = ResourcesFolder + "/SkinDatabase.asset";

    public static void EnsureFolders()
    {
        foreach (string folder in new[]
                 {
                     "Assets/Art", ArtRoot, MeshesFolder, MaterialsFolder, PrefabsFolder, TexturesFolder,
                     "Assets/Data", DataRoot, BallsFolder, ThemesFolder, IconsFolder, ThumbnailsFolder, ResourcesFolder
                 })
        {
            if (AssetDatabase.IsValidFolder(folder))
                continue;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }

    public static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = $"{MeshesFolder}/{name}.asset";
        mesh.name = name;

        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        existing.Clear();
        existing.indexFormat = mesh.indexFormat;
        existing.vertices = mesh.vertices;
        existing.normals = mesh.normals;
        existing.colors = mesh.colors;
        existing.uv = mesh.uv;
        existing.subMeshCount = mesh.subMeshCount;

        for (int i = 0; i < mesh.subMeshCount; i++)
            existing.SetTriangles(mesh.GetTriangles(i), i, false);

        existing.RecalculateBounds();
        existing.name = name;
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    public static Material SaveMaterial(string name, Shader shader, System.Action<Material> configure)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        configure(material);
        material.enableInstancing = false;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader Toon => CourtAssets.FindShader("Court/FlatToon");
    private static Shader UnlitShader => CourtAssets.FindShader("Court/FlatUnlit");
    private static Shader OutlineShader => CourtAssets.FindShader("Court/Outline");

    public static Material Toon3D(
        string name,
        Color color,
        bool rim = false,
        float shadowStrength = 0.3f,
        float threshold = 0f,
        bool doubleSided = false,
        int decal = 0)
    {
        return SaveMaterial(name, Toon, mat =>
        {
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_ShadowTint", CourtMaterials.SombraTinte);
            mat.SetColor("_AmbientColor", CourtMaterials.Ambiente);
            mat.SetColor("_RimColor", CourtMaterials.Filo);
            mat.SetFloat("_ShadowStrength", shadowStrength);
            mat.SetFloat("_LightThreshold", threshold);
            mat.SetFloat("_LightSoftness", 0.06f);
            mat.SetFloat("_LightInfluence", 0.25f);
            mat.SetFloat("_AmbientInfluence", 0.15f);
            mat.SetFloat("_Cull", (float)(doubleSided ? CullMode.Off : CullMode.Back));
            SetKeyword(mat, "_RIM", "_UseRim", rim);
            SetKeyword(mat, "_VERTEX_COLOR", "_UseVertexColor", false);
            SetKeyword(mat, "_PATTERN_MAP", "_UsePatternMap", false);
            mat.SetFloat("_OffsetFactor", -decal);
            mat.SetFloat("_OffsetUnits", -decal);
            mat.renderQueue = 2000 + decal;
        });
    }

    public static Material Graphic(string name, Color color, bool doubleSided = false, int decal = 0)
    {
        Material mat = Toon3D(name, color, doubleSided: doubleSided, decal: decal);
        mat.SetFloat("_LightThreshold", -1f);
        mat.SetFloat("_LightSoftness", 0.001f);
        mat.SetFloat("_LightInfluence", 0.1f);
        return mat;
    }

    public static Material Unlit(string name, Color color, bool vertexColor = false, int queue = 2000, bool doubleSided = false)
    {
        return SaveMaterial(name, UnlitShader, mat =>
        {
            mat.SetColor("_BaseColor", color);
            SetKeyword(mat, "_VERTEX_COLOR", "_UseVertexColor", vertexColor);
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.SetFloat("_Cull", (float)(doubleSided ? CullMode.Off : CullMode.Back));
            mat.SetFloat("_OffsetFactor", 0f);
            mat.SetFloat("_OffsetUnits", 0f);
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = queue;
        });
    }

    public static Material Additive(string name, Color color, int queue = 3010)
    {
        return SaveMaterial(name, UnlitShader, mat =>
        {
            mat.SetColor("_BaseColor", color);
            SetKeyword(mat, "_VERTEX_COLOR", "_UseVertexColor", false);
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_OffsetFactor", 0f);
            mat.SetFloat("_OffsetUnits", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = queue;
        });
    }

    public static Material Outline(string name, Color color, float width = 0.022f)
    {
        return SaveMaterial(name, OutlineShader, mat =>
        {
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_OutlineWidth", width);
        });
    }

    public static void SetKeyword(Material mat, string keyword, string toggleProperty, bool on)
    {
        mat.SetFloat(toggleProperty, on ? 1f : 0f);

        if (on)
            mat.EnableKeyword(keyword);
        else
            mat.DisableKeyword(keyword);
    }

    public static GameObject SavePrefab(GameObject root, string name)
    {
        string path = $"{PrefabsFolder}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static T SaveAsset<T>(T fresh, string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(fresh, path);
            return fresh;
        }

        EditorUtility.CopySerialized(fresh, existing);
        existing.name = Path.GetFileNameWithoutExtension(path);
        Object.DestroyImmediate(fresh);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    public static Color Hex(string hex, float alpha = 1f) =>
        CourtMaterials.Hex(hex, alpha);

    public static Color Over(Color floor, Color paint, float alpha) =>
        Color.Lerp(floor, paint, alpha);
}
