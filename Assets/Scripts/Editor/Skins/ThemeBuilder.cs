using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ThemeBuilder
{
    private const float Horizon = CourtLayout.HorizonRow;
    private const float BelowHorizon = 1120f;

    private sealed class Layer
    {
        public readonly MeshKit Kit = new(24);
        public readonly List<Material> Materials = new();

        public MeshKit With(Material material)
        {
            int index = Materials.IndexOf(material);

            if (index < 0)
            {
                Materials.Add(material);
                index = Materials.Count - 1;
            }

            return Kit.Sub(index);
        }

        public int Triangles => Kit.TriangleCount;

        public void Build(string objectName, Transform parent, string meshName)
        {
            if (Materials.Count == 0)
                return;

            Mesh mesh = SkinAssets.SaveMesh(Kit.ToMesh(meshName, Materials.Count), meshName);
            CourtAssets.MeshObject(objectName, parent, mesh, Materials.ToArray());
        }
    }

    private sealed class Scene
    {
        public readonly Layer Sky = new();
        public readonly Layer Backdrop = new();
        public readonly Layer Glow = new();
        public readonly Layer Ground = new();

        public int Triangles => Sky.Triangles + Backdrop.Triangles + Glow.Triangles + Ground.Triangles;

        public IEnumerable<Material> Materials =>
            Sky.Materials.Concat(Backdrop.Materials).Concat(Glow.Materials).Concat(Ground.Materials).Distinct();
    }

    private sealed class Built
    {
        public GameObject prefab;
        public Material[] materials = Array.Empty<Material>();
        public int triangles;
    }

    public static List<BackgroundTheme> BuildAll(StringBuilder report)
    {
        SkinAssets.EnsureFolders();
        CourtCamera cam = CourtCamera.Solve(out _);

        List<BackgroundTheme> themes = new()
        {
            Save(Plaza(), null),
            Save(Azotea(), Build("Azotea", s => AzoteaScene(s, cam))),
            Save(Playa(), Build("Playa", s => PlayaScene(s, cam))),
            Save(Nevado(), Build("Nevado", s => NevadoScene(s, cam))),
            Save(Desierto(), Build("Desierto", s => DesiertoScene(s, cam))),
            Save(Espacio(), Build("Espacio", s => EspacioScene(s, cam)))
        };

        foreach (BackgroundTheme theme in themes)
        {
            string triangles = theme.environmentPrefab != null
                ? $"{theme.triangleCount:N0} triángulos"
                : "entorno de la escena (sin cambios)";
            report.AppendLine($"  fondo {theme.displayName}: {triangles}");
        }

        return themes;
    }

    private static BackgroundTheme Theme(
        string id, string name, string description, string[] palette, string[] sky,
        string floor, string key, float keyAlpha, UnlockCondition unlock)
    {
        BackgroundTheme theme = ScriptableObject.CreateInstance<BackgroundTheme>();
        theme.id = id;
        theme.displayName = name;
        theme.description = description;
        theme.palette = palette.Select(Hex).ToArray();
        theme.skyColors = sky.Select(Hex).ToArray();
        theme.unlock = unlock;

        Color floorColor = Hex(floor);
        theme.floor = floorColor;
        theme.floorLight = Color.Lerp(floorColor, Color.white, 0.1f);
        theme.floorShadow = Color.Lerp(floorColor, Color.black, 0.06f);
        theme.floorMid = Color.Lerp(floorColor, Color.white, 0.03f);
        theme.crack = Color.Lerp(floorColor, Color.black, 0.18f);
        theme.key = key != null ? SkinAssets.Over(floorColor, Hex(key), keyAlpha) : floorColor;
        return theme;
    }

    private static BackgroundTheme Plaza()
    {
        BackgroundTheme theme = Theme(
            "plaza", "Plaza",
            "La cancha del barrio al atardecer: la reja, dos árboles y el sol cayendo detrás del aro.",
            new[] { "#2B1B5E", "#6A2C8C", "#C9437A", "#F2793A", "#FFD071" },
            new[] { "#2B1B5E", "#6A2C8C", "#C9437A", "#F2793A" },
            "#5F5D7C", null, 0f, UnlockCondition.Default());

        theme.floor = CourtMaterials.Cemento;
        theme.floorLight = CourtMaterials.CementoLuz;
        theme.floorShadow = CourtMaterials.CementoSombra;
        theme.floorMid = CourtMaterials.CementoMedio;
        theme.crack = CourtMaterials.Grieta;
        theme.key = CourtMaterials.Llave;
        return theme;
    }

    private static BackgroundTheme Azotea() => Theme(
        "azotea", "Azotea",
        "Una terraza en el centro, de noche: carteles de neón, tanque de agua y la ciudad prendida.",
        new[] { "#0E1030", "#3A2470", "#F04E8A", "#2EC4E6", "#FFD071" },
        new[] { "#0E1030", "#3A2470" },
        "#3A3550", "#2EC4E6", 0.35f, UnlockCondition.Default());

    private static BackgroundTheme Playa() => Theme(
        "playa", "Playa",
        "Arena caliente, el mar de fondo y una palmera que da sombra al costado de la cancha.",
        new[] { "#3FB7E8", "#BDEBF5", "#1F8FBF", "#F2D49B", "#2F9E4F" },
        new[] { "#3FB7E8", "#BDEBF5" },
        "#F2D49B", "#E3701F", 0.5f, UnlockCondition.Create(UnlockType.Record, 25, "Récord 25"));

    private static BackgroundTheme Nevado() => Theme(
        "nevado", "Nevado",
        "Una cancha helada entre pinos, con las montañas nevadas y el frío de la tarde.",
        new[] { "#1B2A4A", "#7C93C2", "#E8EEF7", "#B7C6DE", "#1E3B4A" },
        new[] { "#1B2A4A", "#7C93C2" },
        "#DCE8F2", "#7C93C2", 1f, UnlockCondition.Create(UnlockType.TotalBaskets, 150, "150 encestadas"));

    private static BackgroundTheme Desierto() => Theme(
        "desierto", "Desierto",
        "Mesetas rojas, cactus y un sol enorme sobre la arena caliente de la tarde.",
        new[] { "#F6A54A", "#FBE2A6", "#C4572E", "#E7B46A", "#2F7D4F" },
        new[] { "#F6A54A", "#FBE2A6" },
        "#E7B46A", "#C4572E", 0.5f, UnlockCondition.Create(UnlockType.Record, 50, "Récord 50"));

    private static BackgroundTheme Espacio() => Theme(
        "espacio", "Espacio",
        "Una cancha en la Luna: cráteres, estrellas y la Tierra mirando desde arriba.",
        new[] { "#0B0820", "#2A1450", "#2E7DE0", "#40D959", "#8A8798" },
        new[] { "#0B0820", "#2A1450" },
        "#8A8798", null, 0f, UnlockCondition.Create(UnlockType.CompleteAllChallenges, 0, "Los 3 desafíos"));

    private static BackgroundTheme Save(BackgroundTheme theme, Built built)
    {
        string[] order = { "plaza", "azotea", "playa", "nevado", "desierto", "espacio" };
        int index = Array.IndexOf(order, theme.id) + 1;
        string path = $"{SkinAssets.ThemesFolder}/{index:00}_{theme.id}.asset";

        BackgroundTheme existing = AssetDatabase.LoadAssetAtPath<BackgroundTheme>(path);
        theme.thumbnail = existing != null ? existing.thumbnail : null;

        if (built != null)
        {
            theme.environmentPrefab = built.prefab;
            theme.environmentMaterials = built.materials;
            theme.triangleCount = built.triangles;
        }

        return SkinAssets.SaveAsset(theme, path);
    }

    private static Built Build(string name, Action<Scene> fill)
    {
        Scene scene = new();
        fill(scene);

        GameObject root = new($"Theme_{name}");
        scene.Sky.Build("Sky", root.transform, $"Theme_{name}_Cielo");
        scene.Backdrop.Build("Backdrop", root.transform, $"Theme_{name}_Fondo");
        scene.Glow.Build("Glow", root.transform, $"Theme_{name}_Emisivo");
        scene.Ground.Build("Ground", root.transform, $"Theme_{name}_Piso");

        Built built = new()
        {
            materials = scene.Materials.ToArray(),
            triangles = scene.Triangles
        };

        CourtAssets.MarkStatic(root);
        built.prefab = SkinAssets.SavePrefab(root, $"Theme_{name}");

        if (built.triangles >= 15000)
            Debug.LogWarning($"[Skins] {name} tiene {built.triangles:N0} triángulos (el límite es 15.000).");

        return built;
    }

    private static void AzoteaScene(Scene s, CourtCamera cam)
    {
        SkyGradient(s, cam, "#0E1030", "#3A2470");
        Stars(s, cam, 46, 11);

        Material moon = SkinAssets.Unlit("M_Theme_Luna", Hex("#F4F1E0"), queue: 2450);
        Disc(s.Glow.With(moon), cam, 820f, 300f, 62f, CourtLayout.SunZ);

        Material building = SkinAssets.Graphic("M_Theme_Azotea_Edificio", Hex("#1A1640"));
        Material window = SkinAssets.Unlit("M_Theme_Azotea_Ventana", Hex("#FFD071"));
        float[,] steps =
        {
            { -300, 0, 800 }, { 0, 140, 820 }, { 140, 240, 760 }, { 240, 380, 860 }, { 380, 520, 800 },
            { 520, 640, 880 }, { 640, 760, 780 }, { 760, 900, 840 }, { 900, 1020, 770 }, { 1020, 1380, 850 }
        };

        const float cityZ = CourtLayout.NearBuildingsZ;
        Skyline(s.Backdrop.With(building), cam, steps, cityZ, 4f);

        System.Random random = new(7);
        MeshKit lit = s.Glow.With(window);

        for (int i = 0; i < steps.GetLength(0); i++)
        {
            for (float row = steps[i, 2] + 26f; row < Horizon - 30f; row += 40f)
            {
                for (float col = steps[i, 0] + 18f; col < steps[i, 1] - 24f; col += 34f)
                {
                    if (random.NextDouble() < 0.3)
                        Rect(lit, cam, col, row, col + 12f, row + 16f, cityZ - 0.03f);
                }
            }
        }

        Material pink = SkinAssets.Unlit("M_Theme_Neon_Rosa", Hex("#F04E8A"));
        Material cyan = SkinAssets.Unlit("M_Theme_Neon_Celeste", Hex("#2EC4E6"));
        RectOutline(s.Glow.With(pink), cam, 156f, 800f, 232f, 836f, 5f, cityZ - 0.05f);
        RectOutline(s.Glow.With(cyan), cam, 660f, 812f, 744f, 846f, 5f, cityZ - 0.05f);

        Material rail = SkinAssets.Graphic("M_Theme_Azotea_Baranda", Hex("#5A5478"));
        MeshKit railKit = s.Backdrop.With(rail);
        const float railZ = CourtLayout.FenceZ;
        const float half = 26f;

        for (float x = -half; x <= half + 0.01f; x += 1.6f)
            railKit.Box(new Vector3(x, 0.55f, railZ), new Vector3(0.08f, 1.1f, 0.08f), BoxFaces.Visible);

        railKit.Box(new Vector3(0f, 1.1f, railZ), new Vector3(half * 2f + 0.1f, 0.09f, 0.09f), BoxFaces.Visible);
        railKit.Box(new Vector3(0f, 0.55f, railZ), new Vector3(half * 2f + 0.1f, 0.05f, 0.05f), BoxFaces.Visible);

        Material tank = SkinAssets.Graphic("M_Theme_Azotea_Tanque", Hex("#2A2140"));
        MeshKit tankKit = s.Backdrop.With(tank);
        Vector3 tankBase = OnGround(cam, 905f, 17f);
        const float radius = 1.1f;

        Prism(tankKit, tankBase + Vector3.up * 1.4f, radius, 1.9f, 8);
        Cone(tankKit, tankBase + Vector3.up * 3.3f, radius * 1.08f, 0.7f, 8);

        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 leg = tankBase + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radius * 0.8f);
            tankKit.Box(leg + Vector3.up * 0.7f, new Vector3(0.12f, 1.4f, 0.12f), BoxFaces.Visible);
        }
    }

    private static void PlayaScene(Scene s, CourtCamera cam)
    {
        SkyGradient(s, cam, "#3FB7E8", "#BDEBF5");

        Material sun = SkinAssets.Unlit("M_Theme_Sol_Claro", Hex("#FFF3C4"), queue: 2450);
        Disc(s.Glow.With(sun), cam, 790f, 640f, 70f, CourtLayout.SunZ);

        const float shore = 30f;
        Material sea = SkinAssets.Graphic("M_Theme_Mar", Hex("#1F8FBF"), decal: 3);
        Material foam = SkinAssets.Graphic("M_Theme_Espuma", Hex("#BDEBF5"), decal: 4);

        s.Ground.With(sea).Quad(new Vector3(-90f, 0.012f, shore), new Vector3(90f, 0.012f, shore),
            new Vector3(90f, 0.012f, 150f), new Vector3(-90f, 0.012f, 150f), Vector3.up);

        MeshKit foamKit = s.Ground.With(foam);
        foamKit.GroundRibbon(new[] { new Vector3(-90f, 0f, shore + 0.45f), new Vector3(90f, 0f, shore + 0.45f) }, 0.9f, 0.016f);
        foamKit.GroundRibbon(new[] { new Vector3(-60f, 0f, 42f), new Vector3(60f, 0f, 42f) }, 0.3f, 0.016f,
            gaps: new[] { new Vector2(0.1f, 0.22f), new Vector2(0.4f, 0.47f), new Vector2(0.7f, 0.85f) });
        foamKit.GroundRibbon(new[] { new Vector3(-80f, 0f, 62f), new Vector3(80f, 0f, 62f) }, 0.45f, 0.016f,
            gaps: new[] { new Vector2(0.05f, 0.2f), new Vector2(0.33f, 0.5f), new Vector2(0.62f, 0.7f), new Vector2(0.82f, 0.95f) });

        Material trunk = SkinAssets.Graphic("M_Theme_Palmera_Tronco", Hex("#8A5A2E"));
        Material leaves = SkinAssets.Graphic("M_Theme_Palmera_Hojas", Hex("#2F9E4F"), doubleSided: true);

        Palm(s.Backdrop.With(trunk), s.Backdrop.With(leaves), OnGround(cam, 140f, 18f), 1f, 1f);
        Palm(s.Backdrop.With(trunk), s.Backdrop.With(leaves), OnGround(cam, 975f, 24f), -1f, 0.8f);
    }

    private static void Palm(MeshKit trunkKit, MeshKit leafKit, Vector3 basePoint, float lean, float scale)
    {
        const int segments = 7;
        float height = 6.2f * scale;
        Vector3 Point(float t) => basePoint + new Vector3(lean * 1.5f * scale * t * t, height * t, 0f);

        for (int i = 0; i < segments; i++)
        {
            Vector3 a = Point(i / (float)segments);
            Vector3 b = Point((i + 1) / (float)segments);
            float width = Mathf.Lerp(0.42f, 0.26f, i / (float)segments) * scale;
            trunkKit.Box((a + b) * 0.5f, new Vector3(width, (b - a).magnitude * 1.05f, width), BoxFaces.Visible,
                Quaternion.FromToRotation(Vector3.up, b - a));
        }

        Vector3 top = Point(1f);
        float[] angles = { -170f, -125f, -80f, -40f, 0f, 40f, 140f };

        foreach (float angle in angles)
        {
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
            dir.z = -0.15f;
            Vector3 tip = top + new Vector3(dir.x, Mathf.Abs(dir.y) * 0.25f - 1.1f, dir.z) * (2.6f * scale);
            Vector3 mid = Vector3.Lerp(top, tip, 0.45f) + Vector3.up * (0.55f * scale);
            Vector3 side = Vector3.Cross(tip - top, Vector3.forward).normalized * (0.32f * scale);

            leafKit.Tri(top, mid + side, mid - side, Vector3.back);
            leafKit.Tri(mid + side, tip, mid - side, Vector3.back);
        }
    }

    private static void NevadoScene(Scene s, CourtCamera cam)
    {
        SkyGradient(s, cam, "#1B2A4A", "#7C93C2");

        Material far = SkinAssets.Graphic("M_Theme_Montana_Fondo", Hex("#D2DCEC"));
        Ridge(s.Backdrop.With(far), cam, CourtLayout.FarBuildingsZ, new[]
        {
            -400f, 860f, -150f, 760f, 60f, 830f, 260f, 720f, 470f, 820f, 700f, 700f, 900f, 800f, 1120f, 730f, 1450f, 850f
        });

        Material snow = SkinAssets.Graphic("M_Theme_Montana", Hex("#E8EEF7"));
        Material snowShadow = SkinAssets.Graphic("M_Theme_Montana_Sombra", Hex("#B7C6DE"));
        const float mountainZ = CourtLayout.NearBuildingsZ;
        (float left, float peakCol, float peakRow, float right)[] mountains =
        {
            (-260f, 140f, 640f, 520f),
            (480f, 820f, 600f, 1240f)
        };

        foreach ((float left, float peakCol, float peakRow, float right) in mountains)
        {
            Vector3 peak = cam.OnPlaneZ(peakCol, peakRow, mountainZ);
            Vector3 baseLeft = cam.OnPlaneZ(left, BelowHorizon, mountainZ);
            Vector3 baseMid = cam.OnPlaneZ(peakCol, BelowHorizon, mountainZ);
            Vector3 baseRight = cam.OnPlaneZ(right, BelowHorizon, mountainZ);
            s.Backdrop.With(snow).Tri(baseLeft, peak, baseMid, Vector3.back);
            s.Backdrop.With(snowShadow).Tri(baseMid, peak, baseRight, Vector3.back);
        }

        Material pine = SkinAssets.Graphic("M_Theme_Pino", Hex("#1E3B4A"));
        MeshKit pineKit = s.Backdrop.With(pine);
        (float col, float z, float height)[] pines =
        {
            (40f, 20f, 4.6f), (150f, 26f, 5.4f), (250f, 19f, 3.6f), (850f, 21f, 4.0f), (960f, 27f, 5.6f), (1060f, 19f, 4.4f)
        };

        foreach ((float col, float z, float height) in pines)
            Pine(pineKit, OnGround(cam, col, z), height);
    }

    private static void Pine(MeshKit kit, Vector3 basePoint, float height)
    {
        float trunk = height * 0.18f;
        kit.Box(basePoint + Vector3.up * (trunk * 0.5f), new Vector3(height * 0.08f, trunk, height * 0.08f), BoxFaces.Visible);

        for (int i = 0; i < 3; i++)
        {
            float bottom = trunk + i * height * 0.24f;
            float top = bottom + height * 0.42f;
            float halfWidth = height * (0.32f - i * 0.07f);
            Vector3 b = basePoint + Vector3.up * bottom;
            kit.Tri(b + Vector3.left * halfWidth, basePoint + Vector3.up * Mathf.Min(top, height), b + Vector3.right * halfWidth, Vector3.back);
        }
    }

    private static void DesiertoScene(Scene s, CourtCamera cam)
    {
        SkyGradient(s, cam, "#F6A54A", "#FBE2A6");

        Material sun = SkinAssets.Unlit("M_Theme_Sol_Claro", Hex("#FFF3C4"), queue: 2450);
        Disc(s.Glow.With(sun), cam, 250f, 720f, 85f, CourtLayout.SunZ);

        Material mesa = SkinAssets.Graphic("M_Theme_Meseta", Hex("#C4572E"));
        Material mesaShadow = SkinAssets.Graphic("M_Theme_Meseta_Sombra", Hex("#A8452A"));

        Mesa(s, mesa, mesaShadow, cam, 90f, 330f, 470f, 930f, 30f, 60f);
        Mesa(s, mesa, mesaShadow, cam, 55f, -150f, 250f, 860f, 90f, 60f);
        Mesa(s, mesa, mesaShadow, cam, 50f, 760f, 1000f, 840f, 70f, 70f);

        Material cactus = SkinAssets.Graphic("M_Theme_Cactus", Hex("#2F7D4F"));
        Cactus(s.Backdrop.With(cactus), OnGround(cam, 120f, 16f), 1f);
        Cactus(s.Backdrop.With(cactus), OnGround(cam, 985f, 22f), 0.75f);
    }

    private static void Mesa(Scene s, Material front, Material side, CourtCamera cam, float z,
        float topLeft, float topRight, float topRow, float slope, float sideWidth)
    {
        Vector3 tl = cam.OnPlaneZ(topLeft, topRow, z);
        Vector3 tr = cam.OnPlaneZ(topRight, topRow, z);
        Vector3 br = cam.OnPlaneZ(topRight + slope, BelowHorizon, z);
        Vector3 bl = cam.OnPlaneZ(topLeft - slope, BelowHorizon, z);
        s.Backdrop.With(front).Quad(bl, br, tr, tl, Vector3.back);

        Vector3 str = cam.OnPlaneZ(topRight + sideWidth, topRow, z + 0.05f);
        Vector3 sbr = cam.OnPlaneZ(topRight + slope + sideWidth, BelowHorizon, z + 0.05f);
        s.Backdrop.With(side).Quad(br, sbr, str, tr, Vector3.back);
    }

    private static void Cactus(MeshKit kit, Vector3 basePoint, float scale)
    {
        float w = 0.45f * scale;
        float h = 3.2f * scale;
        kit.Box(basePoint + Vector3.up * (h * 0.5f), new Vector3(w, h, w), BoxFaces.Visible);

        kit.Box(basePoint + new Vector3(-0.5f * scale, 1.35f * scale, 0f), new Vector3(0.75f * scale, w * 0.8f, w * 0.8f), BoxFaces.Visible);
        kit.Box(basePoint + new Vector3(-0.85f * scale, 1.85f * scale, 0f), new Vector3(w * 0.8f, 1.1f * scale, w * 0.8f), BoxFaces.Visible);
        kit.Box(basePoint + new Vector3(0.45f * scale, 2.0f * scale, 0f), new Vector3(0.65f * scale, w * 0.8f, w * 0.8f), BoxFaces.Visible);
        kit.Box(basePoint + new Vector3(0.75f * scale, 2.4f * scale, 0f), new Vector3(w * 0.8f, 0.9f * scale, w * 0.8f), BoxFaces.Visible);
    }

    private static void EspacioScene(Scene s, CourtCamera cam)
    {
        SkyGradient(s, cam, "#0B0820", "#2A1450");
        Stars(s, cam, 90, 23);

        Material earth = SkinAssets.Unlit("M_Theme_Tierra", Hex("#2E7DE0"), queue: 2450);
        Material land = SkinAssets.Unlit("M_Theme_Continente", Hex("#40D959"), queue: 2450);
        const float earthCol = 820f, earthRow = 400f, earthRadius = 115f;
        Disc(s.Glow.With(earth), cam, earthCol, earthRow, earthRadius, CourtLayout.SunZ);

        float[][] continents =
        {
            new[] { -62f, -40f, -22f, -72f, 12f, -48f, -4f, -6f, -50f, 2f },
            new[] { 22f, 10f, 72f, -2f, 84f, 38f, 44f, 72f, 16f, 46f },
            new[] { -72f, 40f, -42f, 30f, -34f, 62f, -60f, 74f }
        };

        foreach (float[] shape in continents)
        {
            List<Vector3> points = new();
            for (int i = 0; i < shape.Length; i += 2)
                points.Add(cam.OnPlaneZ(earthCol + shape[i], earthRow + shape[i + 1], CourtLayout.SunZ - 0.5f));
            s.Glow.With(land).Fan(points, Vector3.back);
        }

        Material crater = SkinAssets.Graphic("M_Theme_Crater", Hex("#6E6B80"), decal: 3);
        (float x, float z, float r)[] craters =
        {
            (-4.8f, 3f, 0.9f), (5.4f, 6f, 1.2f), (-6.8f, 14f, 1.6f), (7.2f, 18f, 1.4f), (-3f, 24f, 2.2f),
            (4f, 34f, 2.8f), (-9.5f, 40f, 3.5f), (12f, 52f, 4f), (-14f, 26f, 2f), (16f, 30f, 2.6f)
        };

        foreach ((float x, float z, float r) in craters)
            GroundDisc(s.Ground.With(crater), new Vector3(x, 0.006f, z), r, 18);
    }

    private static void SkyGradient(Scene s, CourtCamera cam, string top, string bottom)
    {
        Color a = Hex(top);
        Color b = Hex(bottom);
        Material sky = SkinAssets.Unlit("M_Theme_Cielo", Color.white, vertexColor: true, queue: 2450);
        MeshKit kit = s.Sky.With(sky);

        List<(float row, Color color)> rows = new() { (-900f, a) };
        const int steps = 6;

        for (int i = 0; i <= steps; i++)
            rows.Add((Horizon * i / steps, Color.Lerp(a, b, i / (float)steps)));

        rows.Add((1700f, b));

        const float left = -600f;
        const float right = CourtLayout.MockWidth + 600f;

        for (int i = 0; i < rows.Count - 1; i++)
        {
            (float r0, Color c0) = rows[i];
            (float r1, Color c1) = rows[i + 1];
            kit.QuadColors(
                cam.OnPlaneZ(left, r0, CourtLayout.SkyZ), cam.OnPlaneZ(right, r0, CourtLayout.SkyZ),
                cam.OnPlaneZ(right, r1, CourtLayout.SkyZ), cam.OnPlaneZ(left, r1, CourtLayout.SkyZ),
                c0, c0, c1, c1, Vector3.back);
        }
    }

    private static void Stars(Scene s, CourtCamera cam, int count, int seed)
    {
        Material star = SkinAssets.Unlit("M_Theme_Estrella", Hex("#FFFFFF"), queue: 2450);
        MeshKit kit = s.Glow.With(star);
        System.Random random = new(seed);
        const float z = CourtLayout.CloudsZ;

        for (int i = 0; i < count; i++)
        {
            float col = (float)(random.NextDouble() * 1480.0 - 200.0);
            float row = (float)(random.NextDouble() * 1400.0 - 600.0);
            float size = (float)(2.5 + random.NextDouble() * 4.0);

            kit.Quad(
                cam.OnPlaneZ(col, row - size, z), cam.OnPlaneZ(col + size, row, z),
                cam.OnPlaneZ(col, row + size, z), cam.OnPlaneZ(col - size, row, z), Vector3.back);
        }
    }

    private static void Skyline(MeshKit kit, CourtCamera cam, float[,] steps, float z, float depth)
    {
        for (int i = 0; i < steps.GetLength(0); i++)
        {
            Vector3 a = cam.OnPlaneZ(steps[i, 0], steps[i, 2], z);
            Vector3 b = cam.OnPlaneZ(steps[i, 1], steps[i, 2], z);
            const float bottom = -1f;
            kit.Box(
                new Vector3((a.x + b.x) * 0.5f, (a.y + bottom) * 0.5f, z + depth * 0.5f),
                new Vector3(Mathf.Abs(b.x - a.x), a.y - bottom, depth),
                BoxFaces.Visible);
        }
    }

    private static void Ridge(MeshKit kit, CourtCamera cam, float z, float[] colRow)
    {
        for (int i = 0; i + 3 < colRow.Length; i += 2)
        {
            Vector3 a = cam.OnPlaneZ(colRow[i], colRow[i + 1], z);
            Vector3 b = cam.OnPlaneZ(colRow[i + 2], colRow[i + 3], z);
            Vector3 bb = cam.OnPlaneZ(colRow[i + 2], BelowHorizon, z);
            Vector3 ab = cam.OnPlaneZ(colRow[i], BelowHorizon, z);
            kit.Quad(ab, bb, b, a, Vector3.back);
        }
    }

    private static void Disc(MeshKit kit, CourtCamera cam, float col, float row, float radiusPx, float z, int segments = 24)
    {
        Vector3 center = cam.OnPlaneZ(col, row, z);
        float radius = (cam.OnPlaneZ(col + radiusPx, row, z) - center).magnitude;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            kit.Tri(center,
                center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius,
                center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius,
                Vector3.back);
        }
    }

    private static void GroundDisc(MeshKit kit, Vector3 center, float radius, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            kit.Tri(center,
                center + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius,
                center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius,
                Vector3.up);
        }
    }

    private static void Rect(MeshKit kit, CourtCamera cam, float col0, float row0, float col1, float row1, float z)
    {
        kit.Quad(cam.OnPlaneZ(col0, row1, z), cam.OnPlaneZ(col1, row1, z),
            cam.OnPlaneZ(col1, row0, z), cam.OnPlaneZ(col0, row0, z), Vector3.back);
    }

    private static void RectOutline(MeshKit kit, CourtCamera cam, float col0, float row0, float col1, float row1, float width, float z)
    {
        Rect(kit, cam, col0, row0, col1, row0 + width, z);
        Rect(kit, cam, col0, row1 - width, col1, row1, z);
        Rect(kit, cam, col0, row0, col0 + width, row1, z);
        Rect(kit, cam, col1 - width, row0, col1, row1, z);
    }

    private static void Prism(MeshKit kit, Vector3 bottom, float radius, float height, int sides)
    {
        Vector3 center = bottom + Vector3.up * (height * 0.5f);
        List<Vector3> top = new();

        for (int i = 0; i < sides; i++)
        {
            float a0 = i / (float)sides * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
            Vector3 d0 = new(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
            Vector3 d1 = new(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
            kit.SolidQuad(bottom + d0, bottom + d1, bottom + d1 + Vector3.up * height, bottom + d0 + Vector3.up * height, center);
            top.Add(bottom + d0 + Vector3.up * height);
        }

        kit.Fan(top, Vector3.up);
    }

    private static void Cone(MeshKit kit, Vector3 bottom, float radius, float height, int sides)
    {
        Vector3 apex = bottom + Vector3.up * height;

        for (int i = 0; i < sides; i++)
        {
            float a0 = i / (float)sides * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
            Vector3 p0 = bottom + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
            Vector3 p1 = bottom + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
            Vector3 facing = ((p0 + p1) * 0.5f - bottom) + Vector3.up * 0.3f;
            kit.Tri(p0, p1, apex, facing);
        }
    }

    private static Vector3 OnGround(CourtCamera cam, float col, float z)
    {
        Vector3 p = cam.OnPlaneZ(col, Horizon, z);
        p.y = 0f;
        return p;
    }

    private static Color Hex(string hex) =>
        CourtMaterials.Hex(hex);
}
