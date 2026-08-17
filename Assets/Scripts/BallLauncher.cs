using UnityEngine;

/// <summary>
/// Orquesta el estado de una pelota: sostenida, lanzada y reiniciada.
/// La entrada, la trayectoria y las restricciones físicas se delegan a colaboradores.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(BallHoldPositionConstraint))]
[RequireComponent(typeof(BallTouchInput))]
[RequireComponent(typeof(BallTouchSelectionValidator))]
public sealed class BallLauncher : MonoBehaviour
{
    [Header("Lanzamiento")]
    [SerializeField, Min(0f)] private float throwForceMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float maxThrowSpeed = 20f;
    [SerializeField, Min(1f)] private float swipeSpeedForMaxThrow = 1200f;
    [SerializeField, Min(0f)] private float upwardArc = 0.7f;

    [Header("Reinicio")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField, Min(0f)] private float resetDelay = 0.5f;

    private readonly SwipeVelocityEstimator gestureEstimator = new();
    private Camera mainCamera;
    private Rigidbody rigidbodyComponent;
    private BallHoldPositionConstraint holdPositionConstraint;
    private BallTouchSelectionValidator selectionValidator;
    private BallThrowTrajectoryCalculator trajectoryCalculator;
    private IBallGestureSource gestureSource;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private BallLaunchState state;
    private float holdDepth;
    private Vector2 pendingScreenPosition;
    private bool hasPendingMove;

    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        holdPositionConstraint = GetComponent<BallHoldPositionConstraint>();
        selectionValidator = GetComponent<BallTouchSelectionValidator>();
        gestureSource = GetComponent<BallTouchInput>();
        mainCamera = Camera.main;

        trajectoryCalculator = new BallThrowTrajectoryCalculator(
            maxThrowSpeed,
            throwForceMultiplier,
            swipeSpeedForMaxThrow,
            upwardArc);
        startPosition = transform.position;
        startRotation = transform.rotation;
        rigidbodyComponent.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        SetHeldPhysicsState();
    }

    private void OnEnable()
    {
        if (gestureSource == null)
            return;

        gestureSource.GestureStarted += BeginHold;
        gestureSource.GestureMoved += MoveHold;
        gestureSource.GestureReleased += ReleaseThrow;
    }

    private void OnDisable()
    {
        if (gestureSource == null)
            return;

        gestureSource.GestureStarted -= BeginHold;
        gestureSource.GestureMoved -= MoveHold;
        gestureSource.GestureReleased -= ReleaseThrow;
    }

    private void BeginHold(Vector2 screenPosition)
    {
        if (state == BallLaunchState.Launched
            || !selectionValidator.IsSelected(mainCamera, screenPosition))
            return;

        CancelInvoke(nameof(ResetBall));
        state = BallLaunchState.Held;
        SetHeldPhysicsState();
        // Se usa la profundidad real de la pelota respecto a la cámara en el instante
        // del agarre (no un valor fijo) para que no "salte" hacia adelante o atrás.
        holdDepth = mainCamera.WorldToScreenPoint(transform.position).z;
        hasPendingMove = false;
        gestureEstimator.Begin(screenPosition, Time.unscaledTime);
    }

    private void MoveHold(Vector2 screenPosition)
    {
        if (state != BallLaunchState.Held)
            return;

        // El movimiento del Rigidbody se aplica en FixedUpdate, no acá. Esto evita
        // llamar a MovePosition y a la resolución de colisiones (costosa: overlaps +
        // ComputePenetration) más veces por segundo de las que la física realmente
        // procesa, y mantiene el movimiento sincronizado con el paso físico.
        pendingScreenPosition = screenPosition;
        hasPendingMove = true;
        gestureEstimator.Move(screenPosition);
    }

    private void FixedUpdate()
    {
        if (state != BallLaunchState.Held || !hasPendingMove)
            return;

        Vector3 safePosition = holdPositionConstraint.Constrain(ScreenToWorld(pendingScreenPosition));
        rigidbodyComponent.MovePosition(safePosition);
        hasPendingMove = false;
    }

    private void ReleaseThrow(Vector2 screenPosition)
    {
        if (state != BallLaunchState.Held)
            return;

        SwipeReleaseGesture gesture = gestureEstimator.Release(screenPosition, Time.unscaledTime);
        state = BallLaunchState.Launched;
        rigidbodyComponent.isKinematic = false;
        rigidbodyComponent.useGravity = true;
        rigidbodyComponent.linearVelocity = trajectoryCalculator.Calculate(gesture, mainCamera);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != BallLaunchState.Launched || !collision.gameObject.CompareTag(groundTag))
            return;

        state = BallLaunchState.ResetScheduled;
        Invoke(nameof(ResetBall), resetDelay);
    }

    private void ResetBall()
    {
        if (state != BallLaunchState.ResetScheduled)
            return;

        transform.SetPositionAndRotation(startPosition, startRotation);
        SetHeldPhysicsState();
        state = BallLaunchState.Ready;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        return mainCamera.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            holdDepth));
    }

    private void SetHeldPhysicsState()
    {
        // Unity no permite escribir velocidades en cuerpos cinemáticos. Al volver
        // desde vuelo se limpian antes de cambiar al estado de agarre.
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