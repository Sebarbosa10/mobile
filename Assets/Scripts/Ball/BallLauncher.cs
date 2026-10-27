using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

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

    [Header("Reinicio")]
    [SerializeField]
    private string groundTag = "ground";

    private readonly SwipeVelocityEstimator gestureEstimator = new();

    private Camera mainCamera;
    private Rigidbody rigidbodyComponent;

    private BallHoldPositionConstraint holdPositionConstraint;
    private BallThrowTrajectoryCalculator trajectoryCalculator;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private BallLaunchState state;

    private const float StuckSpeed = 0.15f;
    private const float StuckSeconds = 1.5f;
    private float stuckTime;

    private float holdDepth;

    private Vector2 ballScreenAtGrab;
    private Vector2 touchScreenAtGrab;

    private Vector2 pendingScreenPosition;
    private bool hasPendingMove;

    private readonly List<RaycastResult> uiHits = new();
    private PointerEventData uiPointerData;

    // Audio: con qué tocó la pelota desde el lanzamiento y si ya encestó en este tiro
    private BallSurface lastHoopContact = BallSurface.Floor;
    private bool scoredThisLaunch;

    public event System.Action BallLanded;

    public event System.Action Launched;

    public Vector3 RestPosition =>
        startPosition;

    // Tipo de encestada según lo último que tocó la pelota antes de entrar: aro, tablero o nada
    public ScoreType BasketType => lastHoopContact switch
    {
        BallSurface.Rim => ScoreType.Rim,
        BallSurface.Backboard => ScoreType.Backboard,
        _ => ScoreType.Clean
    };

    public void RegisterBasket()
    {
        scoredThisLaunch = true;
    }

    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
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
        if (state == BallLaunchState.Launched)
        {
            CheckStuck();
            return;
        }

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

        if (mainCamera == null)
            return;

        if (IsOverButton(screenPosition))
            return;

        state = BallLaunchState.Held;

        SetHeldPhysicsState();

        Vector3 ballScreenPoint =
            mainCamera.WorldToScreenPoint(transform.position);

        holdDepth = ballScreenPoint.z;
        ballScreenAtGrab = ballScreenPoint;
        touchScreenAtGrab = screenPosition;

        pendingScreenPosition = ballScreenAtGrab;
        hasPendingMove = false;

        gestureEstimator.Begin(
            screenPosition,
            Time.unscaledTime);

        AudioManager.PlayGrab();
    }

    private bool IsOverButton(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return false;

        uiPointerData ??= new PointerEventData(eventSystem);
        uiPointerData.position = screenPosition;

        uiHits.Clear();
        eventSystem.RaycastAll(uiPointerData, uiHits);

        foreach (RaycastResult hit in uiHits)
        {
            if (hit.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }

    private void MoveHold(Vector2 screenPosition)
    {
        if (state != BallLaunchState.Held)
            return;

        pendingScreenPosition =
            ballScreenAtGrab + (screenPosition - touchScreenAtGrab);
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

        lastHoopContact = BallSurface.Floor;
        scoredThisLaunch = false;

        AudioManager.PlayThrow(
            maxThrowSpeed > 0f ? rigidbodyComponent.linearVelocity.magnitude / maxThrowSpeed : 0.5f);

        Launched?.Invoke();
    }

    public bool IsLaunched =>
        state == BallLaunchState.Launched;

    public void ForceReset()
    {
        ResetBall();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != BallLaunchState.Launched)
            return;

        BallSurface surface = BallSurfaces.Classify(collision.collider, groundTag);

        AudioManager.PlayImpact(surface, collision.relativeVelocity.magnitude);

        if (surface == BallSurface.Rim || surface == BallSurface.Backboard)
            lastHoopContact = surface;

        if (!collision.gameObject.CompareTag(groundTag))
            return;

        Land();
    }

    private void Land()
    {
        if (!scoredThisLaunch)
            AudioManager.PlayMiss();

        ResetBall();

        BallLanded?.Invoke();
    }

    private void CheckStuck()
    {
        if (rigidbodyComponent.linearVelocity.sqrMagnitude > StuckSpeed * StuckSpeed)
        {
            stuckTime = 0f;
            return;
        }

        stuckTime += Time.fixedDeltaTime;

        if (stuckTime < StuckSeconds)
            return;

        Land();
    }

    private void ResetBall()
    {
        if (state == BallLaunchState.Launched)
            AudioManager.PlayRespawn();

        stuckTime = 0f;

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
