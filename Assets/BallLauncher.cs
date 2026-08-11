using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BallLauncher : MonoBehaviour
{
    [Header("Lanzamiento")]
    [SerializeField] private float throwForceMultiplier = 0.5f;
    [SerializeField] private float maxThrowSpeed = 20f;

    [Header("Seguimiento del dedo (mientras sostiene)")]
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private float depthFromCamera = 8f; // distancia fija en Z respecto a la cámara

    [Header("Efecto (curva)")]
    [SerializeField] private float curveForceMultiplier = 2f;
    [SerializeField] private int velocitySampleFrames = 3; // cuántos frames atrás promediar para la velocidad final

    [Header("Reset")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private float resetDelay = 0.5f;

    private Camera mainCamera;
    private Rigidbody rb;
    private Vector3 startPosition;

    private bool isHolding = false;
    private bool hasLaunched = false;

    private List<Vector3> positionHistory = new List<Vector3>();
    private List<float> angleHistory = new List<float>(); // ángulo del dedo respecto al centro del gesto, por frame
    private Vector2 holdCenter; // centro estimado del movimiento circular en pantalla
    private float accumulatedSpin = 0f; // cuánto "giró" el dedo en total durante el hold

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        startPosition = transform.position;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (hasLaunched) return;
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            StartHold(touch.position.ReadValue());
        }

        if (touch.press.isPressed && isHolding)
        {
            UpdateHold(touch.position.ReadValue());
        }

        if (touch.press.wasReleasedThisFrame && isHolding)
        {
            ReleaseThrow();
        }
    }

    private void StartHold(Vector2 screenPos)
    {
        isHolding = true;
        holdCenter = screenPos;
        accumulatedSpin = 0f;
        positionHistory.Clear();
        angleHistory.Clear();
    }

    private void UpdateHold(Vector2 screenPos)
    {
        // Mueve la pelota siguiendo el dedo en el mundo 3D, a una profundidad fija
        Vector3 worldPos = ScreenToWorld(screenPos);
        transform.position = Vector3.Lerp(transform.position, worldPos, Time.deltaTime * followSpeed);

        // Trackea posición para calcular velocidad al soltar
        positionHistory.Add(transform.position);
        if (positionHistory.Count > velocitySampleFrames)
            positionHistory.RemoveAt(0);

        // Calcula el ángulo actual del dedo respecto al centro del gesto (para detectar movimiento circular)
        Vector2 dirFromCenter = screenPos - holdCenter;
        if (dirFromCenter.sqrMagnitude > 4f) // ignora ruido muy chico
        {
            float currentAngle = Mathf.Atan2(dirFromCenter.y, dirFromCenter.x) * Mathf.Rad2Deg;
            angleHistory.Add(currentAngle);

            if (angleHistory.Count >= 2)
            {
                float deltaAngle = Mathf.DeltaAngle(angleHistory[angleHistory.Count - 2], currentAngle);
                accumulatedSpin += deltaAngle; // suma o resta según hacia dónde gira
            }

            if (angleHistory.Count > 10) angleHistory.RemoveAt(0);
        }
    }

    private void ReleaseThrow()
    {
        isHolding = false;
        hasLaunched = true;
        rb.isKinematic = false;

        Vector3 throwVelocity = Vector3.zero;
        if (positionHistory.Count >= 2)
        {
            Vector3 delta = positionHistory[positionHistory.Count - 1] - positionHistory[0];
            throwVelocity = delta / (Time.deltaTime * positionHistory.Count);
        }

        throwVelocity *= throwForceMultiplier;
        if (throwVelocity.magnitude > maxThrowSpeed)
            throwVelocity = throwVelocity.normalized * maxThrowSpeed;

        rb.linearVelocity = throwVelocity;

        // Solo aplica efecto si el spin acumulado supera un umbral (filtra drags rectos)
        float spinThreshold = 90f; // grados mínimos girados para contar como "efecto"
        float curveStrength = 0f;

        if (Mathf.Abs(accumulatedSpin) > spinThreshold)
        {
            curveStrength = Mathf.Clamp(accumulatedSpin, -720f, 720f) / 720f;
        }

        // Dirección de vuelo normalizada, para calcular la perpendicular correcta
        Vector3 flightDirection = throwVelocity.normalized;
        StartCoroutine(ApplyCurveWhileFlying(curveStrength, flightDirection));
    }

    private System.Collections.IEnumerator ApplyCurveWhileFlying(float curveStrength, Vector3 flightDirection)
    {
        while (!IsGrounded())
        {
            // Perpendicular a la dirección de vuelo actual (en el plano horizontal), no un eje fijo
            Vector3 curveAxis = Vector3.Cross(Vector3.up, flightDirection).normalized;
            rb.AddForce(curveAxis * curveStrength * curveForceMultiplier, ForceMode.Acceleration);
            yield return null;
        }
    }

    private bool grounded = false;
    private bool IsGrounded() => grounded;

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, depthFromCamera);
        return mainCamera.ScreenToWorldPoint(screenPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(groundTag) && hasLaunched)
        {
            grounded = true;
            Invoke(nameof(ResetBall), resetDelay);
        }
    }

    private void ResetBall()
    {
        grounded = false;
        hasLaunched = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
    }
}