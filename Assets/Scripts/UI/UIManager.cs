using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dueño del Canvas de una escena: crea el Canvas raíz (Scale With
/// Screen Size, 1080×1920, Match 0) y registra las pantallas para
/// mostrarlas u ocultarlas. La conexión con el gameplay vive en
/// MenuUI y GameplayUI.
/// </summary>
public sealed class UIManager : MonoBehaviour
{
    private readonly List<UIScreen> screens = new();

    /// <summary>Rect del Canvas a pantalla completa (para fondos y overlays).</summary>
    public RectTransform Root { get; private set; }

    public static UIManager Create(string name)
    {
        UIKit.EnsureEventSystem();

        GameObject managerObject = new(name);
        UIManager manager = managerObject.AddComponent<UIManager>();

        Canvas canvas = UIKit.CreateCanvas($"{name}Canvas", 0);
        canvas.transform.SetParent(managerObject.transform, false);

        manager.Root = (RectTransform)canvas.transform;

        return manager;
    }

    public T Register<T>(T screen) where T : UIScreen
    {
        screens.Add(screen);
        return screen;
    }

    public void Show(UIScreen screen)
    {
        screen.Show();
    }

    public void Hide(UIScreen screen)
    {
        screen.Hide();
    }

    /// <summary>Muestra una pantalla y oculta todas las demás registradas.</summary>
    public void ShowOnly(UIScreen screen)
    {
        foreach (UIScreen other in screens)
        {
            if (other != screen && other.IsVisible)
                other.Hide();
        }

        screen.Show();
    }
}
