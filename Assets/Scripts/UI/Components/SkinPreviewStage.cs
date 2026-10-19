using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class SkinPreviewStage : MonoBehaviour
{
    public const int Size = 300;

    private const int PreviewLayer = 31;
    private const float SpinSpeed = 25f;
    private static readonly Vector3 Origin = new(0f, -500f, 0f);

    private Camera stageCamera;
    private Transform spinner;
    private GameObject ball;
    private GameObject shadow;
    private BallSkin current;

    public RenderTexture Texture { get; private set; }

    public static SkinPreviewStage Create(Color background)
    {
        GameObject root = new("SkinPreviewStage");
        root.transform.position = Origin;

        SkinPreviewStage stage = root.AddComponent<SkinPreviewStage>();

        stage.Texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32)
        {
            name = "SkinPreview",
            antiAliasing = 4
        };
        stage.Texture.Create();

        stage.stageCamera = new GameObject("PreviewCamera").AddComponent<Camera>();
        stage.stageCamera.transform.SetParent(root.transform, false);
        stage.stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stage.stageCamera.backgroundColor = background;
        stage.stageCamera.cullingMask = 1 << PreviewLayer;
        stage.stageCamera.fieldOfView = 28f;
        stage.stageCamera.nearClipPlane = 0.1f;
        stage.stageCamera.farClipPlane = 20f;
        stage.stageCamera.allowHDR = false;
        stage.stageCamera.targetTexture = stage.Texture;

        UniversalAdditionalCameraData data = stage.stageCamera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.renderShadows = false;

        Light light = new GameObject("PreviewLight").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.transform.rotation = Quaternion.Euler(40f, 25f, 0f);
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.88f);
        light.intensity = 1f;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << PreviewLayer;

        stage.spinner = new GameObject("Spinner").transform;
        stage.spinner.SetParent(root.transform, false);

        SkinDatabase db = SkinDatabase.Instance;

        if (db != null && db.previewShadowPrefab != null)
        {
            stage.shadow = Instantiate(db.previewShadowPrefab, root.transform);
            stage.shadow.name = "PreviewShadow";

            if (stage.shadow.TryGetComponent(out BallBlobShadow blob))
                Destroy(blob);

            stage.shadow.transform.SetLocalPositionAndRotation(new Vector3(0.04f, -0.56f, 0f), Quaternion.identity);
            stage.shadow.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        }

        if (Camera.main != null)
            Camera.main.cullingMask &= ~(1 << PreviewLayer);

        SetLayer(root, PreviewLayer);
        return stage;
    }

    public void Show(BallSkin skin)
    {
        if (skin == current && ball != null)
            return;

        current = skin;

        if (ball != null)
            Destroy(ball);

        if (skin == null || skin.prefab == null)
            return;

        ball = Instantiate(skin.prefab, spinner);
        ball.name = "PreviewBall";
        ball.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        SetLayer(ball, PreviewLayer);

        Frame();
    }

    private void Frame()
    {
        float width = 1.05f;

        foreach (Renderer r in ball.GetComponentsInChildren<Renderer>())
            width = Mathf.Max(width, r.bounds.size.x);

        float visible = Mathf.Max(1.3f, width * 1.06f);
        float distance = visible * 0.5f / Mathf.Tan(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        stageCamera.transform.localPosition = new Vector3(0f, -0.06f, -distance);
        stageCamera.transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        spinner.localRotation = Quaternion.Euler(0f, Time.unscaledTime * SpinSpeed, 0f);
    }

    private void OnEnable()
    {
        if (stageCamera != null)
            stageCamera.enabled = true;
    }

    private void OnDisable()
    {
        if (stageCamera != null)
            stageCamera.enabled = false;
    }

    private void OnDestroy()
    {
        if (Texture != null)
        {
            Texture.Release();
            Destroy(Texture);
        }
    }

    private static void SetLayer(GameObject root, int layer)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
