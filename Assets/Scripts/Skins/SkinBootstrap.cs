using UnityEngine;
using UnityEngine.SceneManagement;

public static class SkinBootstrap
{
    private const string BallArtName = "BallArt";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SkinDatabase db = SkinDatabase.Instance;

        if (db == null)
            return;

        BallLauncher ball = FindInScene<BallLauncher>(scene);

        if (ball != null)
            ApplyBall(ball, SkinProgress.EquippedBall);

        GameObject environment = FindRoot(scene, ThemeApplier.EnvironmentName);

        if (environment != null)
            ThemeApplier.Apply(environment, SkinProgress.EquippedTheme, Camera.main);
    }

    public static void ApplyBall(BallLauncher ball, BallSkin skin)
    {
        SkinDatabase db = SkinDatabase.Instance;

        if (skin == null || skin.prefab == null || (db != null && skin == db.DefaultBall))
            return;

        Transform oldArt = ball.transform.Find(BallArtName);

        if (oldArt == null)
        {
            BallColor color = ball.GetComponentInChildren<BallColor>(true);
            oldArt = color != null ? color.transform : null;
        }

        GameObject art = Object.Instantiate(skin.prefab, ball.transform);
        art.name = BallArtName;

        if (oldArt != null)
        {
            art.transform.SetLocalPositionAndRotation(oldArt.localPosition, oldArt.localRotation);
            art.transform.localScale = oldArt.localScale;
            oldArt.gameObject.SetActive(false);
            Object.Destroy(oldArt.gameObject);
        }

        if (ball.TryGetComponent(out BallCloseUpScale closeUp))
            closeUp.SetArt(art.transform);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);

            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }
}
