using UnityEngine;

public sealed class CarouselRig : MonoBehaviour
{
    [SerializeField]
    private Transform hub;

    [SerializeField]
    private Transform[] arms = System.Array.Empty<Transform>();

    [SerializeField]
    private Transform[] armTargets = System.Array.Empty<Transform>();

    private float hubYawOffset;
    private bool hasYawOffset;

    private void LateUpdate()
    {
        if (hub == null)
            return;

        Vector3 from = hub.position;

        for (int i = 0; i < arms.Length && i < armTargets.Length; i++)
        {
            Transform arm = arms[i];
            Transform target = armTargets[i];

            if (arm == null || target == null)
                continue;

            Vector3 delta = target.position - from;
            float length = delta.magnitude;

            if (length < 1e-4f)
                continue;

            arm.SetPositionAndRotation(from, Quaternion.LookRotation(delta / length, Vector3.up));
            arm.localScale = new Vector3(1f, 1f, length);
        }

        if (armTargets.Length > 0 && armTargets[0] != null)
        {
            Vector3 offset = armTargets[0].position - from;
            float yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

            if (!hasYawOffset)
            {
                hubYawOffset = hub.eulerAngles.y - yaw;
                hasYawOffset = true;
            }

            hub.rotation = Quaternion.Euler(0f, yaw + hubYawOffset, 0f);
        }
    }
}
