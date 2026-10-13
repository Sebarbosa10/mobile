using UnityEngine;

/// <summary>
/// Cuenta atrás el tiempo disponible para completar un intento
/// (agarrar, apuntar, lanzar y encestar). El límite depende de la
/// etapa de dificultad actual del aro, así que la presión del reloj
/// escala junto con el resto de la dificultad a medida que sube el
/// puntaje.
///
/// Los intentos son ilimitados: si el tiempo se agota, la pelota
/// simplemente se reinicia como si hubiera fallado (BallLauncher.
/// ForceReset) y arranca un intento nuevo con el límite que
/// corresponda al puntaje actual.
/// </summary>
public sealed class BallShotClock : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private HoopController hoopController;

    [Header("Fallback")]
    [Tooltip("Límite usado si no hay un HoopController asignado.")]
    [SerializeField, Min(0.1f)]
    private float fallbackTimeLimit = 5f;

    private float remainingTime;
    private bool wasReady;

    public float RemainingTime =>
        remainingTime;

    public float CurrentTimeLimit =>
        Mathf.Max(
            hoopController != null
                ? hoopController.CurrentShotTimeLimit
                : fallbackTimeLimit,
            0.1f);

    private void Start()
    {
        BeginNewAttempt();
    }

    private void Update()
    {
        if (ballLauncher == null)
            return;

        /*
         * Un intento nuevo también puede empezar sin que nosotros lo
         * forcemos (la pelota tocó el suelo y se reinició sola). Acá
         * detectamos esa transición para arrancar el reloj con el
         * límite que corresponda al puntaje ya actualizado.
         */
        bool isReady = ballLauncher.IsReady;

        if (isReady && !wasReady)
        {
            BeginNewAttempt();
        }

        wasReady = isReady;

        remainingTime -= Time.deltaTime;

        if (remainingTime > 0f)
            return;

        ballLauncher.ForceReset();
        BeginNewAttempt();
    }

    private void BeginNewAttempt()
    {
        remainingTime = CurrentTimeLimit;
    }
}
