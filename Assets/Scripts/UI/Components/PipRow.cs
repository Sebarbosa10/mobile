using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila de puntos de progreso: llenos (disco teñido) y vacíos (aro de
/// 6 px en #8E86A8). Se usa en el HUD de desafíos y en los resultados.
/// </summary>
public sealed class PipRow : MonoBehaviour
{
    private readonly List<Image> pips = new();

    private HorizontalLayoutGroup layout;

    public RectTransform RectTransform =>
        (RectTransform)transform;

    public static PipRow Create(Transform parent, string name, float height)
    {
        RectTransform root = UIKit.CreateRect(parent, name);
        root.sizeDelta = new Vector2(0f, height);

        PipRow row = root.gameObject.AddComponent<PipRow>();

        row.layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.layout.childAlignment = TextAnchor.MiddleCenter;
        row.layout.childControlWidth = false;
        row.layout.childControlHeight = false;
        row.layout.childForceExpandWidth = false;
        row.layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        return row;
    }

    public void Set(int total, int filled, Color filledColor, float size, float spacing)
    {
        UISkin skin = UIKit.Skin;
        UIPalette palette = UIKit.Palette;

        layout.spacing = spacing;

        while (pips.Count < total)
            pips.Add(UIKit.CreateImage(RectTransform, $"Pip{pips.Count + 1}", skin.disc, Color.white));

        for (int i = 0; i < pips.Count; i++)
        {
            Image pip = pips[i];
            bool active = i < total;

            pip.gameObject.SetActive(active);

            if (!active)
                continue;

            bool isFilled = i < filled;

            pip.sprite = isFilled ? skin.disc : skin.pipEmpty;
            pip.color = isFilled ? filledColor : palette.pipEmpty;
            pip.rectTransform.sizeDelta = new Vector2(size, size);
        }
    }
}
