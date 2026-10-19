using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class PaletteAtlasBuilder
{
    private const string TexturesFolder = CourtAssets.Root + "/Textures";
    private const string AtlasPath = TexturesFolder + "/T_PaletteAtlas.png";
    private const string EntriesPath = TexturesFolder + "/T_PaletteAtlas.json";
    private const string AtlasShader = "Court/FlatToonAtlas";
    private const string SourceShader = "Court/FlatToon";

    private const int CellSize = 4;
    private const int Columns = 16;
    private const int AtlasSize = CellSize * Columns;
    private const int MaxEntries = Columns * Columns;

    [Serializable]
    private sealed class EntryList
    {
        public List<string> entries = new();
    }

    [MenuItem("Tools/Optimize/Palette Atlas")]
    public static void BuildMenu()
    {
        string report = Build();
        Debug.Log("[Palette Atlas]\n" + report);
        EditorUtility.DisplayDialog("Palette Atlas", report, "OK");
    }

    public static string Build()
    {
        EnsureFolder(TexturesFolder);

        EntryList entries = LoadEntries();
        Dictionary<string, Mesh> converted = new();
        StringBuilder report = new();

        int renderers = 0;
        int drawCallsBefore = 0;

        List<string> prefabPaths = FindPrefabs();

        foreach (string path in prefabPaths)
        {
            WithPrefab(path, root =>
            {
                foreach (MeshRenderer renderer in Candidates(root))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        string key = EntryKey(material);

                        if (!entries.entries.Contains(key))
                            entries.entries.Add(key);
                    }
                }

                return false;
            });
        }

        if (entries.entries.Count > MaxEntries)
            return $"! La paleta tiene {entries.entries.Count} colores y el atlas entra {MaxEntries}: no se convirtió nada.";

        SaveEntries(entries);
        Texture2D atlas = WriteAtlas(entries);
        Material single = AtlasMaterial("M_PaletteAtlas", atlas, CullMode.Back);
        Material doubleSided = AtlasMaterial("M_PaletteAtlas_DobleCara", atlas, CullMode.Off);

        foreach (string path in prefabPaths)
        {
            WithPrefab(path, root =>
            {
                bool changed = false;

                foreach (MeshRenderer renderer in Candidates(root))
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    Material[] materials = renderer.sharedMaterials;
                    int[] cells = materials.Select(m => entries.entries.IndexOf(EntryKey(m))).ToArray();

                    drawCallsBefore += materials.Length;
                    renderers++;

                    filter.sharedMesh = AtlasMesh(filter.sharedMesh, cells, converted);
                    bool anyDoubleSided = materials.Any(m => (CullMode)(int)m.GetFloat("_Cull") == CullMode.Off);
                    renderer.sharedMaterials = new[] { anyDoubleSided ? doubleSided : single };
                    changed = true;
                }

                if (changed)
                    report.AppendLine($"  {Path.GetFileNameWithoutExtension(path)}");

                return changed;
            });
        }

        AssetDatabase.SaveAssets();

        report.Insert(0,
            $"Atlas: {entries.entries.Count} colores en {AtlasPath}\n" +
            $"Renderers convertidos: {renderers} (de {drawCallsBefore} draw calls a {renderers}, antes del static batching)\n");

        return report.ToString();
    }

    private static List<string> FindPrefabs()
    {
        List<string> folders = new() { CourtAssets.PrefabsFolder };

        if (AssetDatabase.IsValidFolder(SkinAssets.PrefabsFolder))
            folders.Add(SkinAssets.PrefabsFolder);

        return AssetDatabase.FindAssets("t:Prefab", folders.ToArray())
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToList();
    }

    private static void WithPrefab(string path, Func<GameObject, bool> work)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            if (work(root))
                PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static IEnumerable<MeshRenderer> Candidates(GameObject root)
    {
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.gameObject != root && PrefabUtility.IsPartOfPrefabInstance(renderer.gameObject))
                continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            Material[] materials = renderer.sharedMaterials;

            if (mesh == null || materials.Length == 0 || materials.Length != mesh.subMeshCount)
                continue;

            if (materials.All(IsAtlasable))
                yield return renderer;
        }
    }

    private static bool IsAtlasable(Material material)
    {
        if (material == null || material.shader == null || material.shader.name != SourceShader)
            return false;

        if (material.IsKeywordEnabled("_RIM") || material.IsKeywordEnabled("_VERTEX_COLOR") || material.IsKeywordEnabled("_PATTERN_MAP"))
            return false;

        if (Mathf.Abs(material.GetFloat("_OffsetFactor")) > 0.001f || Mathf.Abs(material.GetFloat("_OffsetUnits")) > 0.001f)
            return false;

        if ((CullMode)(int)material.GetFloat("_Cull") == CullMode.Front)
            return false;

        float threshold = material.GetFloat("_LightThreshold");
        return threshold <= -0.99f || Mathf.Abs(threshold) < 0.001f;
    }

    private static string EntryKey(Material material)
    {
        Color32 color = material.GetColor("_BaseColor");
        bool graphic = material.GetFloat("_LightThreshold") <= -0.99f;
        color.a = graphic ? (byte)255 : (byte)Mathf.Clamp(Mathf.RoundToInt(material.GetFloat("_ShadowStrength") * 255f), 0, 250);
        return ColorUtility.ToHtmlStringRGBA(color);
    }

    private static EntryList LoadEntries() =>
        File.Exists(EntriesPath) ? JsonUtility.FromJson<EntryList>(File.ReadAllText(EntriesPath)) ?? new EntryList() : new EntryList();

    private static void SaveEntries(EntryList entries)
    {
        File.WriteAllText(EntriesPath, JsonUtility.ToJson(entries, true));
        AssetDatabase.ImportAsset(EntriesPath);
    }

    private static Texture2D WriteAtlas(EntryList entries)
    {
        Texture2D texture = new(AtlasSize, AtlasSize, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[AtlasSize * AtlasSize];

        for (int i = 0; i < entries.entries.Count; i++)
        {
            ColorUtility.TryParseHtmlString("#" + entries.entries[i], out Color color);
            Vector2Int origin = CellOrigin(i);

            for (int y = 0; y < CellSize; y++)
                for (int x = 0; x < CellSize; x++)
                    pixels[(origin.y + y) * AtlasSize + origin.x + x] = color;
        }

        texture.SetPixels32(pixels);
        File.WriteAllBytes(AtlasPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
    }

    private static Vector2Int CellOrigin(int index) =>
        new(index % Columns * CellSize, index / Columns * CellSize);

    private static Vector2 CellCenter(int index)
    {
        Vector2Int origin = CellOrigin(index);
        return new Vector2((origin.x + CellSize * 0.5f) / AtlasSize, (origin.y + CellSize * 0.5f) / AtlasSize);
    }

    private static Material AtlasMaterial(string name, Texture2D atlas, CullMode cull)
    {
        return CourtAssets.SaveMaterial(name, CourtAssets.FindShader(AtlasShader), mat =>
        {
            mat.SetTexture("_AtlasMap", atlas);
            mat.SetColor("_ShadowTint", CourtMaterials.SombraTinte);
            mat.SetColor("_AmbientColor", CourtMaterials.Ambiente);
            mat.SetFloat("_LightSoftness", 0.06f);
            mat.SetFloat("_LightInfluence", 0.25f);
            mat.SetFloat("_GraphicLightInfluence", 0.1f);
            mat.SetFloat("_AmbientInfluence", 0.15f);
            mat.SetFloat("_Cull", (float)cull);
            mat.renderQueue = 2000;
        });
    }

    private static Mesh AtlasMesh(Mesh source, int[] cells, Dictionary<string, Mesh> converted)
    {
        string key = AssetDatabase.GetAssetPath(source) + "|" + source.name + "|" + string.Join(",", cells);

        if (converted.TryGetValue(key, out Mesh cached))
            return cached;

        Vector3[] vertices = source.vertices;
        Vector2[] uvs = new Vector2[vertices.Length];
        List<int> triangles = new();

        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            int[] subTriangles = source.GetTriangles(sub);
            Vector2 uv = CellCenter(cells[sub]);

            foreach (int index in subTriangles)
                uvs[index] = uv;

            triangles.AddRange(subTriangles);
        }

        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(source))?.Replace('\\', '/') ?? CourtAssets.MeshesFolder;
        string baseName = $"{source.name}_Atlas";
        string name = baseName;
        int suffix = 2;

        while (converted.Values.Any(m => m.name == name))
            name = $"{baseName}{suffix++}";

        string path = $"{folder}/{name}.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (mesh == null)
        {
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
        }

        mesh.Clear();
        mesh.name = name;
        mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.normals = source.normals;
        mesh.colors = source.colors;
        mesh.uv = uvs;
        mesh.subMeshCount = 1;
        mesh.SetTriangles(triangles, 0, false);
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);

        converted[key] = mesh;
        return mesh;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
