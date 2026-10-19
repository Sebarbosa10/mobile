using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el comportamiento simple de la pelota:
/// - Detecta un tap.
/// - Aplica el salto.
/// - Habilita el wrap vertical de pantalla.
///
/// Este componente no controla el lanzamiento por swipe.
/// Ese comportamiento pertenece a BallLauncher.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class BallController : MonoBehaviour
{
    [Header("Salto")]
    [SerializeField, Min(0f)]
    private float jumpForce = 10f;

    [Header("Screen Wrap")]
    [SerializeField, Min(0f)]
    private float screenMargin = 0.5f;

    [SerializeField, Min(0f)]
    private float distanceFromCamera = 10f;

    private Rigidbody rigidbodyComponent;
    private Camera mainCamera;

    private float screenTopY;
    private float screenBottomY;

    private bool wrappingEnabled;

    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        CalculateScreenBounds();
    }

    private void Update()
    {
        HandleTap();
        HandleScreenWrap();
    }

    private void HandleTap()
    {
        if (Pointer.current == null)
            return;

        if (!Pointer.current.press.wasPressedThisFrame)
            return;

        Jump();
        wrappingEnabled = true;
    }

    private void Jump()
    {
        rigidbodyComponent.isKinematic = false;

        Vector3 velocity = rigidbodyComponent.linearVelocity;
        velocity.y = 0f;

        rigidbodyComponent.linearVelocity = velocity;

        rigidbodyComponent.AddForce(
            Vector3.up * jumpForce,
            ForceMode.Impulse);
    }

    private void HandleScreenWrap()
    {
        if (!wrappingEnabled)
            return;

        if (mainCamera == null)
            return;

        if (transform.position.y >= screenBottomY - screenMargin)
            return;

        Vector3 position = transform.position;
        position.y = screenTopY + screenMargin;

        transform.position = position;
    }

    private void CalculateScreenBounds()
    {
        if (mainCamera == null)
            return;

        Vector3 bottomPoint = mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distanceFromCamera));

        Vector3 topPoint = mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distanceFromCamera));

        screenBottomY = bottomPoint.y;
        screenTopY = topPoint.y;
    }
}