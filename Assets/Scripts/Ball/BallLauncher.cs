using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Controla el ciclo completo de interacción de lanzamiento:
///
/// Ready
///   ↓
/// Held
///   ↓
/// Launched
///   ↓
/// Ready
///
/// Gestiona el input táctil, selección de la pelota,
/// movimiento durante el arrastre, lanzamiento y reinicio.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(BallHoldPositionConstraint))]
public sealed class BallLauncher : MonoBehaviour
{
    [Header("Lanzamiento")]
    [SerializeField, Min(0f)]
    private float throwForceMultiplier = 0.5f;

    [SerializeField, Min(0f)]
    private float maxThrowSpeed = 20f;

    [SerializeField, Min(1f)]
    private float swipeSpeedForMaxThrow = 1200f;

    [SerializeField, Min(0f)]
    private float upwardArc = 0.7f;

    [SerializeField, Range(0f, 90f)]
    private float maxAimDeviationDegrees = 40f;

    [Header("Selección")]
    [SerializeField, Min(0.1f)]
    private float raycastDistance = 100f;

    [SerializeField]
    private LayerMask selectableLayers = Physics.DefaultRaycastLayers;

    [Header("Reinicio")]
    [SerializeField]
    private string groundTag = "ground";

    private readonly SwipeVelocityEstimator gestureEstimator = new();

    private Camera mainCamera;
    private Rigidbody rigidbodyComponent;
    private Collider ballCollider;

    private BallHoldPositionConstraint holdPositionConstraint;
    private BallThrowTrajectoryCalculator trajectoryCalculator;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private BallLaunchState state;

    private float holdDepth;

    private Vector2 pendingScreenPosition;
    private bool hasPendingMove;

    /// <summary>
    /// Se dispara apenas la pelota toca el suelo tras un lanzamiento
    /// (haya o no encestado) y se reinicia a la posición de partida.
    /// Lo usa BallShotClock para saber cuándo un tiro que quedó
    /// pendiente de resolver (el timer llegó a cero mientras estaba
    /// en el aire) terminó siendo un fallo.
    /// </summary>
    public event System.Action BallLanded;

    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
        holdPositionConstraint = GetComponent<BallHoldPositionConstraint>();

        mainCamera = Camera.main;

        trajectoryCalculator = new BallThrowTrajectoryCalculator(
            maxThrowSpeed,
            throwForceMultiplier,
            swipeSpeedForMaxThrow,
            upwardArc,
            maxAimDeviationDegrees);

        startPosition = transform.position;
        startRotation = transform.rotation;

        rigidbodyComponent.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        /*
         * Sin interpolación, un Rigidbody kinematic movido con
         * MovePosition en FixedUpdate se ve "a los saltos" en
         * pantallas con refresh rate más alto que el physics
         * timestep. Esto es lo que hace que el drag se sienta tosco.
         */
        rigidbodyComponent.interpolation =
            RigidbodyInterpolation.Interpolate;

        SetHeldPhysicsState();

        state = BallLaunchState.Ready;
    }

    private void Update()
    {
        if (Touchscreen.current == null)
            return;

        TouchControl touch = Touchscreen.current.primaryTouch;

        Vector2 screenPosition = touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
        {
            BeginHold(screenPosition);
        }

        if (touch.press.isPressed)
        {
            MoveHold(screenPosition);
        }

        if (touch.press.wasReleasedThisFrame)
        {
            ReleaseThrow(screenPosition);
        }
    }

    private void FixedUpdate()
    {
        if (state != BallLaunchState.Held)
            return;

        if (!hasPendingMove)
            return;

        Vector3 desiredPosition = ScreenToWorld(pendingScreenPosition);

        Vector3 safePosition =
            holdPositionConstraint.Constrain(desiredPosition);

        rigidbodyComponent.MovePosition(safePosition);

        hasPendingMove = false;
    }

    private void BeginHold(Vector2 screenPosition)
    {
        if (state == BallLaunchState.Launched)
            return;

        if (!IsSelected(screenPosition))
            return;

        state = BallLaunchState.Held;

        SetHeldPhysicsState();

        holdDepth = mainCamera.WorldToScreenPoint(
            transform.position).z;

        pendingScreenPosition = screenPosition;
        hasPendingMove = false;

        gestureEstimator.Begin(
            screenPosition,
            Time.unscaledTime);
    }

    private void MoveHold(Vector2 screenPosition)
    {
        if (state != BallLaunchState.Held)
            return;

        pendingScreenPosition = screenPosition;
        hasPendingMove = true;

        gestureEstimator.Move(screenPosition, Time.unscaledTime);
    }

    private void ReleaseThrow(Vector2 screenPosition)
    {
        if (state != BallLaunchState.Held)
            return;

        SwipeReleaseGesture gesture =
            gestureEstimator.Release(
                screenPosition,
                Time.unscaledTime);

        state = BallLaunchState.Launched;

        rigidbodyComponent.isKinematic = false;
        rigidbodyComponent.useGravity = true;

        rigidbodyComponent.linearVelocity =
            trajectoryCalculator.Calculate(
                gesture,
                mainCamera);
    }

    private bool IsSelected(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            raycastDistance,
            selectableLayers,
            QueryTriggerInteraction.Ignore);

        return hitSomething && hit.collider == ballCollider;
    }

    /// <summary>
    /// True mientras la pelota está en el aire después de un lanzamiento.
    /// Lo usa BallShotClock para saber si, cuando el timer llega a cero,
    /// hay un tiro en curso que todavía puede encestar.
    /// </summary>
    public bool IsLaunched =>
        state == BallLaunchState.Launched;

    /// <summary>
    /// Fuerza el reinicio inmediato de la pelota sin importar el
    /// estado actual (agarrada, en vuelo, etc.). Se usa para cortar
    /// un intento en seco, por ejemplo al perder la partida.
    /// </summary>
    public void ForceReset()
    {
        ResetBall();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != BallLaunchState.Launched)
            return;

        if (!collision.gameObject.CompareTag(groundTag))
            return;

        ResetBall();

        BallLanded?.Invoke();
    }

    private void ResetBall()
    {
        transform.SetPositionAndRotation(
            startPosition,
            startRotation);

        SetHeldPhysicsState();

        state = BallLaunchState.Ready;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        return mainCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                holdDepth));
    }

    private void SetHeldPhysicsState()
    {
        if (!rigidbodyComponent.isKinematic)
        {
            rigidbodyComponent.linearVelocity = Vector3.zero;
            rigidbodyComponent.angularVelocity = Vector3.zero;
        }

        rigidbodyComponent.useGravity = false;
        rigidbodyComponent.isKinematic = true;
    }

    private enum BallLaunchState
    {
        Ready,
        Held,
        Launched
    }
}
