using System.Collections.Generic;
using UnityEngine;

public sealed class UIManager : MonoBehaviour
{
    private readonly List<UIScreen> screens = new();

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
