using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Onboarding del primer lanzamiento: un punto de dedo sobre la pelota,
/// tres chevrons que se encienden en secuencia hacia arriba y el cartel
/// "Deslizá hacia arriba para lanzar". Se muestra hasta el primer tiro
/// y no vuelve a aparecer (PlayerPrefs).
/// </summary>
public sealed class OnboardingHint : MonoBehaviour
{
    private const string DoneKey = "OnboardingDone";

    // Separaciones del mockup, medidas desde el centro de la pelota.
    private static readonly float[] ChevronOffsets = { 210f, 280f, 350f };
    private const float CaptionOffset = 462f;

    private FeedbackLayer projector;
    private Vector3 ballWorldPosition;

    private RectTransform finger;
    private Image[] chevrons;
    private RectTransform caption;

    public static bool IsDone =>
        PlayerPrefs.GetInt(DoneKey, 0) == 1;

    public static OnboardingHint Create(Transform parent, FeedbackLayer projector)
    {
        UISkin skin = UIKit.Skin;
        UIPalette palette = UIKit.Palette;

        RectTransform root = UIKit.CreateRect(parent, "Onboarding");
        UIKit.Stretch(root);

        OnboardingHint hint = root.gameObject.AddComponent<OnboardingHint>();
        hint.projector = projector;

        Image fingerImage = UIKit.CreateImage(root, "Finger", skin.hintFinger, palette.text);
        UIKit.PlaceCenter(fingerImage.rectTransform, Vector2.zero, new Vector2(160f, 160f));
        hint.finger = fingerImage.rectTransform;

        hint.chevrons = new Image[ChevronOffsets.Length];

        for (int i = 0; i < ChevronOffsets.Length; i++)
        {
            Image chevron = UIKit.CreateImage(root, $"Chevron{i + 1}", skin.hintChevron, palette.text);
            UIKit.PlaceCenter(chevron.rectTransform, Vector2.zero, new Vector2(160f, 96f));
            hint.chevrons[i] = chevron;
        }

        hint.caption = UIKit.CreateChip(root, "Caption", 96f, palette.night, 44f, 0f);
        hint.caption.anchorMin = hint.caption.anchorMax = new Vector2(0.5f, 0.5f);
        hint.caption.pivot = new Vector2(0.5f, 0.5f);
        UIKit.AddChipText(hint.caption, "Deslizá hacia arriba para lanzar", UIFont.Label, 46f, palette.text);

        root.gameObject.SetActive(false);

        return hint;
    }

    public void Show(Vector3 ballWorld)
    {
        ballWorldPosition = ballWorld;
        gameObject.SetActive(true);
        Follow();
    }

    /// <summary>Oculta el onboarding y lo marca como visto.</summary>
    public void Complete()
    {
        if (!gameObject.activeSelf)
            return;

        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();

        gameObject.SetActive(false);
    }

    private void Update()
    {
        Follow();

        // Los chevrons se encienden de abajo hacia arriba, en loop.
        float cycle = Time.unscaledTime * 1.4f;

        for (int i = 0; i < chevrons.Length; i++)
        {
            float wave = Mathf.Repeat(cycle - i * 0.22f, 1f);
            float alpha = Mathf.Lerp(0.3f, 1f, 1f - Mathf.Abs(wave * 2f - 1f));

            Color color = chevrons[i].color;
            color.a = alpha;
            chevrons[i].color = color;
        }

        float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f);
        finger.localScale = Vector3.one * pulse;
    }

    private void Follow()
    {
        if (!projector.TryWorldToLocal(ballWorldPosition, out Vector2 ball))
            return;

        finger.anchoredPosition = ball;

        for (int i = 0; i < chevrons.Length; i++)
            chevrons[i].rectTransform.anchoredPosition = ball + new Vector2(0f, ChevronOffsets[i]);

        caption.anchoredPosition = ball + new Vector2(0f, CaptionOffset);
    }
}
