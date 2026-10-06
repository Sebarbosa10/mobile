using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Animaciones mínimas para la UI, siempre con tiempo no escalado: la
/// pausa pone Time.timeScale en 0 y los paneles igual tienen que animar.
/// </summary>
public static class UITween
{
    public static float EaseOutCubic(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t;
    }

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t) - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    /// <summary>Corre "step" con un progreso de 0 a 1 durante "duration" segundos reales.</summary>
    public static IEnumerator Run(float duration, Action<float> step)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            step(elapsed / duration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        step(1f);
    }
}
