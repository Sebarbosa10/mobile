using UnityEngine;



public class BallController : MonoBehaviour
{
    [Header("Física")]
    [SerializeField] private float jumpForce = 10f;

    [Header("Pantalla")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float screenMargin = 0.5f;
    [SerializeField] private float distanceFromCamera = 10f; // distancia en Z respecto a la cámara

    private Rigidbody rb;
    private bool hasStarted = false;

    private float screenTopY;
    private float screenBottomY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // estática al inicio, sin gravedad
    }

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        CalculateScreenBounds();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnTap();
        }

        if (hasStarted && transform.position.y < screenBottomY - screenMargin)
        {
            WrapToTop();
        }
    }

    private void OnTap()
    {
        if (!hasStarted)
        {
            hasStarted = true;
            rb.isKinematic = false; // activa física
        }

        Vector3 vel = rb.linearVelocity;
        vel.y = 0f; // resetea velocidad vertical antes de saltar
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void WrapToTop()
    {
        Vector3 pos = transform.position;
        pos.y = screenTopY + screenMargin;
        transform.position = pos;
        // no tocamos velocidad: la caída sigue siendo continua
    }

    private void CalculateScreenBounds()
    {
        // Usamos la distancia en Z de la pelota respecto a la cámara para el cálculo correcto en perspectiva
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, distanceFromCamera));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, distanceFromCamera));

        screenBottomY = bottomLeft.y;
        screenTopY = topRight.y;
    }

}