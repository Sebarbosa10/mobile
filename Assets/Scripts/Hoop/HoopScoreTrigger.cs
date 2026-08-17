using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class HoopScoreTrigger : MonoBehaviour
{
    [SerializeField]
    private HoopProgression progression;

    [SerializeField]
    private string ballTag = "Ball";

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ballTag))
            return;

        progression?.AddPoint();
    }
}