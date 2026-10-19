using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class HoopGeometry
{
    private const int NetStrands = 12;

    public static void Torus(MeshKit kit, float radius, float tube, int segments, int sides, Vector3 offset = default)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            Vector3 d0 = new(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
            Vector3 d1 = new(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
            Vector3 c0 = offset + d0 * radius;
            Vector3 c1 = offset + d1 * radius;
            Vector3 middle = (c0 + c1) * 0.5f;

            for (int j = 0; j < sides; j++)
            {
                float b0 = j / (float)sides * Mathf.PI * 2f;
                float b1 = (j + 1) / (float)sides * Mathf.PI * 2f;

                Vector3 P(Vector3 c, Vector3 d, float b) =>
                    c + (d * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b)) * tube;

                kit.SolidQuad(P(c0, d0, b0), P(c1, d1, b0), P(c1, d1, b1), P(c0, d0, b1), middle);
            }
        }
    }

    private static void Annulus(MeshKit kit, float radius, float halfWidth, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            Vector3 d0 = new(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
            Vector3 d1 = new(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
            kit.Quad(d0 * (radius - halfWidth), d1 * (radius - halfWidth), d1 * (radius + halfWidth), d0 * (radius + halfWidth), Vector3.up);
        }
    }

    private static Mesh Net(string name, float topRadius, float bottomRadius, float depth, int levels, float linkWidth, float ringTube, float topY)
    {
        MeshKit kit = new(2);
        float step = 360f / NetStrands;
        Color back = new(0.66f, 0.66f, 0.72f, 1f);

        Vector3 Point(int strand, int level)
        {
            float t = level / (float)levels;
            float offset = 0f;

            if (level > 0)
                offset = ((level % 2 == 1) == (strand % 2 == 0)) ? step * 0.5f : -step * 0.5f;

            float angle = (strand * step + offset) * Mathf.Deg2Rad;
            float radius = Mathf.Lerp(topRadius, bottomRadius, Mathf.Pow(t, 0.85f));
            return new Vector3(Mathf.Sin(angle) * radius, topY - depth * t, Mathf.Cos(angle) * radius);
        }

        kit.Sub(0);

        for (int s = 0; s < NetStrands; s++)
        {
            for (int l = 0; l < levels; l++)
            {
                Vector3 a = Point(s, l);
                Vector3 b = Point(s, l + 1);

                foreach (Vector2 link in new[] { new Vector2(0.06f, 0.46f), new Vector2(0.54f, 0.94f) })
                {
                    Vector3 p0 = Vector3.Lerp(a, b, link.x);
                    Vector3 p1 = Vector3.Lerp(a, b, link.y);
                    Vector3 along = (p1 - p0).normalized;
                    Vector3 side = Vector3.Cross(along, Vector3.forward);

                    if (side.sqrMagnitude < 1e-6f)
                        side = Vector3.right;

                    side = side.normalized * (linkWidth * 0.5f);
                    Color shade = (p0.z + p1.z) * 0.5f > 0.02f ? back : Color.white;
                    kit.Quad(p0 - side, p1 - side, p1 + side, p0 + side, Vector3.back, shade);
                }
            }
        }

        kit.Sub(1);
        Torus(kit, bottomRadius, ringTube, 12, 3, new Vector3(0f, topY - depth, 0f));

        return CourtAssets.SaveMesh(kit.ToMesh(name), name);
    }

    private static void RimColliders(Transform parent, float radius, float tube, int count)
    {
        Transform holder = CourtAssets.Empty("RimColliders", parent).transform;
        float chord = 2f * radius * Mathf.Sin(Mathf.PI / count);

        for (int k = 0; k < count; k++)
        {
            float a = (k + 0.5f) / count * Mathf.PI * 2f;
            Vector3 position = new(Mathf.Sin(a) * radius, 0f, Mathf.Cos(a) * radius);
            Vector3 tangent = new(Mathf.Cos(a), 0f, -Mathf.Sin(a));

            GameObject segment = CourtAssets.Empty($"Segment{k}", holder, position);
            segment.transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up);

            CapsuleCollider capsule = segment.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.radius = tube;
            capsule.height = chord + tube * 2f;
        }
    }

    private static GameObject ScoreTrigger(Transform parent, Vector3 size, float y)
    {
        GameObject trigger = CourtAssets.Empty("ScoreTrigger", parent, new Vector3(0f, y, 0f));
        BoxCollider box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        trigger.AddComponent<HoopScoreTrigger>();
        return trigger;
    }

    public static Mesh PoleMesh()
    {
        MeshKit kit = new(2);
        float h = CourtLayout.PoleSize * 0.5f;
        Quaternion rotation = Quaternion.Euler(0f, -30f, 0f);
        Vector3[] corners =
        {
            rotation * new Vector3(-h, 0f, -h),
            rotation * new Vector3(h, 0f, -h),
            rotation * new Vector3(h, 0f, h),
            rotation * new Vector3(-h, 0f, h)
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[(i + 1) % 4];
            Vector3 outward = (a + b) * 0.5f;
            kit.Sub(outward.normalized.x < -0.5f ? 1 : 0)
                .Quad(a, b, b + Vector3.up, a + Vector3.up, outward);
        }

        List<Vector3> top = new();
        foreach (Vector3 c in corners)
            top.Add(c + Vector3.up);
        kit.Sub(0).Fan(top, Vector3.up);

        return CourtAssets.SaveMesh(kit.ToMesh("Pole"), "Pole");
    }

    public static Mesh ShadowStripMesh()
    {
        MeshKit kit = new();
        float w = CourtLayout.PoleSize * 0.6f;
        const float y = 0.01f;
        kit.Quad(new Vector3(-w, y, 0f), new Vector3(w, y, 0f), new Vector3(w * 1.6f, y, 1f), new Vector3(-w * 1.6f, y, 1f), Vector3.up);
        return CourtAssets.SaveMesh(kit.ToMesh("ShadowStrip"), "GroundShadowStrip");
    }

    private static GameObject PoleWithShadow(Transform parent, Mesh pole, Mesh strip, CourtMaterials m, Vector3 basePosition, float height)
    {
        GameObject poleObject = CourtAssets.MeshObject("Pole", parent, pole, m.MPoste, m.MPosteFilo);
        poleObject.transform.localPosition = basePosition;
        poleObject.transform.localScale = new Vector3(1f, height, 1f);
        BoxCollider collider = poleObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.5f, 0f);
        collider.size = new Vector3(CourtLayout.PoleSize, 1f, CourtLayout.PoleSize);

        GameObject shadow = CourtAssets.MeshObject("PoleShadow", parent, strip, m.MSombraPlana);
        shadow.transform.localPosition = basePosition;
        shadow.transform.localRotation = Quaternion.Euler(0f, CourtLayout.LightYaw, 0f);
        shadow.transform.localScale = new Vector3(1f, 1f, height * CourtLayout.ShadowLengthPerMeter);

        return poleObject;
    }

    public static GameObject BuildHoop(CourtMaterials m, Mesh pole, Mesh strip)
    {
        const float R = CourtLayout.RimRadius;
        const float tube = CourtLayout.RimTube;
        const float W = CourtLayout.BoardWidth;
        const float H = CourtLayout.BoardHeight;
        const float T = CourtLayout.BoardThickness;
        const float bottom = CourtLayout.BoardBottom;
        const float front = CourtLayout.BoardFrontZ;

        GameObject root = new("Hoop");
        HoopVisual visual = root.AddComponent<HoopVisual>();
        Transform scaled = CourtAssets.Empty("Scaled", root.transform).transform;

        MeshKit rimKit = new();
        Torus(rimKit, R, tube, 16, 6);
        GameObject rim = CourtAssets.MeshObject("Rim", scaled, CourtAssets.SaveMesh(rimKit.ToMesh("Rim"), "Hoop_Rim"), m.MAro);
        RimColliders(scaled, R, tube, 12);

        MeshKit glowKit = new();
        Annulus(glowKit, R, 0.09f, 32);
        GameObject glow = CourtAssets.MeshObject("Glow", scaled, CourtAssets.SaveMesh(glowKit.ToMesh("Glow"), "Hoop_Glow"), m.MBrilloAro);

        MeshKit bracketKit = new();
        float bracketStart = R - 0.02f;
        bracketKit.Box(new Vector3(0f, 0.03f, (bracketStart + front) * 0.5f), new Vector3(0.18f, 0.1f, front - bracketStart),
            BoxFaces.All & ~BoxFaces.Back);
        CourtAssets.MeshObject("Bracket", scaled, CourtAssets.SaveMesh(bracketKit.ToMesh("Bracket"), "Hoop_Bracket"), m.MSoporte);

        Vector3 boardCenter = new(0f, bottom + H * 0.5f, front + T * 0.5f);
        Vector3 boardSize = new(W, H, T);
        MeshKit boardKit = new(2);
        boardKit.Sub(0).Box(boardCenter, boardSize, BoxFaces.Front);
        boardKit.Sub(1).Box(boardCenter, boardSize, BoxFaces.Left | BoxFaces.Right | BoxFaces.Top | BoxFaces.Bottom);
        GameObject board = CourtAssets.MeshObject("Backboard", scaled, CourtAssets.SaveMesh(boardKit.ToMesh("Backboard"), "Hoop_Backboard"),
            m.MTablero, m.MTableroBorde);
        BoxCollider boardCollider = board.AddComponent<BoxCollider>();
        boardCollider.center = boardCenter;
        boardCollider.size = boardSize;

        CourtAssets.MeshObject("BackboardDetails", scaled, BackboardDetails(),
            m.MTableroSombra, m.MOxido, m.MTableroBorde, m.MTableroGastado, m.MRecuadro, m.MFilo);

        Mesh netMesh = Net("Hoop_Net", R, CourtLayout.NetBottomRadius, CourtLayout.NetDepth, 6, 0.028f, 0.014f, -tube);
        GameObject net = CourtAssets.MeshObject("Net", scaled, netMesh, m.MCadena, m.MAroRed);
        ChainNet chain = net.AddComponent<ChainNet>();

        Transform attach = CourtAssets.Empty("PoleAttach", scaled, new Vector3(0f, bottom + H * 0.5f, CourtLayout.PoleZ)).transform;
        ScoreTrigger(scaled, CourtLayout.HoopTriggerSize, CourtLayout.HoopTriggerY);

        float rimHeight = CourtLayout.RimCenter.y;
        GameObject poleObject = PoleWithShadow(root.transform, pole, strip, m,
            new Vector3(0f, -rimHeight, CourtLayout.PoleZ), rimHeight + attach.localPosition.y);

        SerializedObject so = new(visual);
        so.FindProperty("scaledRoot").objectReferenceValue = scaled;
        SetArray(so.FindProperty("rimRenderers"), rim.GetComponent<Renderer>());
        so.FindProperty("glowRenderer").objectReferenceValue = glow.GetComponent<Renderer>();
        so.FindProperty("net").objectReferenceValue = chain;
        so.FindProperty("poleAttach").objectReferenceValue = attach;
        so.FindProperty("pole").objectReferenceValue = poleObject.transform;
        so.FindProperty("poleShadow").objectReferenceValue = root.transform.Find("PoleShadow");
        so.FindProperty("groundY").floatValue = 0f;
        so.FindProperty("shadowLengthPerMeter").floatValue = CourtLayout.ShadowLengthPerMeter;
        so.ApplyModifiedPropertiesWithoutUndo();

        glow.GetComponent<Renderer>().enabled = false;

        return CourtAssets.SavePrefab(root, "Hoop");
    }

    private static Mesh BackboardDetails()
    {
        MeshKit kit = new(6);

        Vector3 P(float px, float py, float eps) => new(
            -CourtLayout.BoardWidth * 0.5f + (px - 350f) / 380f * CourtLayout.BoardWidth,
            CourtLayout.BoardBottom + (800f - py) / 240f * CourtLayout.BoardHeight,
            CourtLayout.BoardFrontZ - eps);

        void Poly(int sub, float eps, params float[] xy)
        {
            List<Vector3> points = new();
            for (int i = 0; i < xy.Length; i += 2)
                points.Add(P(xy[i], xy[i + 1], eps));
            kit.Sub(sub).Fan(points, Vector3.back);
        }

        void Rect(int sub, float eps, float x0, float y0, float x1, float y1) =>
            Poly(sub, eps, x0, y0, x1, y0, x1, y1, x0, y1);

        Poly(0, 0.002f, 350f, 800f, 730f, 800f, 730f, 560f);

        Poly(1, 0.003f, 640f, 560f, 668f, 560f, 660f, 640f, 648f, 612f);

        Rect(2, 0.004f, 350f, 560f, 730f, 566f);
        Rect(2, 0.004f, 350f, 794f, 730f, 800f);
        Rect(2, 0.004f, 350f, 566f, 356f, 794f);
        Rect(2, 0.004f, 724f, 566f, 730f, 794f);

        Poly(3, 0.005f, 350f, 560f, 404f, 560f, 372f, 594f, 350f, 586f);
        Poly(3, 0.005f, 690f, 800f, 730f, 768f, 730f, 800f);
        Poly(3, 0.005f, 428f, 800f, 468f, 780f, 502f, 800f);

        Rect(4, 0.004f, 457f, 663f, 623f, 673f);
        Rect(4, 0.004f, 457f, 775f, 623f, 785f);
        Rect(4, 0.004f, 457f, 673f, 467f, 775f);
        Rect(4, 0.004f, 613f, 673f, 623f, 775f);

        Rect(5, 0.006f, 350f, 566f, 355f, 800f);
        Rect(5, 0.006f, 352f, 560f, 724f, 565f);

        return CourtAssets.SaveMesh(kit.ToMesh("BackboardDetails"), "Hoop_BackboardDetails");
    }

    public static GameObject BuildHoopSmall(CourtMaterials m)
    {
        const float R = CourtLayout.SmallRimRadius;
        const float tube = CourtLayout.SmallRimTube;
        const float W = CourtLayout.PlateWidth;
        const float H = CourtLayout.PlateHeight;
        const float T = CourtLayout.PlateThickness;
        const float bottom = CourtLayout.PlateBottom;
        const float front = CourtLayout.PlateFrontZ;

        GameObject root = new("HoopSmall");
        HoopVisual visual = root.AddComponent<HoopVisual>();
        Transform scaled = CourtAssets.Empty("Scaled", root.transform).transform;

        MeshKit rimKit = new();
        Torus(rimKit, R, tube, 16, 6);
        GameObject rim = CourtAssets.MeshObject("Rim", scaled, CourtAssets.SaveMesh(rimKit.ToMesh("Rim"), "HoopSmall_Rim"), m.MAro);
        RimColliders(scaled, R, tube, 12);

        MeshKit glowKit = new();
        Annulus(glowKit, R, 0.07f, 32);
        GameObject glow = CourtAssets.MeshObject("Glow", scaled, CourtAssets.SaveMesh(glowKit.ToMesh("Glow"), "HoopSmall_Glow"), m.MBrilloAro);

        MeshKit bracketKit = new();
        float bracketStart = R - 0.02f;
        bracketKit.Box(new Vector3(0f, 0.02f, (bracketStart + front) * 0.5f), new Vector3(0.1f, 0.07f, front - bracketStart),
            BoxFaces.All & ~BoxFaces.Back);
        CourtAssets.MeshObject("Bracket", scaled, CourtAssets.SaveMesh(bracketKit.ToMesh("Bracket"), "HoopSmall_Bracket"), m.MPoste);

        Vector3 plateCenter = new(0f, bottom + H * 0.5f, front + T * 0.5f);
        Vector3 plateSize = new(W, H, T);
        MeshKit plateKit = new(2);
        plateKit.Sub(0).Box(plateCenter, plateSize, BoxFaces.Front);
        plateKit.Sub(1).Box(plateCenter, plateSize, BoxFaces.Left | BoxFaces.Right | BoxFaces.Top | BoxFaces.Bottom);

        const float frame = 0.025f;
        float z = front - 0.003f;
        float x0 = -W * 0.5f, x1 = W * 0.5f, y0 = bottom, y1 = bottom + H;
        plateKit.Sub(1);
        plateKit.Quad(new Vector3(x0, y1 - frame, z), new Vector3(x1, y1 - frame, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z), Vector3.back);
        plateKit.Quad(new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y0 + frame, z), new Vector3(x0, y0 + frame, z), Vector3.back);
        plateKit.Quad(new Vector3(x0, y0, z), new Vector3(x0 + frame, y0, z), new Vector3(x0 + frame, y1, z), new Vector3(x0, y1, z), Vector3.back);
        plateKit.Quad(new Vector3(x1 - frame, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x1 - frame, y1, z), Vector3.back);

        GameObject plate = CourtAssets.MeshObject("Plate", scaled, CourtAssets.SaveMesh(plateKit.ToMesh("Plate"), "HoopSmall_Plate"),
            m.MTablero, m.MTableroBorde);
        BoxCollider plateCollider = plate.AddComponent<BoxCollider>();
        plateCollider.center = plateCenter;
        plateCollider.size = plateSize;

        Mesh netMesh = Net("HoopSmall_Net", R, CourtLayout.SmallNetBottomRadius, CourtLayout.SmallNetDepth, 4, 0.024f, 0.011f, -tube);
        GameObject net = CourtAssets.MeshObject("Net", scaled, netMesh, m.MCadena, m.MAroRed);
        ChainNet chain = net.AddComponent<ChainNet>();

        CourtAssets.Empty("ArmAttach", scaled, new Vector3(0f, bottom + H * 0.5f, front + T));
        ScoreTrigger(scaled, CourtLayout.SmallTriggerSize, -0.1f);

        GameObject check = CourtAssets.MeshObject("Check", root.transform, CheckMesh(), m.MExito, m.MCheckTrazo);
        check.transform.localPosition = new Vector3(0f, bottom + H + 0.24f, front - 0.02f);
        check.SetActive(false);

        SerializedObject so = new(visual);
        so.FindProperty("scaledRoot").objectReferenceValue = scaled;
        SetArray(so.FindProperty("rimRenderers"), rim.GetComponent<Renderer>());
        so.FindProperty("glowRenderer").objectReferenceValue = glow.GetComponent<Renderer>();
        so.FindProperty("net").objectReferenceValue = chain;
        so.FindProperty("checkMark").objectReferenceValue = check;
        so.ApplyModifiedPropertiesWithoutUndo();

        glow.GetComponent<Renderer>().enabled = false;

        return CourtAssets.SavePrefab(root, "HoopSmall");
    }

    private static Mesh CheckMesh()
    {
        MeshKit kit = new(2);
        const float radius = 0.13f;
        const int segments = 20;

        kit.Sub(0);
        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            kit.Tri(Vector3.zero, new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius,
                new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius, Vector3.back);
        }

        Vector3[] stroke = { new(-0.06f, 0f, -0.004f), new(-0.018f, -0.045f, -0.004f), new(0.065f, 0.05f, -0.004f) };
        const float width = 0.032f;
        kit.Sub(1);

        for (int i = 0; i < stroke.Length - 1; i++)
        {
            Vector3 a = stroke[i];
            Vector3 b = stroke[i + 1];
            Vector3 along = (b - a).normalized;
            Vector3 side = new Vector3(-along.y, along.x, 0f) * (width * 0.5f);
            Vector3 cap = along * (width * 0.5f);
            kit.Quad(a - side - cap, b - side + cap, b + side + cap, a + side - cap, Vector3.back);
        }

        return CourtAssets.SaveMesh(kit.ToMesh("Check"), "Check");
    }

    public static GameObject BuildCarousel(CourtMaterials m, GameObject hoopSmall, Mesh pole, Mesh strip)
    {
        GameObject root = new("HoopCarousel");
        float rimHeight = CourtLayout.RimCenter.y;

        PoleWithShadow(root.transform, pole, strip, m, new Vector3(0f, -rimHeight, 0f), rimHeight + CourtLayout.HubY);

        MeshKit hubKit = new(2);
        const float hubHalfHeight = 0.13f;
        Vector3[] ring = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float a = (i / 6f * 360f + 30f) * Mathf.Deg2Rad;
            ring[i] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * CourtLayout.HubRadius;
        }

        List<Vector3> topCap = new();
        List<Vector3> bottomCap = new();
        for (int i = 0; i < 6; i++)
        {
            Vector3 a = ring[i];
            Vector3 b = ring[(i + 1) % 6];
            hubKit.Sub(0).Quad(a + Vector3.down * hubHalfHeight, b + Vector3.down * hubHalfHeight,
                b + Vector3.up * hubHalfHeight, a + Vector3.up * hubHalfHeight, (a + b) * 0.5f);
            topCap.Add(a + Vector3.up * hubHalfHeight);
            bottomCap.Add(a + Vector3.down * hubHalfHeight);
        }

        hubKit.Sub(1).Fan(topCap, Vector3.up);
        hubKit.Sub(1).Fan(bottomCap, Vector3.down);

        GameObject hub = CourtAssets.MeshObject("Hub", root.transform, CourtAssets.SaveMesh(hubKit.ToMesh("Hub"), "Carousel_Hub"),
            m.MHubLado, m.MHubTapa);
        hub.transform.localPosition = new Vector3(0f, CourtLayout.HubY, 0f);

        MeshKit armKit = new();
        armKit.Box(new Vector3(0f, 0f, 0.5f), new Vector3(0.06f, 0.06f, 1f), BoxFaces.Left | BoxFaces.Right | BoxFaces.Top | BoxFaces.Bottom);
        Mesh armMesh = CourtAssets.SaveMesh(armKit.ToMesh("Arm"), "Carousel_Arm");
        Transform armsHolder = CourtAssets.Empty("Arms", root.transform).transform;

        Transform[] hoops = new Transform[5];
        Transform[] arms = new Transform[5];
        Transform[] targets = new Transform[5];

        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            GameObject hoop = (GameObject)PrefabUtility.InstantiatePrefab(hoopSmall);
            hoop.name = $"Hoop{i + 1}";
            hoop.transform.SetParent(root.transform, false);
            hoop.transform.localPosition = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * CourtLayout.CarouselRadius;
            hoops[i] = hoop.transform;
            targets[i] = hoop.transform.Find("Scaled/ArmAttach");

            GameObject arm = CourtAssets.MeshObject($"Arm{i + 1}", armsHolder, armMesh, m.MPoste);
            Vector3 from = hub.transform.localPosition;
            Vector3 delta = hoop.transform.localPosition + targets[i].localPosition - from;
            arm.transform.localPosition = from;
            arm.transform.localRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            arm.transform.localScale = new Vector3(1f, 1f, delta.magnitude);
            arms[i] = arm.transform;
        }

        ChallengeCarousel carousel = root.AddComponent<ChallengeCarousel>();
        SerializedObject carouselSo = new(carousel);
        SetArray(carouselSo.FindProperty("hoops"), hoops);
        carouselSo.ApplyModifiedPropertiesWithoutUndo();

        CarouselRig rig = root.AddComponent<CarouselRig>();
        SerializedObject rigSo = new(rig);
        rigSo.FindProperty("hub").objectReferenceValue = hub.transform;
        SetArray(rigSo.FindProperty("arms"), arms);
        SetArray(rigSo.FindProperty("armTargets"), targets);
        rigSo.ApplyModifiedPropertiesWithoutUndo();

        return CourtAssets.SavePrefab(root, "HoopCarousel");
    }

    public static void SetArray(SerializedProperty array, params Object[] values)
    {
        array.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
