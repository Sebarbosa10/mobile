using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feedback de encestada sobre el aro: destello de rayos (fx_burst),
/// anillo que se expande (fx_ring) y un "+1" en Sol que sube y se
/// desvanece. Las posiciones se calculan proyectando el aro del mundo a
/// la pantalla.
/// </summary>
public sealed class FeedbackLayer : MonoBehaviour
{
    private const float BurstSize = 512f;
    private const float BurstDuration = 0.25f;
    private const float RingSize = 256f;
    private const float RingDuration = 0.35f;

    private const float PlusOneSize = 190f;
    private const float PlusOneDuration = 0.8f;
    private const float PlusOneRise = 140f;
    private static readonly Vector2 PlusOneOffset = new(250f, 190f);

    private RectTransform layer;

    public static FeedbackLayer Create(Transform parent)
    {
        RectTransform rect = UIKit.CreateRect(parent, "Feedback");
        UIKit.Stretch(rect);

        FeedbackLayer feedback = rect.gameObject.AddComponent<FeedbackLayer>();
        feedback.layer = rect;

        return feedback;
    }

    /// <summary>Juega el feedback completo en la posición del aro.</summary>
    public void PlayScore(Vector3 hoopWorldPosition, Color ringColor)
    {
        if (!TryWorldToLocal(hoopWorldPosition, out Vector2 local))
            return;

        UIPalette palette = UIKit.Palette;

        StartCoroutine(BurstRoutine(local, palette.burst));
        StartCoroutine(RingRoutine(local, ringColor));
        StartCoroutine(PlusOneRoutine(local + PlusOneOffset));
    }

    public bool TryWorldToLocal(Vector3 worldPosition, out Vector2 local)
    {
        local = Vector2.zero;

        Camera camera = Camera.main;

        if (camera == null)
            return false;

        Vector3 screen = camera.WorldToScreenPoint(worldPosition);

        if (screen.z < 0f)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            layer, screen, null, out local);
    }

    private IEnumerator BurstRoutine(Vector2 position, Color color)
    {
        Image burst = UIKit.CreateImage(layer, "Burst", UIKit.Skin.fxBurst, color);
        UIKit.PlaceCenter(burst.rectTransform, Vector2.zero, new Vector2(BurstSize, BurstSize));
        burst.rectTransform.anchoredPosition = position;

        yield return UITween.Run(BurstDuration, t =>
        {
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.2f, UITween.EaseOutCubic(t));
            burst.color = new Color(color.r, color.g, color.b, 1f - t * t);
        });

        Destroy(burst.gameObject);
    }

    private IEnumerator RingRoutine(Vector2 position, Color color)
    {
        Image ring = UIKit.CreateImage(layer, "Ring", UIKit.Skin.fxRing, color);
        UIKit.PlaceCenter(ring.rectTransform, Vector2.zero, new Vector2(RingSize, RingSize));
        ring.rectTransform.anchoredPosition = position;

        yield return UITween.Run(RingDuration, t =>
        {
            ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.4f, UITween.EaseOutCubic(t));
            ring.color = new Color(color.r, color.g, color.b, 1f - t);
        });

        Destroy(ring.gameObject);
    }

    private IEnumerator PlusOneRoutine(Vector2 position)
    {
        UIPalette palette = UIKit.Palette;

        RectTransform plusOne = UIKit.CreateShadowedText(
            layer, "PlusOne", "+1", "+1", UIFont.Display, PlusOneSize,
            palette.sun, 10f, 0f, out TextMeshProUGUI label, out TextMeshProUGUI shadow);

        UIKit.PlaceCenter(plusOne, Vector2.zero, new Vector2(320f, 240f));
        plusOne.localRotation = Quaternion.Euler(0f, 0f, -8f);

        CanvasGroup group = plusOne.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        yield return UITween.Run(PlusOneDuration, t =>
        {
            plusOne.anchoredPosition = position + new Vector2(0f, PlusOneRise * UITween.EaseOutCubic(t));
            plusOne.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, UITween.EaseOutBack(Mathf.Clamp01(t * 3f)));
            group.alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
        });

        Destroy(plusOne.gameObject);
    }
}
