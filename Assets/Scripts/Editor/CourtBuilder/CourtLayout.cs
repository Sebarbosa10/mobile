using UnityEngine;

/// <summary>
/// Medidas del mundo y composición de cámara.
///
/// La cámara se resuelve contra el mockup (Escena, 1080×1920):
///   horizonte en la fila 1012 (53%), pelota centrada en 1600 (83%) con
///   radio de 150 px, y centro del aro en la fila 826 (43%).
/// Las anclas del gameplay no se tocan: pelota en reposo en (0, 1, 0)
/// y centro del aro en (0, 2.5, 8). Con esa cámara, el tamaño del aro y
/// del tablero sale de las filas del tablero en el mockup (560–800, es
/// decir 29%–42%): el aro mide ~0.92 m, unas 1.3 veces el trigger de
/// 0.7 m que ya tenía el juego.
///
/// Todo lo que vive en el fondo (cielo, sol, nubes, edificios, reja,
/// árboles) se ubica "desproyectando" las coordenadas en píxeles del
/// mockup sobre planos a distintas profundidades, así en 1080×1920 cae
/// exactamente donde está dibujado.
/// </summary>
public static class CourtLayout
{
    // Mockup
    public const float MockWidth = 1080f;
    public const float MockHeight = 1920f;
    public const float HorizonRow = 1012f;
    public const float BallRow = 1600f;
    public const float BallRadiusPx = 150f;
    public const float RimRow = 826f;
    public const float BoardTopRow = 560f;
    public const float BoardBottomRow = 800f;
    public const float MockBaselineRow = 1050f;

    // Anclas del gameplay (iguales en DummyLab y Challenges)
    public static readonly Vector3 BallRest = new(0f, 1f, 0f);
    public static readonly Vector3 RimCenter = new(0f, 2.5f, 8f);
    public const float BallDiameter = 0.38128f;

    // Aro grande (prefab Hoop), en metros relativos al centro del aro.
    // Salen de resolver el tablero del mockup con la cámara de abajo.
    public const float RimRadius = 0.46f;        // radio de la línea central del toro (Ø 0.92 m)
    public const float RimTube = 0.035f;
    public const float BoardWidth = 1.62f;       // proporción 380×240 del mockup
    public const float BoardHeight = 1.02f;
    public const float BoardThickness = 0.06f;
    public const float BoardBottom = 0.15f;      // borde inferior por encima del aro
    public const float BoardFrontZ = 0.60f;
    public const float PoleSize = 0.14f;
    public const float PoleZ = BoardFrontZ + BoardThickness + 0.12f;
    public const float NetDepth = 0.58f;
    public const float NetBottomRadius = 0.26f;
    public static readonly Vector3 HoopTriggerSize = new(0.7f, 0.35f, 0.7f); // mismo ancho que el trigger original
    public const float HoopTriggerY = -0.12f;

    // Aro chico del carrusel. El mockup lo dibuja más chico (Ø ~0.5 m),
    // pero con colliders de aro la pelota (Ø 0.38) casi no pasaría: se
    // agrandó para conservar la dificultad que tenía el desafío.
    public const float SmallRimRadius = 0.40f;
    public const float SmallRimTube = 0.03f;
    public const float PlateWidth = 0.52f;       // proporción 92×64 del mockup
    public const float PlateHeight = 0.36f;
    public const float PlateBottom = 0.09f;
    public const float PlateThickness = 0.04f;
    public const float PlateFrontZ = 0.52f;
    public const float SmallNetDepth = 0.45f;
    public const float SmallNetBottomRadius = 0.22f;
    public static readonly Vector3 SmallTriggerSize = new(0.6f, 0.3f, 0.6f);
    public const float CarouselRadius = 1.8f;    // el mismo radio que ya tenía el desafío
    public const float HubRadius = 0.18f;
    public const float HubY = 0.30f;

    // Cancha
    public static float BaselineZ => RimCenter.z + PoleZ - PoleSize * 0.5f - LineWidth * 0.5f;
    public const float CourtHalfWidth = 7.5f;
    public const float LineWidth = 0.07f;

    // Fondo: profundidad (z) de cada capa de silueta
    public const float FenceZ = 14f;
    public const float TreesZ = 24f;
    public const float NearBuildingsZ = 45f;
    public const float FarBuildingsZ = 80f;
    public const float CloudsZ = 140f;
    public const float SunZ = 146f;
    public const float SkyZ = 150f;

    // Luz: baja (15°) desde atrás y un poco a la izquierda del aro, del
    // mismo lado que el sol dibujado en el cielo.
    public const float LightElevation = 15f;
    public const float LightYaw = 176f;

    public static Quaternion LightRotation => Quaternion.Euler(LightElevation, LightYaw, 0f);

    /// <summary>Dirección en el piso hacia donde caen las sombras.</summary>
    public static Vector3 ShadowDirection
    {
        get
        {
            Vector3 forward = LightRotation * Vector3.forward;
            forward.y = 0f;
            return forward.normalized;
        }
    }

    /// <summary>Largo de la sombra por cada metro de altura.</summary>
    public static float ShadowLengthPerMeter => 1f / Mathf.Tan(LightElevation * Mathf.Deg2Rad);
}

/// <summary>
/// Cámara pinhole simple (sin roll ni yaw) con la que se resuelve la
/// composición y se ubican las piezas del fondo. Filas y columnas en
/// píxeles del mockup de 1080×1920, contadas desde arriba a la izquierda.
/// </summary>
public readonly struct CourtCamera
{
    public CourtCamera(Vector3 position, float pitchDown, float verticalFov)
    {
        Position = position;
        PitchDown = pitchDown;
        VerticalFov = verticalFov;
    }

    public Vector3 Position { get; }

    /// <summary>Pitch en grados, positivo mirando hacia abajo (como el euler X de Unity).</summary>
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

    /// <summary>Punto del mundo sobre el plano z = <paramref name="z"/> que se ve en (columna, fila).</summary>
    public Vector3 OnPlaneZ(float column, float row, float z)
    {
        Vector3 ray = Ray(column, row);
        float t = (z - Position.z) / ray.z;
        return Position + ray * t;
    }

    /// <summary>Punto del piso (y = <paramref name="y"/>) que se ve en (columna, fila).</summary>
    public Vector3 OnGround(float column, float row, float y = 0f)
    {
        Vector3 ray = Ray(column, row);

        if (ray.y >= -1e-4f)
            ray.y = -1e-4f; // por encima del horizonte: lo más lejos posible

        float t = (y - Position.y) / ray.y;
        return Position + ray * t;
    }

    /// <summary>(columna, fila) del mockup donde se ve un punto del mundo.</summary>
    public Vector2 Project(Vector3 world)
    {
        Vector3 rel = world - Position;
        float depth = Vector3.Dot(rel, Forward);
        float f = Focal;
        return new Vector2(
            CourtLayout.MockWidth * 0.5f + f * Vector3.Dot(rel, Right) / depth,
            CourtLayout.MockHeight * 0.5f - f * Vector3.Dot(rel, Up) / depth);
    }

    /// <summary>
    /// Fila del mockup equivalente en nuestra cámara para algo pintado en
    /// el piso de la cancha. El mockup comprime la cancha entre su línea
    /// de fondo (fila 1050) y el borde inferior; acá la línea de fondo cae
    /// más abajo (el aro está a 2.5 m), así que se reescala ese tramo.
    /// </summary>
    public float CourtRow(float mockRow)
    {
        float baselineRow = Project(new Vector3(0f, 0f, CourtLayout.BaselineZ)).y;
        return baselineRow + (mockRow - CourtLayout.MockBaselineRow) *
               (CourtLayout.MockHeight - baselineRow) / (CourtLayout.MockHeight - CourtLayout.MockBaselineRow);
    }

    /// <summary>Punto del piso para algo dibujado en la cancha del mockup (ver <see cref="CourtRow"/>).</summary>
    public Vector3 OnCourt(float column, float mockRow, float y = 0f) =>
        OnGround(column, CourtRow(mockRow), y);

    /// <summary>
    /// Resuelve FOV, posición y pitch:
    ///   - el horizonte fija el pitch (para un FOV dado);
    ///   - fila y radio de la pelota fijan la posición (distancia sobre
    ///     el rayo que pasa por la fila 1600);
    ///   - el FOV se busca por bisección hasta que el centro del aro caiga
    ///     en su fila.
    /// </summary>
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

        // Horizonte: fila = centro − f·tan(pitch) → pitch (negativo = mirando arriba).
        float pitch = Mathf.Atan((half - CourtLayout.HorizonRow) / f) * Mathf.Rad2Deg;
        CourtCamera oriented = new(Vector3.zero, pitch, fov);

        // La pelota se ve en (540, 1600) con radio 150 px: está sobre ese
        // rayo, a la distancia en que su radio proyectado mide eso.
        Vector3 ray = oriented.Ray(CourtLayout.MockWidth * 0.5f, CourtLayout.BallRow);
        float depthPerUnit = Vector3.Dot(ray, oriented.Forward);
        float distance = f * (CourtLayout.BallDiameter * 0.5f) / (CourtLayout.BallRadiusPx * depthPerUnit);

        return new CourtCamera(CourtLayout.BallRest - ray * distance, pitch, fov);
    }
}
