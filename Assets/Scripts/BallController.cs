using UnityEngine;

/// <summary>
/// Punto de composición para el comportamiento alternativo de salto y wrap.
/// La entrada, el salto y el límite de pantalla se delegan a componentes dedicados.
/// </summary>
public sealed class BallController : MonoBehaviour
{
    [Header("Física")]
    [SerializeField, Min(0f)] private float jumpForce = 10f;

    [Header("Pantalla")]
    [SerializeField, Min(0f)] private float screenMargin = 0.5f;
    [SerializeField, Min(0f)] private float distanceFromCamera = 10f;

    private BallTapInput tapInput;
    private BallJumpMotor jumpMotor;
    private BallScreenWrapController screenWrapController;
    private Camera mainCamera;

    private void Awake()
    {
        tapInput = GetOrAddComponent<BallTapInput>();
        jumpMotor = GetOrAddComponent<BallJumpMotor>();
        screenWrapController = GetOrAddComponent<BallScreenWrapController>();
        mainCamera ??= Camera.main;

        jumpMotor.Configure(jumpForce);
        screenWrapController.Configure(mainCamera, screenMargin, distanceFromCamera);
    }

    private void OnEnable()
    {
        if (tapInput != null)
            tapInput.Tapped += HandleTap;
    }

    private void OnDisable()
    {
        if (tapInput != null)
            tapInput.Tapped -= HandleTap;
    }

    private void HandleTap()
    {
        jumpMotor.Jump();
        screenWrapController.EnableWrapping();
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
