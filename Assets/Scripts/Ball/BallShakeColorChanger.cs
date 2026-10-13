using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cambia el color de la pelota cuando detecta un shake del dispositivo,
/// usando el acelerómetro del Input System.
///
/// El shake se detecta comparando la magnitud de la aceleración contra
/// un umbral, restando la gravedad (~1g) para no disparar en reposo.
/// </summary>
[RequireComponent(typeof(Renderer))]
public sealed class BallShakeColorChanger : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Detección de shake")]
    [SerializeField, Min(0f)]
    private float shakeThreshold = 2.5f;

    [SerializeField, Min(0f)]
    private float shakeCooldown = 0.5f;

    [Header("Colores")]
    [SerializeField]
    private Color[] colorPalette;

    private Renderer ballRenderer;
    private MaterialPropertyBlock propertyBlock;

    private float lastShakeTime;
    private int lastColorIndex = -1;

    private void Awake()
    {
        ballRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    private void OnDisable()
    {
        if (Accelerometer.current != null)
            InputSystem.DisableDevice(Accelerometer.current);
    }

    private void Update()
    {
        if (Accelerometer.current == null)
            return;

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();

        float shakeMagnitude = Mathf.Abs(acceleration.magnitude - 1f);

        if (shakeMagnitude < shakeThreshold)
            return;

        if (Time.unscaledTime - lastShakeTime < shakeCooldown)
            return;

        lastShakeTime = Time.unscaledTime;

        ApplyRandomColor();
    }

    private void ApplyRandomColor()
    {
        Color newColor = PickRandomColor();

        ballRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(BaseColorId, newColor);
        propertyBlock.SetColor(ColorId, newColor);

        ballRenderer.SetPropertyBlock(propertyBlock);
    }

    private Color PickRandomColor()
    {
        if (colorPalette == null || colorPalette.Length == 0)
            return Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);

        if (colorPalette.Length == 1)
            return colorPalette[0];

        int index;

        do
        {
            index = Random.Range(0, colorPalette.Length);
        }
        while (index == lastColorIndex);

        lastColorIndex = index;

        return colorPalette[index];
    }
}
