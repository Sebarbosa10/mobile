using UnityEngine;

/// <summary>Pantalla que UIManager muestra u oculta.</summary>
public abstract class UIScreen : MonoBehaviour
{
    public bool IsVisible =>
        gameObject.activeSelf;

    public RectTransform RectTransform =>
        (RectTransform)transform;

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
