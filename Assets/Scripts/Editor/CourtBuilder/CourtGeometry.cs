using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escenario estático: cielo, cancha, reja, edificios y árboles. Todo
/// se arma con meshes low-poly propios, se guarda como .asset y cada
/// pieza queda en su prefab (Sky, Court, Fence, Buildings, Tree) para
/// poder reemplazarla después por un modelo de Blender.
///
/// Las posiciones del fondo salen del mockup: se toman sus coordenadas
/// en píxeles y se desproyectan con la cámara resuelta sobre el plano de
/// profundidad de cada capa.
/// </summary>
public static class CourtGeometry
{
    private static readonly Vector3 TowardCamera = Vector3.back;

    // ---------------------------------------------------------------- Cielo

    public static GameObject BuildSky(CourtCamera cam, CourtMaterials m)
    {
        GameObject root = new("Sky");

        // Degradé de 4 paradas (0%, 38%, 72%, 100% de la franja de cielo
        // del mockup, que va de la fila 0 al horizonte). Entre paradas se
        // agregan filas intermedias interpoladas en sRGB, así el degradé se
        // ve como en el mockup y no como una interpolación lineal.
        (float row, Color color)[] stops =
        {
            (-900f, CourtMaterials.CieloCenit), // extensión para pantallas 9:20 / 9:21
            (0f, CourtMaterials.CieloCenit),
            (CourtLayout.HorizonRow * 0.38f, CourtMaterials.CieloVioleta),
            (CourtLayout.HorizonRow * 0.72f, CourtMaterials.CieloRosa),
            (CourtLayout.HorizonRow, CourtMaterials.CieloHorizonte),
            (1700f, CourtMaterials.CieloHorizonte) // por debajo del horizonte (tapado por el fondo)
        };

        List<(float row, Color color)> rows = new();

        for (int i = 0; i < stops.Length - 1; i++)
        {
            int steps = stops[i].color == stops[i + 1].color ? 1 : 4;

            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                rows.Add((Mathf.Lerp(stops[i].row, stops[i + 1].row, t), Color.Lerp(stops[i].color, stops[i + 1].color, t)));
            }
        }

        rows.Add(stops[stops.Length - 1]);

        MeshKit sky = new();
        const float left = -600f;
        const float right = CourtLayout.MockWidth + 600f;

        for (int i = 0; i < rows.Count - 1; i++)
        {
            (float r0, Color c0) = rows[i];
            (float r1, Color c1) = rows[i + 1];
            sky.QuadColors(
                cam.OnPlaneZ(left, r0, CourtLayout.SkyZ),
                cam.OnPlaneZ(right, r0, CourtLayout.SkyZ),
                cam.OnPlaneZ(right, r1, CourtLayout.SkyZ),
                cam.OnPlaneZ(left, r1, CourtLayout.SkyZ),
                c0, c0, c1, c1,
                TowardCamera);
        }

        CourtAssets.MeshObject("Gradient", root.transform, CourtAssets.SaveMesh(sky.ToMesh("Sky"), "Sky_Gradient"), m.MCielo);

        // Sol: disco plano bajo, a la izquierda del aro, con halo detrás.
        Mesh disc = CourtAssets.SaveMesh(Disc(24, Vector3.back).ToMesh("Disc"), "Disc24");

        Vector3 sunCenter = cam.OnPlaneZ(250f, 940f, CourtLayout.SunZ);
        float sunRadius = (cam.OnPlaneZ(380f, 940f, CourtLayout.SunZ) - sunCenter).magnitude;
        GameObject sun = CourtAssets.MeshObject("Sun", root.transform, disc, m.MSol);
        sun.transform.SetLocalPositionAndRotation(sunCenter, Quaternion.identity);
        sun.transform.localScale = Vector3.one * sunRadius;

        float haloZ = CourtLayout.SunZ + 1f;
        Vector3 haloCenter = cam.OnPlaneZ(250f, 940f, haloZ);
        float haloRadius = (cam.OnPlaneZ(440f, 940f, haloZ) - haloCenter).magnitude;
        GameObject halo = CourtAssets.MeshObject("SunHalo", root.transform, disc, m.MSolHalo);
        halo.transform.SetLocalPositionAndRotation(haloCenter, Quaternion.identity);
        halo.transform.localScale = Vector3.one * haloRadius;

        // Nubes: franjas planas alargadas (paralelogramos del mockup).
        MeshKit clouds = new(3);
        float[][] cloudShapes =
        {
            new[] { 620f, 470f, 900f, 470f, 960f, 492f, 700f, 492f },
            new[] { 60f, 610f, 330f, 610f, 380f, 630f, 120f, 630f },
            new[] { 700f, 720f, 1300f, 720f, 1300f, 744f, 760f, 744f }
        };

        for (int i = 0; i < cloudShapes.Length; i++)
        {
            float[] p = cloudShapes[i];
            clouds.Sub(i).Quad(
                cam.OnPlaneZ(p[0], p[1], CourtLayout.CloudsZ),
                cam.OnPlaneZ(p[2], p[3], CourtLayout.CloudsZ),
                cam.OnPlaneZ(p[4], p[5], CourtLayout.CloudsZ),
                cam.OnPlaneZ(p[6], p[7], CourtLayout.CloudsZ),
                TowardCamera);
        }

        CourtAssets.MeshObject("Clouds", root.transform, CourtAssets.SaveMesh(clouds.ToMesh("Clouds"), "Sky_Clouds"),
            m.MNube1, m.MNube2, m.MNube3);

        CourtAssets.MarkStatic(root);
        return CourtAssets.SavePrefab(root, "Sky");
    }

    // --------------------------------------------------------------- Cancha

    public static GameObject BuildCourt(CourtCamera cam, CourtMaterials m)
    {
        GameObject root = new("Court");

        // Medidas de la cancha leídas del mockup (ver CourtCamera.CourtRow).
        float baseline = CourtLayout.BaselineZ;
        float freeThrowZ = cam.OnCourt(540f, 1480f).z;
        float keyHalf = Mathf.Abs(cam.OnCourt(865f, 1480f).x);
        float arcApexZ = cam.OnCourt(540f, 1687f).z;
        float arcRadius = CourtLayout.RimCenter.z - arcApexZ;
        float half = CourtLayout.CourtHalfWidth;
        float w = CourtLayout.LineWidth;

        // Submeshes: 0 cemento, 1 luz, 2 sombra, 3 medio, 4 llave, 5 líneas, 6 grietas
        MeshKit floor = new(7);

        // Piso base y, detrás de la línea de fondo, la franja iluminada.
        floor.Sub(0).Quad(new Vector3(-80f, 0f, -12f), new Vector3(80f, 0f, -12f), new Vector3(80f, 0f, 150f), new Vector3(-80f, 0f, 150f), Vector3.up);
        floor.Sub(1).Quad(new Vector3(-80f, 0.003f, baseline + w * 0.5f), new Vector3(80f, 0.003f, baseline + w * 0.5f),
            new Vector3(80f, 0.003f, 150f), new Vector3(-80f, 0.003f, 150f), Vector3.up);

        // Facetas grandes: cuña en sombra abajo a la izquierda y cuña media
        // a la derecha, como en el mockup (extendidas fuera de cuadro para
        // cubrir pantallas más altas).
        floor.Sub(2).Fan(CourtPolygon(cam, 0.002f, -1400f, 1300f, 0f, 1300f, 948f, 2700f, -1400f, 2700f), Vector3.up);
        floor.Sub(3).Fan(CourtPolygon(cam, 0.002f, 720f, 1050f, 2400f, 1050f, 2400f, 2700f, 1984f, 2700f), Vector3.up);

        // Llave rellena en azul.
        floor.Sub(4).Quad(
            new Vector3(-keyHalf, 0.004f, freeThrowZ), new Vector3(keyHalf, 0.004f, freeThrowZ),
            new Vector3(keyHalf, 0.004f, baseline), new Vector3(-keyHalf, 0.004f, baseline), Vector3.up);

        // Líneas pintadas, algo gastadas (cortes en algunos tramos).
        const float lineY = 0.008f;
        floor.Sub(5);
        floor.GroundRibbon(new[] { new Vector3(-half, 0f, baseline), new Vector3(half, 0f, baseline) }, w, lineY,
            gaps: new[] { new Vector2(0.30f, 0.33f), new Vector2(0.62f, 0.645f) });
        floor.GroundRibbon(new[] { new Vector3(-half, 0f, baseline), new Vector3(-half, 0f, -12f) }, w, lineY);
        floor.GroundRibbon(new[] { new Vector3(half, 0f, baseline), new Vector3(half, 0f, -12f) }, w, lineY,
            gaps: new[] { new Vector2(0.18f, 0.24f) });
        floor.GroundRibbon(new[] { new Vector3(-keyHalf, 0f, freeThrowZ), new Vector3(-keyHalf, 0f, baseline) }, w, lineY,
            gaps: new[] { new Vector2(0.35f, 0.43f) });
        floor.GroundRibbon(new[] { new Vector3(keyHalf, 0f, freeThrowZ), new Vector3(keyHalf, 0f, baseline) }, w, lineY);
        floor.GroundRibbon(new[] { new Vector3(-keyHalf, 0f, freeThrowZ), new Vector3(keyHalf, 0f, freeThrowZ) }, w, lineY);
        floor.GroundRibbon(Circle(new Vector3(0f, 0f, freeThrowZ), keyHalf, 40), w, lineY, closed: true,
            gaps: new[] { new Vector2(0.58f, 0.62f) });
        floor.GroundRibbon(ThreePointLine(arcRadius, baseline), w, lineY,
            gaps: new[] { new Vector2(0.28f, 0.31f), new Vector2(0.71f, 0.73f) });

        // Grietas: líneas finas.
        floor.Sub(6);
        floor.GroundRibbon(CourtPolyline(cam, 760f, 1240f, 800f, 1300f, 780f, 1350f, 830f, 1420f), 0.03f, 0.006f);
        floor.GroundRibbon(CourtPolyline(cam, 170f, 1640f, 230f, 1680f, 215f, 1740f), 0.03f, 0.006f);
        floor.GroundRibbon(CourtPolyline(cam, 300f, 1120f, 350f, 1160f, 395f, 1165f), 0.025f, 0.006f);

        CourtAssets.MeshObject("Floor", root.transform, CourtAssets.SaveMesh(floor.ToMesh("Court"), "Court_Floor"),
            m.MCemento, m.MCementoLuz, m.MCementoSombra, m.MCementoMedio, m.MLlave, m.MLineas, m.MGrieta);

        // Collider del piso: la pelota se reinicia al tocar algo con tag "ground".
        GameObject collider = CourtAssets.Empty("FloorCollider", root.transform);
        collider.tag = "ground";
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, -0.5f, 60f);
        box.size = new Vector3(160f, 1f, 150f);

        CourtAssets.MarkStatic(root);
        return CourtAssets.SavePrefab(root, "Court");
    }

    private static List<Vector3> CourtPolygon(CourtCamera cam, float y, params float[] columnRowPairs)
    {
        List<Vector3> points = new();

        for (int i = 0; i < columnRowPairs.Length; i += 2)
        {
            Vector3 p = cam.OnCourt(columnRowPairs[i], columnRowPairs[i + 1]);
            p.y = y;
            points.Add(p);
        }

        return points;
    }

    private static List<Vector3> CourtPolyline(CourtCamera cam, params float[] columnRowPairs) =>
        CourtPolygon(cam, 0f, columnRowPairs);

    private static List<Vector3> Circle(Vector3 center, float radius, int segments)
    {
        List<Vector3> points = new();

        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            points.Add(center + new Vector3(Mathf.Sin(a) * radius, 0f, Mathf.Cos(a) * radius));
        }

        return points;
    }

    private static List<Vector3> ThreePointLine(float radius, float baseline)
    {
        float rimZ = CourtLayout.RimCenter.z;
        List<Vector3> points = new() { new Vector3(-radius, 0f, baseline), new Vector3(-radius, 0f, rimZ) };

        const int segments = 36;
        for (int i = 1; i < segments; i++)
        {
            float a = Mathf.Lerp(-90f, 90f, i / (float)segments) * Mathf.Deg2Rad;
            points.Add(new Vector3(Mathf.Sin(a) * radius, 0f, rimZ - Mathf.Cos(a) * radius));
        }

        points.Add(new Vector3(radius, 0f, rimZ));
        points.Add(new Vector3(radius, 0f, baseline));
        return points;
    }

    // ----------------------------------------------------------------- Reja

    public static GameObject BuildFence(CourtCamera cam, CourtMaterials m)
    {
        GameObject root = new("Fence");
        MeshKit fence = new();
        float z = CourtLayout.FenceZ;

        // Postes (cada 190 px en el mockup) y travesaño superior.
        float[] postColumns = { -320f, -130f, 60f, 250f, 440f, 630f, 820f, 1010f, 1200f, 1390f };
        float top = cam.OnPlaneZ(540f, 900f, z).y;
        float railTop = cam.OnPlaneZ(540f, 908f, z).y;
        float railBottom = Mathf.Min(cam.OnPlaneZ(540f, 920f, z).y, railTop - 0.06f);
        const float post = 0.08f;

        float xMin = float.MaxValue;
        float xMax = float.MinValue;

        foreach (float column in postColumns)
        {
            float x = cam.OnPlaneZ(column, 900f, z).x;
            xMin = Mathf.Min(xMin, x);
            xMax = Mathf.Max(xMax, x);
            fence.Box(new Vector3(x, (top - 0.1f) * 0.5f, z), new Vector3(post, top + 0.1f, post), BoxFaces.Visible);
        }

        fence.Box(new Vector3((xMin + xMax) * 0.5f, (railTop + railBottom) * 0.5f, z),
            new Vector3(xMax - xMin + post, railTop - railBottom, 0.06f), BoxFaces.Visible);

        // Malla de alambre en rombos, como líneas finas (sin texturas ni alfa).
        const float spacing = 0.2f;
        const float wire = 0.016f;
        float meshTop = railBottom;
        float meshZ = z + 0.03f;

        for (int direction = -1; direction <= 1; direction += 2)
        {
            for (float c = xMin - meshTop; c <= xMax + meshTop; c += spacing)
            {
                // Recta x = c + direction·y, recortada al rectángulo de la malla.
                float y0 = 0f;
                float y1 = meshTop;
                ClipToRange(c, direction, xMin, xMax, ref y0, ref y1);

                if (y1 - y0 < 0.01f)
                    continue;

                Vector3 a = new(c + direction * y0, y0, meshZ);
                Vector3 b = new(c + direction * y1, y1, meshZ);
                Vector3 along = (b - a).normalized;
                Vector3 side = new Vector3(-along.y, along.x, 0f) * (wire * 0.5f);
                fence.Quad(a - side, b - side, b + side, a + side, TowardCamera);
            }
        }

        CourtAssets.MeshObject("Fence", root.transform, CourtAssets.SaveMesh(fence.ToMesh("Fence"), "Fence"), m.MReja);
        CourtAssets.MarkStatic(root);
        return CourtAssets.SavePrefab(root, "Fence");
    }

    /// <summary>Recorta el tramo [y0, y1] de la recta x = c + dir·y para que x quede en [xMin, xMax].</summary>
    private static void ClipToRange(float c, int direction, float xMin, float xMax, ref float y0, ref float y1)
    {
        float ya = (xMin - c) / direction;
        float yb = (xMax - c) / direction;
        y0 = Mathf.Max(y0, Mathf.Min(ya, yb));
        y1 = Mathf.Min(y1, Mathf.Max(ya, yb));
    }

    // ------------------------------------------------------------ Edificios

    public static GameObject BuildBuildings(CourtCamera cam, CourtMaterials m)
    {
        GameObject root = new("Buildings");

        // Siluetas del mockup como escalones: (columna desde, columna hasta, fila del techo).
        float[,] far =
        {
            { -300, 0, 900 }, { 0, 70, 900 }, { 70, 150, 860 }, { 150, 210, 910 }, { 210, 300, 830 }, { 300, 380, 880 },
            { 380, 700, 930 }, { 700, 780, 870 }, { 780, 860, 820 }, { 860, 940, 890 }, { 940, 1020, 850 },
            { 1020, 1080, 905 }, { 1080, 1380, 880 }
        };

        float[,] near =
        {
            { -300, 0, 940 }, { 0, 120, 950 }, { 120, 200, 915 }, { 200, 330, 960 }, { 330, 420, 935 }, { 420, 660, 975 },
            { 660, 760, 940 }, { 760, 880, 965 }, { 880, 990, 925 }, { 990, 1080, 960 }, { 1080, 1380, 945 }
        };

        CourtAssets.MeshObject("Far", root.transform,
            CourtAssets.SaveMesh(Skyline(cam, far, CourtLayout.FarBuildingsZ, 8f).ToMesh("BuildingsFar"), "Buildings_Far"),
            m.MEdificiosLejos);
        CourtAssets.MeshObject("Near", root.transform,
            CourtAssets.SaveMesh(Skyline(cam, near, CourtLayout.NearBuildingsZ, 4f).ToMesh("BuildingsNear"), "Buildings_Near"),
            m.MEdificiosCerca);

        CourtAssets.MarkStatic(root);
        return CourtAssets.SavePrefab(root, "Buildings");
    }

    private static MeshKit Skyline(CourtCamera cam, float[,] steps, float z, float depth)
    {
        MeshKit kit = new();

        for (int i = 0; i < steps.GetLength(0); i++)
        {
            Vector3 a = cam.OnPlaneZ(steps[i, 0], steps[i, 2], z);
            Vector3 b = cam.OnPlaneZ(steps[i, 1], steps[i, 2], z);
            float bottom = -1f;
            kit.Box(
                new Vector3((a.x + b.x) * 0.5f, (a.y + bottom) * 0.5f, z + depth * 0.5f),
                new Vector3(Mathf.Abs(b.x - a.x), a.y - bottom, depth),
                BoxFaces.Visible);
        }

        return kit;
    }

    // --------------------------------------------------------------- Árbol

    /// <summary>
    /// Árbol de copa hexagonal low-poly (silueta del árbol izquierdo del
    /// mockup). El pivote queda en la base del tronco. Devuelve también
    /// dónde van los dos árboles del mockup.
    /// </summary>
    public static GameObject BuildTree(CourtCamera cam, CourtMaterials m, out Vector3 leftBase, out Vector3 rightBase)
    {
        float z = CourtLayout.TreesZ;
        leftBase = cam.OnPlaneZ(110f, 1012f, z);
        leftBase.y = 0f;
        rightBase = cam.OnPlaneZ(977f, 1012f, z);
        rightBase.y = 0f;

        float[] crownPx = { 40f, 940f, 60f, 880f, 110f, 850f, 160f, 880f, 175f, 940f, 110f, 975f };
        List<Vector3> crown = new();

        for (int i = 0; i < crownPx.Length; i += 2)
            crown.Add(cam.OnPlaneZ(crownPx[i], crownPx[i + 1], z) - leftBase);

        Vector3 center = Vector3.zero;
        foreach (Vector3 p in crown)
            center += p;
        center /= crown.Count;

        float width = crown[4].x - crown[0].x;
        Vector3 frontApex = center + Vector3.back * (width * 0.35f);
        Vector3 backApex = center + Vector3.forward * (width * 0.35f);

        MeshKit tree = new();

        for (int i = 0; i < crown.Count; i++)
        {
            Vector3 a = crown[i];
            Vector3 b = crown[(i + 1) % crown.Count];
            tree.Tri(a, b, frontApex, (a + b + frontApex) / 3f - center);
            tree.Tri(a, b, backApex, (a + b + backApex) / 3f - center);
        }

        float trunk = Mathf.Abs(cam.OnPlaneZ(119f, 1000f, z).x - cam.OnPlaneZ(101f, 1000f, z).x);
        tree.Box(new Vector3(0f, center.y * 0.5f, 0f), new Vector3(trunk, center.y, trunk), BoxFaces.Visible);

        GameObject root = new("Tree");
        CourtAssets.MeshObject("Mesh", root.transform, CourtAssets.SaveMesh(tree.ToMesh("Tree"), "Tree"), m.MArboles);
        CourtAssets.MarkStatic(root);
        return CourtAssets.SavePrefab(root, "Tree");
    }

    // ------------------------------------------------------------- Comunes

    /// <summary>Disco plano de radio 1 (sol, halo, check, sombras).</summary>
    public static MeshKit Disc(int segments, Vector3 facing)
    {
        MeshKit kit = new();
        Quaternion toFacing = Quaternion.FromToRotation(Vector3.back, facing);

        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            kit.Tri(
                Vector3.zero,
                toFacing * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f),
                toFacing * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f),
                facing);
        }

        return kit;
    }
}
