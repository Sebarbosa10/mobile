using UnityEngine;

public sealed class BallSparkles : MonoBehaviour
{
    [SerializeField]
    private Transform[] sparkles = System.Array.Empty<Transform>();

    [SerializeField, Min(0.1f)]
    private float period = 1.6f;

    [SerializeField, Range(0f, 1f)]
    private float minScale = 0.15f;

    private Vector3[] baseScales;

    private void Awake()
    {
        baseScales = new Vector3[sparkles.Length];

        for (int i = 0; i < sparkles.Length; i++)
        {
            if (sparkles[i] != null)
                baseScales[i] = sparkles[i].localScale;
        }
    }

    private void LateUpdate()
    {
        float time = Time.unscaledTime;

        for (int i = 0; i < sparkles.Length; i++)
        {
            Transform sparkle = sparkles[i];

            if (sparkle == null)
                continue;

            float phase = (time / period + i * 0.37f) % 1f;
            float pulse = Mathf.Max(minScale, Mathf.Sin(phase * Mathf.PI));

            sparkle.rotation = Quaternion.Euler(0f, 0f, time * 40f + i * 25f);
            sparkle.localScale = baseScales[i] * pulse;
        }
    }
}
