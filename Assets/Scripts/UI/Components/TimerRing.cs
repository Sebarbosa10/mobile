using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anillo de timer con un número adentro (el puntaje en el endless, los
/// segundos en Contrarreloj). Image Filled Radial 360, origen arriba,
/// sentido horario, sobre una pista blanca al 25% y un disco Noche al 78%.
///
/// Color según el tiempo restante: blanco de 100 a 50%, interpola a
/// rojo de 50 a 20%, y por debajo de 20% rojo con un pulso de escala
/// 1 → 1,06 dos veces por segundo.
/// </summary>
public sealed class TimerRing : MonoBehaviour
{
    private const float DiscAlpha = 0.78f;
    private const float TrackAlpha = 0.25f;
    private const float PulseScale = 0.06f;
    private const float PulsesPerSecond = 2f;
    private const float PopScale = 1.17f;
    private const float PopDuration = 0.3f;

    private RectTransform ringRoot;
    private Image fill;
    private TextMeshProUGUI label;

    private float fraction = 1f;
    private Coroutine popRoutine;

    public RectTransform RectTransform =>
        (RectTransform)transform;

    /// <param name="size">340 (endless) o 200 (Contrarreloj).</param>
    public static TimerRing Create(Transform parent, string name, float size, float fontSize)
    {
        UISkin skin = UIKit.Skin;
        UIPalette palette = UIKit.Palette;

        bool large = size >= 300f;
        Sprite ringSprite = large ? skin.ring340 : skin.ring200;

        // Diámetro de la línea media del anillo (donde va el disco de fondo).
        float discDiameter = large ? 300f : 172f;

        RectTransform root = UIKit.CreateRect(parent, name);
        root.sizeDelta = new Vector2(size, size);

        TimerRing ring = root.gameObject.AddComponent<TimerRing>();

        ring.ringRoot = UIKit.CreateRect(root, "Ring");
        UIKit.Stretch(ring.ringRoot);

        Color discColor = palette.night;
        discColor.a = DiscAlpha;

        Image disc = UIKit.CreateImage(ring.ringRoot, "Disc", skin.disc, discColor);
        UIKit.PlaceCenter(disc.rectTransform, Vector2.zero, new Vector2(discDiameter, discDiameter));

        Color trackColor = palette.text;
        trackColor.a = TrackAlpha;

        Image track = UIKit.CreateImage(ring.ringRoot, "Track", ringSprite, trackColor);
        UIKit.Stretch(track.rectTransform);

        ring.fill = UIKit.CreateImage(ring.ringRoot, "Fill", ringSprite, palette.text);
        UIKit.Stretch(ring.fill.rectTransform);
        ring.fill.type = Image.Type.Filled;
        ring.fill.fillMethod = Image.FillMethod.Radial360;
        ring.fill.fillOrigin = (int)Image.Origin360.Top;
        ring.fill.fillClockwise = true;
        ring.fill.fillAmount = 1f;

        ring.label = UIKit.CreateText(root, "Value", "0", UIFont.Display, fontSize, palette.text);
        UIKit.Stretch(ring.label.rectTransform);

        return ring;
    }

    public void SetText(string text)
    {
        label.text = text;
    }

    /// <summary>Tiempo restante sobre el total, de 0 a 1.</summary>
    public void SetFraction(float value)
    {
        fraction = Mathf.Clamp01(value);
        fill.fillAmount = fraction;

        UIPalette palette = UIKit.Palette;

        if (fraction >= 0.5f)
            fill.color = palette.text;
        else if (fraction >= 0.2f)
            fill.color = Color.Lerp(palette.text, palette.urgent, (0.5f - fraction) / 0.3f);
        else
            fill.color = palette.urgent;
    }

    /// <summary>"Pop" del número al encestar: crece, se pinta de Sol y vuelve.</summary>
    public void Pop()
    {
        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        UIPalette palette = UIKit.Palette;

        yield return UITween.Run(PopDuration, t =>
        {
            float bump = Mathf.Sin(t * Mathf.PI);
            label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, PopScale, bump);
            label.color = Color.Lerp(palette.text, palette.sun, bump);
        });

        label.rectTransform.localScale = Vector3.one;
        label.color = palette.text;
        popRoutine = null;
    }

    private void Update()
    {
        if (fraction >= 0.2f || fraction <= 0f)
        {
            ringRoot.localScale = Vector3.one;
            return;
        }

        float wave = 0.5f - 0.5f * Mathf.Cos(Time.time * PulsesPerSecond * 2f * Mathf.PI);
        ringRoot.localScale = Vector3.one * (1f + PulseScale * wave);
    }
}
