using UnityEngine;

/// <summary>Reposiciona una pelota cuando cae por debajo del límite visible.</summary>
public sealed class BallScreenWrapController : MonoBehaviour
{
    private Camera mainCamera;
    private float screenMargin;
    private float distanceFromCamera;
    private float screenTopY;
    private float screenBottomY;
    private bool isEnabled;

    public void Configure(Camera camera, float margin, float depth)
    {
        mainCamera = camera;
        screenMargin = margin;
        distanceFromCamera = depth;
        CalculateScreenBounds();
    }

    public void EnableWrapping() => isEnabled = true;

    private void Update()
    {
        if (!isEnabled || mainCamera == null || transform.position.y >= screenBottomY - screenMargin)
            return;

        Vector3 position = transform.position;
        position.y = screenTopY + screenMargin;
        transform.position = position;
    }

    private void CalculateScreenBounds()
    {
        if (mainCamera == null)
            return;

        screenBottomY = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceFromCamera)).y;
        screenTopY = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distanceFromCamera)).y;
    }
}
