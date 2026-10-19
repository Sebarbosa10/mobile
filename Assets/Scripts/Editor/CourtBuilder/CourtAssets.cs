using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Guardado idempotente de los assets que genera el builder: si el asset
/// ya existe se actualiza en el lugar (mismo GUID, así las referencias de
/// escenas y prefabs no se rompen); si no, se crea. Correr el builder dos
/// veces no duplica nada.
/// </summary>
public static class CourtAssets
{
    public const string Root = "Assets/Art/Scene";
    public const string MeshesFolder = Root + "/Meshes";
    public const string MaterialsFolder = Root + "/Materials";
    public const string PrefabsFolder = Root + "/Prefabs";
    public const string ShadersFolder = Root + "/Shaders";

    public static void EnsureFolders()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder(Root);
        EnsureFolder(MeshesFolder);
        EnsureFolder(MaterialsFolder);
        EnsureFolder(PrefabsFolder);
        EnsureFolder(ShadersFolder);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    /// <summary>Guarda (o actualiza) un mesh como .asset y devuelve el asset persistido.</summary>
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

        // Copia explícita de los canales que usamos (posición, normal,
        // color y submeshes): sin UVs ni tangentes, porque no hay texturas.
        existing.Clear();
        existing.indexFormat = mesh.indexFormat;
        existing.vertices = mesh.vertices;
        existing.normals = mesh.normals;
        existing.colors = mesh.colors;
        existing.subMeshCount = mesh.subMeshCount;

        for (int i = 0; i < mesh.subMeshCount; i++)
            existing.SetTriangles(mesh.GetTriangles(i), i, false);

        existing.RecalculateBounds();
        existing.name = name;
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(mesh);
        return existing;
    }

    /// <summary>
    /// Crea o actualiza un material. El callback configura las
    /// propiedades; se aplica igual sobre un material existente, así
    /// cambiar un color en la paleta y volver a correr el builder lo
    /// actualiza sin perder referencias.
    /// </summary>
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
        material.enableInstancing = false; // el SRP Batcher ya agrupa; instancing no suma acá.
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>Guarda el GameObject como prefab (reemplazando el existente) y lo destruye.</summary>
    public static GameObject SavePrefab(GameObject root, string name)
    {
        string path = $"{PrefabsFolder}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static GameObject LoadPrefab(string name) =>
        AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/{name}.prefab");

    public static Shader FindShader(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);

        if (shader == null)
            throw new System.InvalidOperationException(
                $"No se encontró el shader \"{shaderName}\". ¿Está en {ShadersFolder} y compiló sin errores?");

        return shader;
    }

    /// <summary>Hijo con MeshFilter + MeshRenderer (un material por submesh).</summary>
    public static GameObject MeshObject(string name, Transform parent, Mesh mesh, params Material[] materials)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
        SetupRenderer(renderer);
        return go;
    }

    public static GameObject Empty(string name, Transform parent, Vector3 localPosition = default)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go;
    }

    /// <summary>Marca todo como estático (batching, oclusión, probes); sin GI porque no se hornea nada.</summary>
    public static void MarkStatic(GameObject root)
    {
        const StaticEditorFlags flags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
    }

    /// <summary>Configura un renderer para el look de la escena: sin sombras, probes ni motion vectors.</summary>
    public static void SetupRenderer(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
    }
}
