using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class OverlayPanel : MonoBehaviour
{
    public const float Width = 840f;

    private const float EnterDuration = 0.2f;
    private const float EnterScale = 0.9f;

    private RectTransform panel;
    private Image border;
    private float cursor;
    private Coroutine enterRoutine;

    public RectTransform Content =>
        panel;

    public RectTransform Backdrop { get; private set; }

    public static OverlayPanel Create(Transform parent, string name, float paddingTop)
    {
        UISkin skin = UIKit.Skin;
        UIPalette palette = UIKit.Palette;

        Image dim = UIKit.CreateImage(parent, name, null, palette.overlay, raycast: true);
        UIKit.Stretch(dim.rectTransform);

        OverlayPanel overlay = dim.gameObject.AddComponent<OverlayPanel>();

        overlay.Backdrop = UIKit.CreateRect(dim.rectTransform, "Backdrop");
        UIKit.Stretch(overlay.Backdrop);

        Image panelImage = UIKit.CreateImage(dim.rectTransform, "Panel", skin.panel, palette.night);
        overlay.panel = panelImage.rectTransform;
        UIKit.PlaceCenter(overlay.panel, Vector2.zero, new Vector2(Width, 0f));

        overlay.border = UIKit.CreateImage(overlay.panel, "Border", skin.panelBorder, palette.sun);
        UIKit.Stretch(overlay.border.rectTransform);
        overlay.border.rectTransform.offsetMin = new Vector2(0f, UIKit.PanelLip);
        overlay.border.gameObject.SetActive(false);

        overlay.cursor = paddingTop;

        dim.gameObject.SetActive(false);

        return overlay;
    }

    public void SetBorder(Color? color)
    {
        border.gameObject.SetActive(color.HasValue);

        if (color.HasValue)
            border.color = color.Value;
    }

    public T Add<T>(T item, float height, float spacing = 0f) where T : Component
    {
        cursor += spacing;

        RectTransform rect = (RectTransform)item.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -cursor);

        cursor += height;

        return item;
    }

    public void Finish(float paddingBottom)
    {
        panel.sizeDelta = new Vector2(Width, cursor + paddingBottom);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (enterRoutine != null)
            StopCoroutine(enterRoutine);

        enterRoutine = StartCoroutine(EnterRoutine());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator EnterRoutine()
    {
        yield return UITween.Run(EnterDuration, t =>
            panel.localScale = Vector3.one * Mathf.Lerp(EnterScale, 1f, UITween.EaseOutBack(t)));

        enterRoutine = null;
    }
}
