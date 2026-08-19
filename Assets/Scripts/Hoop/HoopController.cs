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

#if UNITY_EDITOR
    [Header("Prototype Score (solo Editor)")]
    [SerializeField]
    private bool enablePrototypeInput = true;
#endif

    [SerializeField]
    private int currentScore;

    public int CurrentScore =>
        currentScore;

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
        // Input de prueba solo para PC/Editor. Se compila afuera del build de
        // mobile por completo (no llega ni a existir en el .apk/.ipa), y usa
        // el New Input System en vez de la clase Input legacy, que directamente
        // no funciona si "Active Input Handling" está seteado a "Input System
        // Package (New)" únicamente.
        if (!enablePrototypeInput || Keyboard.current == null)
            return;

        /*
         * TESTING:
         *
         * Space = sumar punto.
         * R = reiniciar.
         */

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

        // Confirmación visible en consola de que el puntaje efectivamente
        // se está actualizando (útil para probar en dispositivo, donde no
        // siempre es cómodo poner breakpoints).
        Debug.Log($"[HoopController] Puntaje actualizado: {currentScore}", this);

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