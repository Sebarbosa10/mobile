using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BallLauncher))]
public sealed class ShotAssist : MonoBehaviour
{
    public const float CaptureMargin = 0.15f;

    public const float MaxHeightAbove = 0.9f;

    public const float Strength = 3f;

    public const float MaxAcceleration = 4f;

    private Rigidbody body;
    private BallLauncher launcher;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        launcher = GetComponent<BallLauncher>();
    }

    private void FixedUpdate()
    {
        if (!launcher.IsLaunched || body.isKinematic)
            return;

        Vector3 velocity = body.linearVelocity;

        if (velocity.y >= 0f)
            return;

        Vector3 position = body.position;
        float gravity = -Physics.gravity.y;

        bool found = false;
        Vector2 bestError = Vector2.zero;
        float bestTime = 0f;

        foreach (HoopScoreTrigger hoop in HoopScoreTrigger.Active)
        {
            Vector3 center = hoop.RimCenterWorld;
            float height = position.y - center.y;

            if (height < -0.05f || height > MaxHeightAbove)
                continue;

            float time = (velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2f * gravity * Mathf.Max(0f, height))) / gravity;
            time = Mathf.Max(time, 0.02f);

            Vector2 landing = new(position.x + velocity.x * time, position.z + velocity.z * time);
            Vector2 error = landing - new Vector2(center.x, center.z);

            if (error.magnitude > hoop.RimRadiusWorld + CaptureMargin)
                continue;

            if (!found || error.sqrMagnitude < bestError.sqrMagnitude)
            {
                found = true;
                bestError = error;
                bestTime = time;
            }
        }

        if (!found)
            return;

        Vector2 correction = -bestError / bestTime;
        float blend = 1f - Mathf.Exp(-Strength * Time.fixedDeltaTime);
        Vector2 change = Vector2.ClampMagnitude(correction * blend, MaxAcceleration * Time.fixedDeltaTime);

        velocity.x += change.x;
        velocity.z += change.y;
        body.linearVelocity = velocity;
    }
}
