using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ButtonVariant
{
    /// <summary>Azul: acción principal (JUGAR, REANUDAR).</summary>
    Primary,

    /// <summary>Naranja: reintentar / reiniciar.</summary>
    Secondary,

    /// <summary>Gris: volver / menú.</summary>
    Neutral
}

/// <summary>
/// Botón del design system: 520×150 (640×170 la variante grande de
/// JUGAR) más el labio de 12 px, Anton 64 (84) con tracking +3, teñido
/// según la variante. Presionado: sprite de labio 4, color presionado y
/// el contenido baja 8 px. Deshabilitado: colores de Bloqueado.
/// </summary>
public sealed class UIButton : MonoBehaviour
{
    public static readonly Vector2 DefaultSize = new(520f, 150f);
    public static readonly Vector2 LargeSize = new(640f, 170f);

    private Button button;
    private PressableFace pressable;
    private TextMeshProUGUI label;
    private ButtonVariant variant;

    public Button.ButtonClickedEvent OnClick =>
        button.onClick;

    public RectTransform RectTransform =>
        (RectTransform)transform;

    /// <summary>Alto que ocupa en un layout (sin contar el labio, que sobresale abajo).</summary>
    public float LayoutHeight { get; private set; }

    public static UIButton Create(
        Transform parent,
        string name,
        string text,
        ButtonVariant variant,
        bool large = false)
    {
        Vector2 size = large ? LargeSize : DefaultSize;

        RectTransform root = UIKit.CreateRect(parent, name);
        root.sizeDelta = new Vector2(size.x, size.y + UIKit.ButtonLip);

        UIButton view = root.gameObject.AddComponent<UIButton>();
        view.LayoutHeight = size.y;

        view.button = root.gameObject.AddComponent<Button>();
        view.button.transition = Selectable.Transition.None;

        Image face = UIKit.CreateImage(root, "Face", UIKit.Skin.buttonBase, Color.white, raycast: true);
        UIKit.Stretch(face.rectTransform);

        RectTransform content = UIKit.CreateRect(face.rectTransform, "Content");
        UIKit.Stretch(content);
        content.offsetMin = new Vector2(0f, UIKit.ButtonLip);

        view.label = UIKit.CreateText(
            content, "Label", text, UIFont.Display,
            large ? 84f : 64f, UIKit.Palette.text,
            large ? 4f : 3f);
        UIKit.Stretch(view.label.rectTransform);

        view.pressable = root.gameObject.AddComponent<PressableFace>();
        view.pressable.Configure(face, content, view.button);

        view.variant = variant;
        view.ApplyColors();

        return view;
    }

    public void SetText(string text)
    {
        label.text = text;
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
        ApplyColors();
    }

    private void ApplyColors()
    {
        UIPalette palette = UIKit.Palette;

        if (!button.interactable)
        {
            pressable.SetColors(palette.locked, palette.locked);
            label.color = palette.lockedText;
            return;
        }

        label.color = palette.text;

        switch (variant)
        {
            case ButtonVariant.Secondary:
                pressable.SetColors(palette.secondary, palette.secondaryPressed);
                break;

            case ButtonVariant.Neutral:
                pressable.SetColors(palette.neutral, palette.neutralPressed);
                break;

            default:
                pressable.SetColors(palette.primary, palette.primaryPressed);
                break;
        }
    }
}
