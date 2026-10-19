using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PauseButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    private const float PressedScale = 0.92f;

    private RectTransform plate;
    private Button button;

    public Button.ButtonClickedEvent OnClick =>
        button.onClick;

    public static PauseButton Create(Transform parent)
    {
        Image hitArea = UIKit.CreateImage(parent, "PauseButton", null, Color.clear, raycast: true);
        hitArea.rectTransform.sizeDelta = new Vector2(140f, 140f);

        PauseButton view = hitArea.gameObject.AddComponent<PauseButton>();

        view.button = hitArea.gameObject.AddComponent<Button>();
        view.button.transition = Selectable.Transition.None;

        Image plateImage = UIKit.CreateImage(hitArea.rectTransform, "Plate", UIKit.Skin.chip, UIKit.Palette.night);
        UIKit.PlaceCenter(plateImage.rectTransform, Vector2.zero, new Vector2(112f, 112f));
        view.plate = plateImage.rectTransform;

        Image icon = UIKit.CreateImage(view.plate, "Icon", UIKit.Skin.iconPause, UIKit.Palette.text);
        UIKit.PlaceCenter(icon.rectTransform, Vector2.zero, new Vector2(56f, 56f));

        return view;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        plate.localScale = Vector3.one * PressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        plate.localScale = Vector3.one;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        plate.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        if (plate != null)
            plate.localScale = Vector3.one;
    }
}
