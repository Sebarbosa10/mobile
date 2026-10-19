using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public sealed class HoopController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField]
    private HoopDifficultyProfile difficultyProfile;

    [Header("References")]
    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private Transform scoreTriggerRoot;

    [SerializeField]
    private HoopMovement movement;

    [Tooltip("Parte visual del aro (prefab Hoop). Si está asignada, la escala de cada etapa se le pide a ella y visualRoot no se usa.")]
    [SerializeField]
    private HoopVisual hoopVisual;

#if UNITY_EDITOR
    [Header("Prototype Score (solo Editor)")]
    [SerializeField]
    private bool enablePrototypeInput = true;
#endif

    [SerializeField]
    private int currentScore;

    public int CurrentScore =>
        currentScore;

    public Vector3 RimPosition =>
        scoreTriggerRoot != null ? scoreTriggerRoot.position : transform.position;

    public event System.Action ShotScored;

    private HoopStage currentStage;

    private void Awake()
    {
        if (movement != null)
        {
            movement.Initialize(
                transform.position,
                transform.rotation);
        }
    }

    private void Start()
    {
        ApplyScore(currentScore);
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!enablePrototypeInput || Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AddPoint();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SetScore(0);
        }
    }
#endif

    public void AddPoint()
    {
        SetScore(currentScore + 1);
        ShotScored?.Invoke();
    }

    public void SetScore(int score)
    {
        int sanitizedScore =
            Mathf.Max(0, score);

        if (sanitizedScore ==
            currentScore)
            return;

        currentScore =
            sanitizedScore;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[HoopController] Puntaje actualizado: {currentScore}", this);
#endif

        ApplyScore(currentScore);
    }

    private void ApplyScore(int score)
    {
        if (difficultyProfile == null)
        {
            Debug.LogWarning(
                "HoopController: No hay Difficulty Profile asignado.",
                this);

            return;
        }

        currentStage =
            difficultyProfile.GetStageForScore(score);

        ApplyScale(currentStage.scale);

        if (movement != null)
        {
            movement.ApplyStage(
                currentStage);
        }
    }

    private void ApplyScale(float scale)
    {
        float safeScale =
            Mathf.Max(
                0.01f,
                scale);

        if (hoopVisual != null)
        {
            hoopVisual.SetScale(safeScale);
            return;
        }

        if (visualRoot != null)
        {
            visualRoot.localScale =
                Vector3.one *
                safeScale;
        }

        if (scoreTriggerRoot != null)
        {
            scoreTriggerRoot.localScale =
                Vector3.one *
                safeScale;
        }
    }
}
