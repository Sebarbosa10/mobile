using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BallSkinBuilder
{
    private const float Radius = 0.5f;
    private const float Lift = Radius * 1.012f;
    private const float OutlineWidth = 0.022f;

    private static readonly Color Naranja = Hex("#F28A1E");
    private static readonly Color Celeste = Hex("#2EC4E6");
    private static readonly Color Rosa = Hex("#F04E8A");
    private static readonly Color Amarillo = Hex("#F2D21E");
    private static readonly Color Verde = Hex("#40D959");
    private static readonly Color Noche = Hex("#1E1638");

    private sealed class Built
    {
        public GameObject prefab;
        public Mesh mesh;
        public Material[] materials;
        public BallVariant[] variants;
    }

    public static List<BallSkin> BuildAll(StringBuilder report)
    {
        SkinAssets.EnsureFolders();

        List<Vector3[]> faces = BallGeometry.Icosphere(2);
        Mesh outline = SkinAssets.SaveMesh(OutlineMesh(faces), "Skin_Ball_Outline");

        List<BallSkin> skins = new()
        {
            Save("clasica", "Clásica", 1, Clasica(), UnlockCondition.Default()),
            Save("playera", "Playera", 2, Playera(outline), UnlockCondition.Default()),
            Save("tenis", "Tenis", 3, Tenis(faces, outline), UnlockCondition.Default()),
            Save("sandia", "Sandía", 4, Sandia(faces, outline), UnlockCondition.Create(UnlockType.Record, 15, "Récord 15")),
            Save("pixel", "Pixel", 5, Pixel(faces, outline), UnlockCondition.Create(UnlockType.TotalBaskets, 60, "60 encestadas")),
            Save("neon", "Neón", 6, Neon(faces, outline), UnlockCondition.Create(UnlockType.CompleteChallenge, 1, "Completá Carrusel")),
            Save("lava", "Lava", 7, Lava(faces, outline), UnlockCondition.Create(UnlockType.Record, 30, "Récord 30")),
            Save("planeta", "Planeta", 8, Planeta(faces, outline), UnlockCondition.Create(UnlockType.CompleteChallenge, 3, "Completá Contrarreloj")),
            Save("disco", "Disco", 9, Disco(faces, outline), UnlockCondition.Create(UnlockType.CompleteAllChallenges, 0, "Los 3 desafíos"))
        };

        foreach (BallSkin skin in skins.Where(s => s.prefab != null))
            report.AppendLine($"  pelota {skin.displayName}: {Triangles(skin.prefab):N0} triángulos");

        return skins;
    }

    private static BallSkin Save(string id, string displayName, int order, Built built, UnlockCondition unlock)
    {
        string path = $"{SkinAssets.BallsFolder}/{order:00}_{id}.asset";
        BallSkin existing = AssetDatabase.LoadAssetAtPath<BallSkin>(path);

        BallSkin skin = ScriptableObject.CreateInstance<BallSkin>();
        skin.id = id;
        skin.displayName = displayName;
        skin.prefab = built?.prefab;
        skin.mesh = built?.mesh;
        skin.materials = built?.materials ?? Array.Empty<Material>();
        skin.variants = built?.variants ?? Array.Empty<BallVariant>();
        skin.unlock = unlock;
        skin.icon = existing != null ? existing.icon : null;

        return SkinAssets.SaveAsset(skin, path);
    }

    private static Built Clasica()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CourtAssets.PrefabsFolder}/Ball.prefab");

        if (prefab == null)
        {
            Debug.LogError("[Skins] Falta Assets/Art/Scene/Prefabs/Ball.prefab: corré Tools > Build Court Scene.");
            return null;
        }

        Renderer body = prefab.transform.Find("Body")?.GetComponent<Renderer>();

        return new Built
        {
            prefab = prefab,
            mesh = body != null ? body.GetComponent<MeshFilter>().sharedMesh : null,
            materials = body != null ? body.sharedMaterials : Array.Empty<Material>(),
            variants = Variants(new[] { Naranja, Celeste, Rosa, Amarillo })
        };
    }

    private static Built Playera(Mesh outline)
    {
        MeshKit body = new(3);

        foreach (Vector3[] f in BallGeometry.Icosphere(3))
        {
            Vector3 c = (f[0] + f[1] + f[2]).normalized;
            float azimuth = Mathf.Atan2(c.x, -c.z) * Mathf.Rad2Deg;
            int sub = azimuth < -18f && azimuth > -120f ? 1 : azimuth > 18f && azimuth < 120f ? 2 : 0;
            body.Sub(sub).Tri(f[0] * Radius, f[1] * Radius, f[2] * Radius, c);
        }

        Material[] mats =
        {
            BallMaterial("M_Skin_Playera_Base", Hex("#FFFFFF")),
            BallMaterial("M_Skin_Playera_Izq", Hex("#E63333")),
            BallMaterial("M_Skin_Playera_Der", Hex("#2E7DE0"))
        };

        Color[] left = { Hex("#E63333"), Verde, Rosa, Hex("#E3701F") };
        Color[] right = { Hex("#2E7DE0"), Amarillo, Celeste, Hex("#6A2C8C") };

        return Assemble("Playera", body, mats, outline, Noche, 1, left, 2, right);
    }

    private static Built Tenis(List<Vector3[]> faces, Mesh outline)
    {
        MeshKit body = new(2);
        Faceted(body, faces, _ => 0);

        body.Sub(1);
        const float a = 0.72f, b = 0.28f;
        float c = 2f * Mathf.Sqrt(a * b);
        Ribbon(body, Curve(t => new Vector3(
            a * Mathf.Cos(t) + b * Mathf.Cos(3f * t),
            a * Mathf.Sin(t) - b * Mathf.Sin(3f * t),
            c * Mathf.Sin(2f * t)), 64, Quaternion.Euler(0f, 0f, 90f)), 0.028f, closed: true);

        Color main = Hex("#D7E84A");
        Material[] mats = { BallMaterial("M_Skin_Tenis", main), SkinAssets.Graphic("M_Skin_Tenis_Costura", Hex("#FFFFFF")) };

        return Assemble("Tenis", body, mats, outline, Noche, 0, ShakeColors(main, Hex("#FFFFFF")));
    }

    private static Built Sandia(List<Vector3[]> faces, Mesh outline)
    {
        MeshKit body = new(2);
        Faceted(body, faces, _ => 0);

        body.Sub(1);
        Quaternion tilt = Quaternion.Euler(12f, 0f, 0f);

        for (int k = 0; k < 4; k++)
        {
            float yaw = k * 45f;
            Ribbon(body, Curve(t => Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, Mathf.Cos(t), Mathf.Sin(t)), 40, tilt),
                0.045f, closed: true);
        }

        Color main = Hex("#4DB860");
        Color stripe = Hex("#1E6B34");
        Material[] mats = { BallMaterial("M_Skin_Sandia", main), SkinAssets.Graphic("M_Skin_Sandia_Franja", stripe) };

        return Assemble("Sandia", body, mats, outline, Noche, 0, ShakeColors(main, stripe));
    }

    private static Built Pixel(List<Vector3[]> faces, Mesh outline)
    {
        MeshKit body = new();

        foreach (Vector3[] f in faces)
        {
            Vector2[] uv = SphereUVs(f);
            Vector3 c = f[0] + f[1] + f[2];
            body.TriUV(f[0] * Radius, f[1] * Radius, f[2] * Radius, uv[0], uv[1], uv[2], c);
        }

        Color main = Hex("#3269C2");
        Color pattern = Hex("#2EC4E6");
        Texture2D mask = PixelMask();

        Material mat = BallMaterial("M_Skin_Pixel", main);
        SkinAssets.SetKeyword(mat, "_PATTERN_MAP", "_UsePatternMap", true);
        mat.SetTexture("_PatternMap", mask);
        mat.SetColor("_PatternColor", pattern);

        return Assemble("Pixel", body, new[] { mat }, outline, Noche, 0, ShakeColors(main, pattern));
    }

    private static Built Neon(List<Vector3[]> faces, Mesh outline)
    {
        MeshKit body = new(2);
        Faceted(body, faces, _ => 0);

        body.Sub(1);
        AddBasketSeams(body, 0.023f);

        Color tube = Hex("#2EC4E6");
        Color contour = Hex("#F04E8A");
        Material[] mats = { BallMaterial("M_Skin_Neon_Base", Noche), SkinAssets.Unlit("M_Skin_Neon_Tubo", tube) };

        return Assemble("Neon", body, mats, outline, contour, 1, ShakeColors(tube, contour, Noche), outlineWidth: 0.03f);
    }

    private static Built Lava(List<Vector3[]> faces, Mesh outline)
    {
        MeshKit body = new(2);
        Faceted(body, faces, _ => 0);

        body.Sub(1);
        float[][] cracks =
        {
            new[] { 20f, -80f, 10f, -55f, 18f, -35f, 5f, -15f, -8f, 0f, -2f, 20f, -15f, 40f, -10f, 62f, -20f, 85f },
            new[] { -8f, 0f, -30f, -5f, -45f, 12f, -62f, 5f },
            new[] { 18f, -35f, 40f, -30f, 52f, -10f, 66f, -22f },
            new[] { -2f, 20f, 15f, 35f, 30f, 30f, 44f, 55f },
            new[] { 10f, 110f, -5f, 140f, 8f, 165f, -10f, 190f, 5f, 215f, -12f, 245f, 6f, 275f },
            new[] { -40f, 100f, -55f, 130f, -48f, 160f },
            new[] { 45f, 150f, 60f, 185f, 50f, 220f }
        };

        foreach (float[] crack in cracks)
        {
            List<Vector3> points = new();
            for (int i = 0; i < crack.Length; i += 2)
                points.Add(LatLon(crack[i], crack[i + 1]));

            Ribbon(body, Subdivide(points, 3), 0.022f, closed: false);
        }

        Color glow = Hex("#FF7A1A");
        Color rock = Hex("#3A1A22");
        Material[] mats = { BallMaterial("M_Skin_Lava_Roca", rock), SkinAssets.Unlit("M_Skin_Lava_Grieta", glow) };

        return Assemble("Lava", body, mats, outline, Noche, 1, ShakeColors(glow, rock));
    }

    private static Built Planeta(List<Vector3[]> faces, Mesh outline)
    {
        MeshKit body = new(2);
        Faceted(body, faces, _ => 0);

        body.Sub(1);
        Quaternion tilt = Quaternion.Euler(0f, 0f, -15f);

        foreach (float lat in new[] { 16f, -13f })
            Ribbon(body, Curve(t => LatLon(lat, t * Mathf.Rad2Deg), 40, tilt), 0.04f, closed: true);

        Color main = Hex("#7B5CE0");
        Color band = Hex("#5A3FB8");
        Color ringColor = Hex("#FFD071");
        Material[] mats = { BallMaterial("M_Skin_Planeta", main), SkinAssets.Graphic("M_Skin_Planeta_Banda", band) };

        Built built = Assemble("Planeta", body, mats, outline, Noche, 0, ShakeColors(main, band, ringColor), save: false);
        GameObject root = built.prefab;

        MeshKit ringKit = new();
        const int segments = 48;
        const float inner = 0.665f, outer = 0.737f;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            Vector3 d0 = new(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
            Vector3 d1 = new(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
            ringKit.Quad(d0 * inner, d1 * inner, d1 * outer, d0 * outer, Vector3.up);
        }

        GameObject ring = CourtAssets.MeshObject("Ring", root.transform,
            SkinAssets.SaveMesh(ringKit.ToMesh("Ring"), "Skin_Planeta_Anillo"),
            SkinAssets.Graphic("M_Skin_Planeta_Anillo", ringColor, doubleSided: true));
        ring.transform.localRotation = Quaternion.Euler(-13f, 0f, -15f);

        built.prefab = SkinAssets.SavePrefab(root, "Ball_Planeta");
        return built;
    }

    private static Built Disco(List<Vector3[]> faces, Mesh outline)
    {
        MeshKit body = new(2);
        Faceted(body, faces, _ => 0);

        body.Sub(1);
        foreach (Vector3[] f in faces)
        {
            Vector3 center = (f[0] + f[1] + f[2]) / 3f;
            Vector3 P(Vector3 v) => (center + (v - center) * 0.8f) * Radius * 1.006f;
            body.Tri(P(f[0]), P(f[1]), P(f[2]), center);
        }

        Color mirror = Hex("#C9D2E0");
        Color joint = Hex("#8E9AB0");
        Material[] mats = { BallMaterial("M_Skin_Disco_Junta", joint), BallMaterial("M_Skin_Disco_Espejo", mirror) };

        Built built = Assemble("Disco", body, mats, outline, Noche, 1, ShakeColors(mirror, joint), save: false);
        GameObject root = built.prefab;

        Mesh star = SkinAssets.SaveMesh(StarMesh().ToMesh("Star"), "Skin_Disco_Destello");
        Material sparkleMat = SkinAssets.Additive("M_Skin_Disco_Destello", Hex("#FFFFFF"));

        GameObject holder = CourtAssets.Empty("Sparkles", root.transform);
        (float lat, float lon, float size)[] spots = { (32f, -38f, 0.16f), (-12f, 42f, 0.12f), (55f, 28f, 0.1f), (-42f, -22f, 0.13f) };
        List<Transform> sparkles = new();

        foreach ((float lat, float lon, float size) in spots)
        {
            GameObject s = CourtAssets.MeshObject("Sparkle", holder.transform, star, sparkleMat);
            s.transform.localPosition = LatLon(lat, lon) * 0.53f;
            s.transform.localScale = Vector3.one * size;
            sparkles.Add(s.transform);
        }

        BallSparkles component = holder.AddComponent<BallSparkles>();
        SerializedObject so = new(component);
        SerializedProperty list = so.FindProperty("sparkles");
        list.arraySize = sparkles.Count;
        for (int i = 0; i < sparkles.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = sparkles[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        built.prefab = SkinAssets.SavePrefab(root, "Ball_Disco");
        return built;
    }

    private static Material BallMaterial(string name, Color color) =>
        SkinAssets.Toon3D(name, color, rim: true, shadowStrength: 0.18f, threshold: -0.1f);

    private static Built Assemble(
        string name,
        MeshKit body,
        Material[] materials,
        Mesh outline,
        Color contour,
        int slot0,
        Color[] colors0,
        int slot1 = -1,
        Color[] colors1 = null,
        bool save = true,
        float outlineWidth = OutlineWidth)
    {
        Mesh bodyMesh = SkinAssets.SaveMesh(body.ToMesh(name), $"Skin_{name}");

        GameObject root = new($"Ball_{name}");
        GameObject bodyObject = CourtAssets.MeshObject("Body", root.transform, bodyMesh, materials);
        CourtAssets.MeshObject("Outline", root.transform, outline,
            SkinAssets.Outline($"M_Skin_{name}_Contorno", contour, outlineWidth));

        Renderer renderer = bodyObject.GetComponent<Renderer>();
        BallColor ballColor = root.AddComponent<BallColor>();
        SerializedObject so = new(ballColor);
        so.FindProperty("bodyRenderer").objectReferenceValue = renderer;
        so.FindProperty("bodyMaterialIndex").intValue = slot0;
        so.FindProperty("popTarget").objectReferenceValue = root.transform;
        SetColors(so.FindProperty("colors"), colors0);

        SerializedProperty extras = so.FindProperty("extraTints");
        extras.arraySize = slot1 >= 0 ? 1 : 0;

        if (slot1 >= 0)
        {
            SerializedProperty extra = extras.GetArrayElementAtIndex(0);
            extra.FindPropertyRelative("renderer").objectReferenceValue = renderer;
            extra.FindPropertyRelative("materialIndex").intValue = slot1;
            SetColors(extra.FindPropertyRelative("colors"), colors1);
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        return new Built
        {
            prefab = save ? SkinAssets.SavePrefab(root, $"Ball_{name}") : root,
            mesh = bodyMesh,
            materials = materials,
            variants = colors1 != null ? Variants(colors0, colors1) : Variants(colors0)
        };
    }

    private static void SetColors(SerializedProperty property, Color[] colors)
    {
        property.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
            property.GetArrayElementAtIndex(i).colorValue = colors[i];
    }

    private static BallVariant[] Variants(Color[] primary, Color[] secondary = null)
    {
        BallVariant[] variants = new BallVariant[primary.Length];

        for (int i = 0; i < primary.Length; i++)
            variants[i] = secondary != null ? new BallVariant(primary[i], secondary[i]) : new BallVariant(primary[i]);

        return variants;
    }

    private static Color[] ShakeColors(Color own, params Color[] pattern)
    {
        List<Color> result = new() { own };

        foreach (Color candidate in new[] { Celeste, Rosa, Amarillo, Naranja, Verde })
        {
            if (result.Count == 4)
                break;

            if (Similar(candidate, own) || pattern.Any(p => Similar(candidate, p)))
                continue;

            result.Add(candidate);
        }

        return result.ToArray();
    }

    private static bool Similar(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.15f;

    private static void Faceted(MeshKit kit, List<Vector3[]> faces, Func<Vector3, int> submeshOf)
    {
        foreach (Vector3[] f in faces)
        {
            Vector3 c = f[0] + f[1] + f[2];
            kit.Sub(submeshOf(c.normalized)).Tri(f[0] * Radius, f[1] * Radius, f[2] * Radius, c);
        }
    }

    private static Mesh OutlineMesh(List<Vector3[]> faces)
    {
        MeshKit outline = new();
        Dictionary<Vector3Int, int> shared = new();

        int Shared(Vector3 p)
        {
            Vector3Int key = Vector3Int.RoundToInt(p * 10000f);
            if (!shared.TryGetValue(key, out int index))
            {
                index = outline.Vertex(p * Radius, p);
                shared[key] = index;
            }
            return index;
        }

        foreach (Vector3[] f in faces)
        {
            int a = Shared(f[0]), b = Shared(f[1]), c = Shared(f[2]);
            if (Vector3.Dot(Vector3.Cross(f[1] - f[0], f[2] - f[0]), f[0]) >= 0f)
                outline.Index(a, b, c);
            else
                outline.Index(a, c, b);
        }

        return outline.ToMesh("BallOutline");
    }

    private static void AddBasketSeams(MeshKit kit, float halfWidth)
    {
        Quaternion orient = Quaternion.Euler(20f, 35f, 0f);
        const float side = 0.62f;
        float sideRadius = Mathf.Sqrt(1f - side * side);

        Func<float, Vector3>[] curves =
        {
            t => new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)),
            t => new Vector3(0f, Mathf.Cos(t), Mathf.Sin(t)),
            t => new Vector3(side, Mathf.Cos(t) * sideRadius, Mathf.Sin(t) * sideRadius),
            t => new Vector3(-side, Mathf.Cos(t) * sideRadius, Mathf.Sin(t) * sideRadius)
        };

        foreach (Func<float, Vector3> curve in curves)
            Ribbon(kit, Curve(curve, 32, orient), halfWidth, closed: true);
    }

    private static List<Vector3> Curve(Func<float, Vector3> f, int segments, Quaternion orient)
    {
        List<Vector3> points = new();
        for (int i = 0; i < segments; i++)
            points.Add(orient * f(i / (float)segments * Mathf.PI * 2f));
        return points;
    }

    private static void Ribbon(MeshKit kit, IReadOnlyList<Vector3> points, float halfWidth, bool closed)
    {
        int count = closed ? points.Count : points.Count - 1;

        for (int i = 0; i < count; i++)
        {
            Vector3 p0 = points[i].normalized;
            Vector3 p1 = points[(i + 1) % points.Count].normalized;
            Vector3 outward = (p0 + p1).normalized;
            Vector3 across = Vector3.Cross(p1 - p0, outward).normalized * halfWidth;
            Vector3 a = p0 * Lift;
            Vector3 b = p1 * Lift;
            kit.Quad(a - across, b - across, b + across, a + across, outward);
        }
    }

    private static List<Vector3> Subdivide(List<Vector3> points, int steps)
    {
        List<Vector3> result = new();

        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int s = 0; s < steps; s++)
                result.Add(Vector3.Slerp(points[i], points[i + 1], s / (float)steps));
        }

        result.Add(points[points.Count - 1]);
        return result;
    }

    private static Vector3 LatLon(float latitude, float longitude)
    {
        float lat = latitude * Mathf.Deg2Rad;
        float lon = longitude * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), -Mathf.Cos(lat) * Mathf.Cos(lon));
    }

    private static Vector2[] SphereUVs(Vector3[] f)
    {
        Vector2[] uv = new Vector2[3];
        bool[] pole = new bool[3];

        for (int i = 0; i < 3; i++)
        {
            Vector3 p = f[i];
            pole[i] = Mathf.Abs(p.y) > 0.999f;
            uv[i] = new Vector2(
                0.5f + Mathf.Atan2(p.x, -p.z) / (2f * Mathf.PI),
                0.5f + Mathf.Asin(Mathf.Clamp(p.y, -1f, 1f)) / Mathf.PI);
        }

        float max = Mathf.Max(uv[0].x, uv[1].x, uv[2].x);
        float min = Mathf.Min(uv[0].x, uv[1].x, uv[2].x);

        if (max - min > 0.5f)
        {
            for (int i = 0; i < 3; i++)
                if (uv[i].x < 0.5f)
                    uv[i].x += 1f;
        }

        for (int i = 0; i < 3; i++)
        {
            if (!pole[i])
                continue;

            float sum = 0f;
            int count = 0;
            for (int j = 0; j < 3; j++)
            {
                if (j == i || pole[j]) continue;
                sum += uv[j].x;
                count++;
            }

            if (count > 0)
                uv[i].x = sum / count;
        }

        return uv;
    }

    private static Texture2D PixelMask()
    {
        string path = $"{SkinAssets.TexturesFolder}/T_Skin_PixelMask.png";

        Texture2D texture = new(8, 8, TextureFormat.RGBA32, false);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                texture.SetPixel(x, y, (x + y) % 2 == 0 ? Color.black : Color.white);

        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static MeshKit StarMesh()
    {
        MeshKit kit = new();
        const int points = 8;

        for (int i = 0; i < points; i++)
        {
            float a0 = i / (float)points * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)points * Mathf.PI * 2f;
            float r0 = i % 2 == 0 ? 1f : 0.22f;
            float r1 = i % 2 == 0 ? 0.22f : 1f;
            kit.Tri(Vector3.zero,
                new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, 0f),
                new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, 0f),
                Vector3.back);
        }

        return kit;
    }

    private static int Triangles(GameObject prefab)
    {
        int count = 0;
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            for (int i = 0; i < mesh.subMeshCount; i++)
                count += (int)mesh.GetIndexCount(i) / 3;
        }
        return count;
    }

    private static Color Hex(string hex) =>
        CourtMaterials.Hex(hex);
}
