using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public sealed class ChainNet : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float duration = 1.1f;

    [SerializeField, Min(0f)]
    private float frequency = 15f;

    [SerializeField, Min(0f)]
    private float damping = 4.5f;

    [Tooltip("Cuánto se infla hacia afuera (metros, en la punta de abajo).")]
    [SerializeField, Min(0f)]
    private float swell = 0.07f;

    [Tooltip("Cuánto baja la red al pasar la pelota (metros).")]
    [SerializeField, Min(0f)]
    private float drop = 0.12f;

    [Tooltip("Balanceo lateral (metros).")]
    [SerializeField, Min(0f)]
    private float sway = 0.05f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] vertices;
    private Vector3[] radial;
    private float[] weights;
    private float time;
    private bool playing;

    private void Awake()
    {
        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter.sharedMesh == null)
        {
            enabled = false;
            return;
        }

        mesh = Instantiate(filter.sharedMesh);
        mesh.name = filter.sharedMesh.name + " (animada)";
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;

        baseVertices = mesh.vertices;
        vertices = new Vector3[baseVertices.Length];
        radial = new Vector3[baseVertices.Length];
        weights = new float[baseVertices.Length];

        float top = float.MinValue;
        float bottom = float.MaxValue;

        foreach (Vector3 v in baseVertices)
        {
            top = Mathf.Max(top, v.y);
            bottom = Mathf.Min(bottom, v.y);
        }

        float depth = Mathf.Max(0.001f, top - bottom);

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i];
            weights[i] = Mathf.Pow(Mathf.Clamp01((top - v.y) / depth), 1.3f);
            Vector3 flat = new(v.x, 0f, v.z);
            radial[i] = flat.sqrMagnitude > 1e-8f ? flat.normalized : Vector3.zero;
        }

        Bounds bounds = mesh.bounds;
        bounds.Expand(new Vector3(swell + sway, drop, swell + sway) * 2f);
        mesh.bounds = bounds;

        enabled = false;
    }

    public void Shake()
    {
        if (mesh == null)
            return;

        time = 0f;
        playing = true;
        enabled = true;
    }

    private void Update()
    {
        if (!playing)
        {
            enabled = false;
            return;
        }

        time += Time.deltaTime;

        if (time >= duration)
        {
            mesh.vertices = baseVertices;
            playing = false;
            enabled = false;
            return;
        }

        float envelope = Mathf.Exp(-damping * time);
        float wave = Mathf.Sin(time * frequency) * envelope;
        float swing = Mathf.Sin(time * frequency * 0.6f + 1.3f) * envelope;
        float fall = Mathf.Max(0f, wave);

        for (int i = 0; i < baseVertices.Length; i++)
        {
            float w = weights[i];
            vertices[i] = baseVertices[i]
                          + radial[i] * (swell * wave * w)
                          + Vector3.down * (drop * fall * w)
                          + Vector3.right * (sway * swing * w * w);
        }

        mesh.vertices = vertices;
    }
}
