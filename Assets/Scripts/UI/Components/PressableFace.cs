using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PressableFace : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler,
    IPointerEnterHandler
{
    private const float PressedDrop = 8f;
    private const float PressedLip = 4f;

    private Image face;
    private RectTransform content;
    private Selectable selectable;

    private Color normalColor;
    private Color pressedColor;

    private bool pointerDown;
    private bool pressEnabled = true;

    public void Configure(Image faceImage, RectTransform contentRect, Selectable target)
    {
        face = faceImage;
        content = contentRect;
        selectable = target;

        Apply(false);
    }

    public void SetColors(Color normal, Color pressed)
    {
        normalColor = normal;
        pressedColor = pressed;

        Apply(false);
    }

    public void SetPressEnabled(bool enabled)
    {
        pressEnabled = enabled;
        Apply(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        Apply(CanPress);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
        Apply(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Apply(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (pointerDown)
            Apply(CanPress);
    }

    private void OnDisable()
    {
        pointerDown = false;
        Apply(false);
    }

    private bool CanPress =>
        pressEnabled && (selectable == null || selectable.IsInteractable());

    private void Apply(bool pressed)
    {
        if (face == null)
            return;

        UISkin skin = UISkin.Instance;

        face.sprite = pressed ? skin.buttonPressed : skin.buttonBase;
        face.color = pressed ? pressedColor : normalColor;
        face.rectTransform.offsetMax = new Vector2(0f, pressed ? -PressedDrop : 0f);

        if (content != null)
        {
            Vector2 offsetMin = content.offsetMin;
            offsetMin.y = pressed ? PressedLip : UIKit.ButtonLip;
            content.offsetMin = offsetMin;
        }
    }
}
