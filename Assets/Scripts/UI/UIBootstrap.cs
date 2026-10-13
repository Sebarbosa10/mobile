using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arma la UI de cada escena al cargarla, sin que haga falta poner nada
/// en las escenas:
///  - Si la escena tiene un IGameSession activo (BallShotClock, un
///    desafío o GameplayEventsStub), crea GameplayUI conectado a él.
///  - Si no, y hay un MainMenuController, crea MenuUI.
///
/// Corre en sceneLoaded, que llega después de Awake/OnEnable de la
/// escena y antes de Start, así la UI ya está suscripta cuando el
/// gameplay dispara su primer RunStarted.
/// </summary>
public static class UIBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IGameSession session = FindSession(scene);
        MainMenuController menu = session == null ? FindInScene<MainMenuController>(scene) : null;

        if (session == null && menu == null)
            return;

        if (!CanBuildUi())
            return;

        GameObject uiRoot = session != null
            ? GameplayUI.Create(session).gameObject
            : MenuUI.Create(menu).gameObject;

        SceneManager.MoveGameObjectToScene(uiRoot, scene);
    }

    private static bool CanBuildUi()
    {
        if (UISkin.Instance == null)
        {
            Debug.LogError(
                "[UIBootstrap] Falta Assets/UI/Resources/UISkin.asset. " +
                "Generalo con Tools/GenerateUiAssets.ps1.");
            return false;
        }

        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            Debug.LogError(
                "[UIBootstrap] Faltan los TMP Essential Resources. " +
                "Importalos con Window > TextMeshPro > Import TMP Essential Resources.");
            return false;
        }

        return true;
    }

    private static IGameSession FindSession(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour is IGameSession session && behaviour.isActiveAndEnabled)
                    return session;
            }
        }

        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>();

            if (found != null)
                return found;
        }

        return null;
    }
}
