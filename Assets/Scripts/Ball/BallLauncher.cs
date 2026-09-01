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
/// ResetScheduled
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

    [Header("Selección")]
    [SerializeField, Min(0.1f)]
    private float raycastDistance = 100f;

    [SerializeField]
    private LayerMask selectableLayers = Physics.DefaultRaycastLayers;

    [Header("Reinicio")]
    [SerializeField]
    private string groundTag = "Ground";

    [SerializeField, Min(0f)]
    private float resetDelay = 0.5f;

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
            upwardArc);

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

        CancelInvoke(nameof(ResetBall));

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
    /// True cuando la pelota está lista para ser agarrada.
    /// Usado por sistemas externos (por ejemplo, un reloj de intento)
    /// que necesitan saber cuándo empieza un intento nuevo.
    /// </summary>
    public bool IsReady =>
        state == BallLaunchState.Ready;

    /// <summary>
    /// Fuerza el reinicio inmediato de la pelota sin importar el
    /// estado actual (agarrada, en vuelo, etc.). A diferencia del
    /// reinicio normal por contacto con el suelo, este no espera
    /// resetDelay: se usa para cortar un intento en seco, por ejemplo
    /// cuando se agota el tiempo disponible.
    /// </summary>
    public void ForceReset()
    {
        CancelInvoke(nameof(ResetBall));

        state = BallLaunchState.ResetScheduled;

        ResetBall();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != BallLaunchState.Launched)
            return;

        if (!collision.gameObject.CompareTag(groundTag))
            return;

        state = BallLaunchState.ResetScheduled;

        Invoke(
            nameof(ResetBall),
            resetDelay);
    }

    private void ResetBall()
    {
        if (state != BallLaunchState.ResetScheduled)
            return;

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
        Launched,
        ResetScheduled
    }
}