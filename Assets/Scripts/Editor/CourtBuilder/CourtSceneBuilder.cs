using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Build Court Scene
///
/// Genera toda la escena "básquet callejero al atardecer" por código:
///  1. Resuelve la cámara contra el mockup (CourtCamera.Solve).
///  2. Crea/actualiza materiales, meshes y prefabs en Assets/Art/Scene.
///  3. Abre DummyLab y Challenges y les pone el entorno, la pelota y los
///     aros nuevos, reemplazando solo la parte visual: BallLauncher,
///     BallShotClock, HoopController y los desafíos siguen conectados.
///  4. Configura cámara, luz y ambiente, y reporta los triángulos.
///
/// Se puede correr las veces que haga falta: los assets se actualizan en
/// el lugar y las instancias que ya están en las escenas se reutilizan.
/// </summary>
public static class CourtSceneBuilder
{
    private const string EndlessScenePath = "Assets/Scenes/DummyLab.unity";
    private const string ChallengesScenePath = "Assets/Scenes/Challenges.unity";

    private const string EnvironmentName = "CourtEnvironment";
    private const string BallArtName = "BallArt";
    private const string BallShadowName = "BallShadow";
    private const string HoopArtName = "HoopArt";
    private const string CarouselName = "HoopCarousel";

    private sealed class Prefabs
    {
        public GameObject Environment, Hoop, HoopSmall, Carousel, Ball, BallShadow;
    }

    [MenuItem("Tools/Build Court Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string previousScene = SceneManager.GetActiveScene().path;
        StringBuilder report = new();

        try
        {
            Progress("Cámara", 0.02f);
            CourtAssets.EnsureFolders();
            CourtCamera camera = CourtCamera.Solve(out string cameraReport);
            report.AppendLine(cameraReport);

            Progress("Shaders y materiales", 0.08f);
            CourtMaterials materials = CourtMaterials.Build();

            Progress("Cielo", 0.15f);
            GameObject sky = CourtGeometry.BuildSky(camera, materials);

            Progress("Cancha y fondo", 0.25f);
            GameObject court = CourtGeometry.BuildCourt(camera, materials);
            GameObject fence = CourtGeometry.BuildFence(camera, materials);
            GameObject buildings = CourtGeometry.BuildBuildings(camera, materials);
            GameObject tree = CourtGeometry.BuildTree(camera, materials, out Vector3 leftTree, out Vector3 rightTree);

            Prefabs prefabs = new()
            {
                Environment = BuildEnvironment(sky, court, fence, buildings, tree, leftTree, rightTree)
            };

            Progress("Aro", 0.45f);
            Mesh pole = HoopGeometry.PoleMesh();
            Mesh strip = HoopGeometry.ShadowStripMesh();
            prefabs.Hoop = HoopGeometry.BuildHoop(materials, pole, strip);

            Progress("Pelota", 0.55f);
            prefabs.Ball = BallGeometry.BuildBall(materials);
            prefabs.BallShadow = BallGeometry.BuildBallShadow(materials);

            Progress("Carrusel", 0.62f);
            prefabs.HoopSmall = HoopGeometry.BuildHoopSmall(materials);
            prefabs.Carousel = HoopGeometry.BuildCarousel(materials, prefabs.HoopSmall, pole, strip);

            AssetDatabase.SaveAssets();

            Progress("Escena DummyLab", 0.75f);
            report.AppendLine(WireScene(EndlessScenePath, prefabs, camera));

            Progress("Escena Challenges", 0.88f);
            report.AppendLine(WireScene(ChallengesScenePath, prefabs, camera));

            EnableStaticBatching(report);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            if (!string.IsNullOrEmpty(previousScene) && System.IO.File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        Debug.Log("[Build Court Scene]\n" + report);
        EditorUtility.DisplayDialog("Build Court Scene", report.ToString(), "OK");
    }

    private static void Progress(string step, float t) =>
        EditorUtility.DisplayProgressBar("Build Court Scene", step, t);

    // ------------------------------------------------------------ Entorno

    private static GameObject BuildEnvironment(
        GameObject sky, GameObject court, GameObject fence, GameObject buildings, GameObject tree,
        Vector3 leftTree, Vector3 rightTree)
    {
        GameObject root = new(EnvironmentName);

        void Add(GameObject prefab, string name, Vector3 position, Vector3 scale)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = position;
            instance.transform.localScale = scale;
        }

        Add(sky, "Sky", Vector3.zero, Vector3.one);
        Add(court, "Court", Vector3.zero, Vector3.one);
        Add(fence, "Fence", Vector3.zero, Vector3.one);
        Add(buildings, "Buildings", Vector3.zero, Vector3.one);
        Add(tree, "TreeLeft", leftTree, Vector3.one);
        // El árbol derecho del mockup es un poco más ancho: mismo prefab, espejado.
        Add(tree, "TreeRight", rightTree, new Vector3(-1.1f, 1f, 1.1f));

        GameObjectUtility.SetStaticEditorFlags(root,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

        return CourtAssets.SavePrefab(root, EnvironmentName);
    }

    // ------------------------------------------------------------- Escenas

    private static string WireScene(string path, Prefabs prefabs, CourtCamera camera)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        StringBuilder log = new();
        log.AppendLine($"{scene.name}:");

        RemoveLegacyFloor(scene, log);
        RemoveVolumes(scene, log);

        EnsureRootInstance(scene, prefabs.Environment, EnvironmentName);
        SetupCamera(scene, camera);
        SetupLight(scene);
        SetupRenderSettings(scene);
        SetupBall(scene, prefabs, log);

        foreach (HoopController controller in FindAll<HoopController>(scene))
            SetupEndlessHoop(controller, prefabs, log);

        foreach (ChallengeControllerBase challenge in FindAll<ChallengeControllerBase>(scene))
            SetupChallenge(challenge, prefabs, log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        log.Append(TriangleReport(scene));
        return log.ToString();
    }

    private static List<T> FindAll<T>(Scene scene) where T : Component
    {
        List<T> found = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<T>(true));
        return found;
    }

    private static bool IsInstanceOf(GameObject go, GameObject prefab) =>
        go != null &&
        PrefabUtility.IsAnyPrefabInstanceRoot(go) &&
        PrefabUtility.GetCorrespondingObjectFromSource(go) == prefab;

    private static GameObject EnsureRootInstance(Scene scene, GameObject prefab, string name)
    {
        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);

        if (IsInstanceOf(existing, prefab))
            return existing;

        if (existing != null)
            Object.DestroyImmediate(existing);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = name;
        return instance;
    }

    private static GameObject EnsureChildInstance(Transform parent, GameObject prefab, string name)
    {
        Transform existing = parent.Find(name);

        if (existing != null && IsInstanceOf(existing.gameObject, prefab))
        {
            PrefabUtility.RemoveUnusedOverrides(new[] { existing.gameObject }, InteractionMode.AutomatedAction);
            return existing.gameObject;
        }

        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    /// <summary>El piso viejo (plano o cubo con tag "ground") lo reemplaza el collider de Court.</summary>
    private static void RemoveLegacyFloor(Scene scene, StringBuilder log)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == EnvironmentName || !root.CompareTag("ground"))
                continue;

            log.AppendLine($"  - se quitó el piso anterior \"{root.name}\"");
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>Sin post-procesado: se quitan los Volume de la escena.</summary>
    private static void RemoveVolumes(Scene scene, StringBuilder log)
    {
        foreach (Volume volume in FindAll<Volume>(scene))
        {
            log.AppendLine($"  - se quitó el Volume \"{volume.name}\" (sin post-procesado)");
            Object.DestroyImmediate(volume.gameObject);
        }
    }

    private static void SetupCamera(Scene scene, CourtCamera solved)
    {
        List<Camera> cameras = FindAll<Camera>(scene);
        Camera camera = cameras.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? cameras.FirstOrDefault();

        if (camera == null)
        {
            camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        }

        camera.transform.SetPositionAndRotation(solved.Position, solved.Rotation);
        camera.fieldOfView = solved.VerticalFov;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 400f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = CourtMaterials.CieloHorizonte;
        camera.allowHDR = false;

        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.antialiasing = AntialiasingMode.None; // los bordes los suaviza el MSAA del URP Asset

        // FOV horizontal constante: en 9:20 / 9:21 se gana cielo arriba y piso abajo.
        if (!camera.TryGetComponent(out CameraAspectFit fit))
            fit = camera.gameObject.AddComponent<CameraAspectFit>();

        SerializedObject so = new(fit);
        so.FindProperty("referenceAspect").floatValue = CourtLayout.MockWidth / CourtLayout.MockHeight;
        so.FindProperty("referenceVerticalFov").floatValue = solved.VerticalFov;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupLight(Scene scene)
    {
        List<Light> lights = FindAll<Light>(scene);
        Light sun = lights.FirstOrDefault(l => l.type == LightType.Directional);

        if (sun == null)
        {
            sun = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            SceneManager.MoveGameObjectToScene(sun.gameObject, scene);
        }

        // Una sola luz en tiempo real: cualquier otra se apaga.
        foreach (Light other in lights)
            if (other != sun)
                other.enabled = false;

        sun.transform.rotation = CourtLayout.LightRotation;
        sun.color = CourtMaterials.LuzSol;
        sun.intensity = 1f;
        sun.shadows = LightShadows.None;
        sun.lightmapBakeType = LightmapBakeType.Realtime;
        RenderSettings.sun = sun;
    }

    private static void SetupRenderSettings(Scene scene)
    {
        SceneManager.SetActiveScene(scene);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = CourtMaterials.Ambiente;
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
    }

    private static void SetupBall(Scene scene, Prefabs prefabs, StringBuilder log)
    {
        BallLauncher launcher = FindAll<BallLauncher>(scene).FirstOrDefault();

        if (launcher == null)
        {
            log.AppendLine("  ! no hay BallLauncher en la escena");
            return;
        }

        GameObject ball = launcher.gameObject;

        int missing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(ball);
        if (missing > 0)
            log.AppendLine($"  - se quitaron {missing} scripts faltantes de la pelota (BallShakeColorChanger revertido; lo reemplaza BallColor)");

        // La esfera de Unity queda apagada: la ve el prefab Ball. El
        // SphereCollider y el Rigidbody siguen siendo los de siempre.
        if (ball.TryGetComponent(out MeshRenderer oldRenderer))
            oldRenderer.enabled = false;

        EnsureChildInstance(ball.transform, prefabs.Ball, BallArtName);

        GameObject shadow = EnsureRootInstance(scene, prefabs.BallShadow, BallShadowName);
        SerializedObject so = new(shadow.GetComponent<BallBlobShadow>());
        so.FindProperty("target").objectReferenceValue = ball.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        shadow.transform.position = new Vector3(ball.transform.position.x, 0.012f, ball.transform.position.z);
    }

    /// <summary>
    /// Endless: el GameObject del HoopController (con HoopMovement) queda
    /// como raíz, ubicado en el centro del aro; su parte visual pasa a ser
    /// una instancia del prefab Hoop.
    /// </summary>
    private static void SetupEndlessHoop(HoopController controller, Prefabs prefabs, StringBuilder log)
    {
        Transform root = controller.transform;
        SerializedObject so = new(controller);
        Transform art = root.Find(HoopArtName);

        if (art == null || !IsInstanceOf(art.gameObject, prefabs.Hoop))
        {
            Transform oldTrigger = so.FindProperty("scoreTriggerRoot").objectReferenceValue as Transform;
            Vector3 rim = oldTrigger != null ? oldTrigger.position : CourtLayout.RimCenter;

            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            root.SetPositionAndRotation(rim, Quaternion.identity);
            log.AppendLine($"  - aro del endless reemplazado (centro del aro en {rim})");
        }

        GameObject artObject = EnsureChildInstance(root, prefabs.Hoop, HoopArtName);
        HoopVisual visual = artObject.GetComponent<HoopVisual>();
        HoopScoreTrigger trigger = artObject.GetComponentInChildren<HoopScoreTrigger>(true);

        so.FindProperty("hoopVisual").objectReferenceValue = visual;
        so.FindProperty("visualRoot").objectReferenceValue = artObject.transform.Find("Scaled");
        so.FindProperty("scoreTriggerRoot").objectReferenceValue = trigger.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject triggerSo = new(trigger);
        triggerSo.FindProperty("hoopController").objectReferenceValue = controller;
        triggerSo.ApplyModifiedPropertiesWithoutUndo();

        visual.SnapPoleToGround();
    }

    private static void SetupChallenge(ChallengeControllerBase challenge, Prefabs prefabs, StringBuilder log)
    {
        SerializedObject so = new(challenge);
        SerializedProperty hoops = so.FindProperty("hoops");
        HoopScoreTrigger first = hoops.arraySize > 0 ? hoops.GetArrayElementAtIndex(0).objectReferenceValue as HoopScoreTrigger : null;
        Transform challengeRoot = challenge.transform.parent != null ? challenge.transform.parent : challenge.transform;

        if (challenge is RingChallengeController)
        {
            ChallengeCarousel carousel = first != null
                ? first.GetComponentInParent<ChallengeCarousel>(true)
                : challengeRoot.GetComponentInChildren<ChallengeCarousel>(true);

            GameObject carouselObject;

            if (carousel != null && IsInstanceOf(carousel.gameObject, prefabs.Carousel))
            {
                carouselObject = carousel.gameObject;
                PrefabUtility.RemoveUnusedOverrides(new[] { carouselObject }, InteractionMode.AutomatedAction);
            }
            else
            {
                Vector3 position = carousel != null ? carousel.transform.position : CourtLayout.RimCenter;
                Transform parent = carousel != null ? carousel.transform.parent : challengeRoot;

                if (carousel != null)
                    Object.DestroyImmediate(carousel.gameObject);

                carouselObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Carousel, parent);
                carouselObject.name = CarouselName;
                carouselObject.transform.SetPositionAndRotation(position, Quaternion.identity);
                log.AppendLine($"  - Carrusel reemplazado por {CarouselName}");
            }

            List<Object> triggers = new();
            for (int i = 1; i <= 5; i++)
            {
                Transform hoop = carouselObject.transform.Find($"Hoop{i}");
                if (hoop != null)
                    triggers.Add(hoop.GetComponentInChildren<HoopScoreTrigger>(true));
            }

            HoopGeometry.SetArray(hoops, triggers.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            return;
        }

        // Aro esquivo y Contrarreloj: un aro grande colgado del objeto que
        // lo mueve (EvasiveHoopMover) o lo sostiene.
        Transform hoopRoot = null;

        if (first != null)
        {
            HoopVisual existingVisual = first.GetComponentInParent<HoopVisual>(true);
            hoopRoot = existingVisual != null ? existingVisual.transform.parent : first.transform.parent;
        }

        if (hoopRoot == null)
        {
            hoopRoot = CourtAssets.Empty("Hoop", challengeRoot, CourtLayout.RimCenter).transform;
            log.AppendLine($"  ! {challenge.GetType().Name} no tenía aro: se creó uno en {CourtLayout.RimCenter}");
        }

        Transform art = hoopRoot.Find(HoopArtName);
        if (art == null || !IsInstanceOf(art.gameObject, prefabs.Hoop))
        {
            for (int i = hoopRoot.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(hoopRoot.GetChild(i).gameObject);

            log.AppendLine($"  - aro de {challenge.GetType().Name} ({hoopRoot.name}) reemplazado");
        }

        GameObject artObject = EnsureChildInstance(hoopRoot, prefabs.Hoop, HoopArtName);
        HoopScoreTrigger trigger = artObject.GetComponentInChildren<HoopScoreTrigger>(true);
        HoopGeometry.SetArray(hoops, trigger);
        so.ApplyModifiedPropertiesWithoutUndo();

        artObject.GetComponent<HoopVisual>().SnapPoleToGround();
    }

    // -------------------------------------------------------- Triángulos

    /// <summary>
    /// Cuenta triángulos de los renderers de la escena (los que usan los
    /// shaders Court/*). En Challenges solo uno de los tres desafíos está
    /// activo a la vez: se informa la base más cada desafío.
    /// </summary>
    private static string TriangleReport(Scene scene)
    {
        List<ChallengeControllerBase> challenges = FindAll<ChallengeControllerBase>(scene);
        HashSet<Transform> challengeRoots = new(challenges.Select(c => c.transform.parent).Where(p => p != null));

        int baseCount = 0;
        Dictionary<Transform, int> perChallenge = challengeRoots.ToDictionary(r => r, _ => 0);

        foreach (MeshRenderer renderer in FindAll<MeshRenderer>(scene))
        {
            if (!renderer.sharedMaterials.Any(mat => mat != null && mat.shader != null && mat.shader.name.StartsWith("Court/")))
                continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            int triangles = 0;
            for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
                triangles += (int)filter.sharedMesh.GetIndexCount(i) / 3;

            Transform owner = challengeRoots.FirstOrDefault(r => renderer.transform.IsChildOf(r));

            if (owner != null)
                perChallenge[owner] += triangles;
            else
                baseCount += triangles;
        }

        if (perChallenge.Count == 0)
            return $"  triángulos: {baseCount:N0}\n";

        StringBuilder text = new($"  triángulos: base {baseCount:N0}");
        foreach (KeyValuePair<Transform, int> pair in perChallenge.OrderBy(p => p.Key.name))
            text.Append($" · {pair.Key.name} +{pair.Value:N0} = {baseCount + pair.Value:N0}");
        text.AppendLine();
        return text.ToString();
    }

    // ---------------------------------------------------- Static batching

    /// <summary>
    /// Static batching para Android e iOS. La API pública no lo expone en
    /// todas las versiones; se intenta por reflexión y, si no está, se
    /// avisa para activarlo a mano en Player Settings.
    /// </summary>
    private static void EnableStaticBatching(StringBuilder report)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        BuildTarget[] targets = { BuildTarget.Android, BuildTarget.iOS };

        MethodInfo setStatic = typeof(PlayerSettings).GetMethod("SetStaticBatchingForPlatform", flags);
        if (setStatic != null && setStatic.GetParameters().Length == 2)
        {
            foreach (BuildTarget target in targets)
                setStatic.Invoke(null, new object[] { target, true });

            report.AppendLine("Static batching activado (Android, iOS).");
            return;
        }

        MethodInfo setBatching = typeof(PlayerSettings).GetMethod("SetBatchingForPlatform", flags);
        if (setBatching != null && setBatching.GetParameters().Length == 3)
        {
            foreach (BuildTarget target in targets)
                setBatching.Invoke(null, new object[] { target, 1, 0 });

            report.AppendLine("Static batching activado (Android, iOS).");
            return;
        }

        report.AppendLine("! No se pudo activar static batching por código: revisalo en Player Settings > Other Settings.");
    }
}
