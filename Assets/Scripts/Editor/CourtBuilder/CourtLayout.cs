using UnityEngine;

public static class CourtLayout
{
    public const float MockWidth = 1080f;
    public const float MockHeight = 1920f;
    public const float HorizonRow = 1012f;
    public const float BallRow = 1600f;
    public const float BallRadiusPx = 150f;
    public const float RimRow = 826f;
    public const float BoardTopRow = 560f;
    public const float BoardBottomRow = 800f;
    public const float MockBaselineRow = 1050f;

    public static readonly Vector3 BallRest = new(0f, 1f, 0f);
    public static readonly Vector3 RimCenter = new(0f, 2.5f, 8f);
    public const float BallDiameter = 0.38128f;

    public const float RimRadius = 0.46f;
    public const float RimTube = 0.035f;
    public const float BoardWidth = 1.62f;
    public const float BoardHeight = 1.02f;
    public const float BoardThickness = 0.06f;
    public const float BoardBottom = 0.15f;
    public const float BoardFrontZ = 0.60f;
    public const float PoleSize = 0.14f;
    public const float PoleZ = BoardFrontZ + BoardThickness + 0.12f;
    public const float NetDepth = 0.58f;
    public const float NetBottomRadius = 0.26f;
    public static readonly Vector3 HoopTriggerSize = new(0.7f, 0.35f, 0.7f);
    public const float HoopTriggerY = -0.12f;

    public const float SmallRimRadius = 0.40f;
    public const float SmallRimTube = 0.03f;
    public const float PlateWidth = 0.52f;
    public const float PlateHeight = 0.36f;
    public const float PlateBottom = 0.09f;
    public const float PlateThickness = 0.04f;
    public const float PlateFrontZ = 0.52f;
    public const float SmallNetDepth = 0.45f;
    public const float SmallNetBottomRadius = 0.22f;
    public static readonly Vector3 SmallTriggerSize = new(0.6f, 0.3f, 0.6f);
    public const float CarouselRadius = 1.8f;
    public const float HubRadius = 0.18f;
    public const float HubY = 0.30f;

    public static float BaselineZ => RimCenter.z + PoleZ - PoleSize * 0.5f - LineWidth * 0.5f;
    public const float CourtHalfWidth = 7.5f;
    public const float LineWidth = 0.07f;

    public const float FenceZ = 14f;
    public const float TreesZ = 24f;
    public const float NearBuildingsZ = 45f;
    public const float FarBuildingsZ = 80f;
    public const float CloudsZ = 140f;
    public const float SunZ = 146f;
    public const float SkyZ = 150f;

    public const float LightElevation = 15f;
    public const float LightYaw = 176f;

    public static Quaternion LightRotation => Quaternion.Euler(LightElevation, LightYaw, 0f);

    public static float ShadowLengthPerMeter => 1f / Mathf.Tan(LightElevation * Mathf.Deg2Rad);
}

public readonly struct CourtCamera
{
    public CourtCamera(Vector3 position, float pitchDown, float verticalFov)
    {
        Position = position;
        PitchDown = pitchDown;
        VerticalFov = verticalFov;
    }

    public Vector3 Position { get; }

    public float PitchDown { get; }

    public float VerticalFov { get; }

    public Quaternion Rotation => Quaternion.Euler(PitchDown, 0f, 0f);

    private float Focal => FocalFor(VerticalFov);

    private Vector3 Forward => Rotation * Vector3.forward;
    private Vector3 Up => Rotation * Vector3.up;
    private Vector3 Right => Rotation * Vector3.right;

    private static float FocalFor(float fov) =>
        CourtLayout.MockHeight * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

    public Vector3 Ray(float column, float row)
    {
        float f = Focal;
        return (Forward
                + Up * ((CourtLayout.MockHeight * 0.5f - row) / f)
                + Right * ((column - CourtLayout.MockWidth * 0.5f) / f)).normalized;
    }

    public Vector3 OnPlaneZ(float column, float row, float z)
    {
        Vector3 ray = Ray(column, row);
        float t = (z - Position.z) / ray.z;
        return Position + ray * t;
    }

    public Vector3 OnGround(float column, float row, float y = 0f)
    {
        Vector3 ray = Ray(column, row);

        if (ray.y >= -1e-4f)
            ray.y = -1e-4f;

        float t = (y - Position.y) / ray.y;
        return Position + ray * t;
    }

    public Vector2 Project(Vector3 world)
    {
        Vector3 rel = world - Position;
        float depth = Vector3.Dot(rel, Forward);
        float f = Focal;
        return new Vector2(
            CourtLayout.MockWidth * 0.5f + f * Vector3.Dot(rel, Right) / depth,
            CourtLayout.MockHeight * 0.5f - f * Vector3.Dot(rel, Up) / depth);
    }

    public float CourtRow(float mockRow)
    {
        float baselineRow = Project(new Vector3(0f, 0f, CourtLayout.BaselineZ)).y;
        return baselineRow + (mockRow - CourtLayout.MockBaselineRow) *
               (CourtLayout.MockHeight - baselineRow) / (CourtLayout.MockHeight - CourtLayout.MockBaselineRow);
    }

    public Vector3 OnCourt(float column, float mockRow, float y = 0f) =>
        OnGround(column, CourtRow(mockRow), y);

    public static CourtCamera Solve(out string report)
    {
        float low = 15f;
        float high = 80f;
        float errorLow = Build(low).Project(CourtLayout.RimCenter).y - CourtLayout.RimRow;
        float fov = 40f;

        for (int i = 0; i < 80; i++)
        {
            fov = (low + high) * 0.5f;
            float error = Build(fov).Project(CourtLayout.RimCenter).y - CourtLayout.RimRow;

            if (Mathf.Abs(error) < 0.01f)
                break;

            if (Mathf.Sign(error) == Mathf.Sign(errorLow))
            {
                low = fov;
                errorLow = error;
            }
            else
            {
                high = fov;
            }
        }

        CourtCamera camera = Build(fov);

        Vector3 boardBottom = CourtLayout.RimCenter + new Vector3(0f, CourtLayout.BoardBottom, CourtLayout.BoardFrontZ);
        Vector3 boardTop = boardBottom + Vector3.up * CourtLayout.BoardHeight;
        float horizon = CourtLayout.MockHeight * 0.5f - Mathf.Tan(camera.PitchDown * Mathf.Deg2Rad) * FocalFor(fov);
        float h = CourtLayout.MockHeight;

        report =
            $"Cámara: pos {camera.Position:F3}, pitch {camera.PitchDown:F2}°, FOV vertical {fov:F2}° en 9:16\n" +
            $"  horizonte {horizon / h:P1} · tablero {camera.Project(boardTop).y / h:P1}–{camera.Project(boardBottom).y / h:P1}" +
            $" · aro {camera.Project(CourtLayout.RimCenter).y / h:P1} · pelota {camera.Project(CourtLayout.BallRest).y / h:P1}" +
            $" · línea de fondo {camera.Project(new Vector3(0f, 0f, CourtLayout.BaselineZ)).y / h:P1}";

        return camera;
    }

    private static CourtCamera Build(float fov)
    {
        float f = FocalFor(fov);
        float half = CourtLayout.MockHeight * 0.5f;

        float pitch = Mathf.Atan((half - CourtLayout.HorizonRow) / f) * Mathf.Rad2Deg;
        CourtCamera oriented = new(Vector3.zero, pitch, fov);

        Vector3 ray = oriented.Ray(CourtLayout.MockWidth * 0.5f, CourtLayout.BallRow);
        float depthPerUnit = Vector3.Dot(ray, oriented.Forward);
        float distance = f * (CourtLayout.BallDiameter * 0.5f) / (CourtLayout.BallRadiusPx * depthPerUnit);

        return new CourtCamera(CourtLayout.BallRest - ray * distance, pitch, fov);
    }
}
