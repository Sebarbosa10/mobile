using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de progreso de desbloqueo: 904×36, radio 18, pista #3A2D5C y
/// relleno Sol. El relleno es una píldora 9-slice cuyo ancho sigue el
/// progreso (así conserva las puntas redondeadas).
/// </summary>
public sealed class ProgressBar : MonoBehaviour
{
    private const float FlashDuration = 0.2f;

    private RectTransform fillRect;
    private Image flash;
    private float height;
    private Coroutine flashRoutine;

    public RectTransform RectTransform =>
        (RectTransform)transform;

    public static ProgressBar Create(Transform parent, string name, Vector2 size)
    {
        UISkin skin = UIKit.Skin;
        UIPalette palette = UIKit.Palette;

        Image track = UIKit.CreateImage(parent, name, skin.pill, palette.track);
        track.rectTransform.sizeDelta = size;

        ProgressBar bar = track.gameObject.AddComponent<ProgressBar>();
        bar.height = size.y;

        Image fill = UIKit.CreateImage(track.rectTransform, "Fill", skin.pill, palette.sun);
        bar.fillRect = fill.rectTransform;
        bar.fillRect.anchorMin = Vector2.zero;
        bar.fillRect.anchorMax = new Vector2(0f, 1f);
        bar.fillRect.pivot = new Vector2(0f, 0.5f);
        bar.fillRect.offsetMin = Vector2.zero;
        bar.fillRect.offsetMax = Vector2.zero;

        bar.flash = UIKit.CreateImage(track.rectTransform, "Flash", skin.pill, new Color(1f, 1f, 1f, 0f));
        UIKit.Stretch(bar.flash.rectTransform);

        return bar;
    }

    public void SetFraction(float value)
    {
        value = Mathf.Clamp01(value);

        // Por debajo del alto de la barra la píldora se deformaría: se oculta.
        float width = RectTransform.rect.width > 0f ? RectTransform.rect.width : RectTransform.sizeDelta.x;
        bool visible = value * width >= height;

        fillRect.gameObject.SetActive(visible);
        fillRect.anchorMax = new Vector2(value, 1f);
        fillRect.offsetMax = Vector2.zero;
    }

    /// <summary>Destello blanco de 0,2 s (al completar un tramo).</summary>
    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return UITween.Run(FlashDuration, t =>
            flash.color = new Color(1f, 1f, 1f, 1f - t));

        flashRoutine = null;
    }
}
