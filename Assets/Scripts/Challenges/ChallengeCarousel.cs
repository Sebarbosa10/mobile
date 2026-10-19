using UnityEngine;

public sealed class ChallengeCarousel : MonoBehaviour
{
    [SerializeField]
    private float degreesPerSecond = 25f;

    [SerializeField]
    private Transform[] hoops = System.Array.Empty<Transform>();

    private float[] hoopRadii;
    private float[] hoopStartAngles;

    private float elapsedDegrees;

    private void Awake()
    {
        hoopRadii = new float[hoops.Length];
        hoopStartAngles = new float[hoops.Length];

        for (int i = 0; i < hoops.Length; i++)
        {
            if (hoops[i] == null)
                continue;

            Vector3 offset = hoops[i].position - transform.position;

            hoopRadii[i] =
                new Vector2(offset.x, offset.z).magnitude;

            hoopStartAngles[i] =
                Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }
    }

    private void Update()
    {
        elapsedDegrees += degreesPerSecond * Time.deltaTime;

        for (int i = 0; i < hoops.Length; i++)
        {
            Transform hoop = hoops[i];

            if (hoop == null)
                continue;

            float angle =
                (hoopStartAngles[i] + elapsedDegrees) * Mathf.Deg2Rad;

            Vector3 offset = new(
                Mathf.Sin(angle) * hoopRadii[i],
                0f,
                Mathf.Cos(angle) * hoopRadii[i]);

            hoop.position = transform.position + offset;
        }
    }
}
