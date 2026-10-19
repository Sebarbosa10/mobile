using System;
using System.Collections;
using UnityEngine;

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
