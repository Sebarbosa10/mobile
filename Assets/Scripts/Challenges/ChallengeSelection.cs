/// <summary>
/// Número de desafío elegido en el menú (1, 2, 3...). MainMenuController
/// lo fija antes de cargar la escena de desafíos; ChallengeSelector lo
/// lee al arrancar esa escena para saber cuál activar. Es estático
/// porque SceneManager.LoadScene no permite pasar parámetros directo.
/// </summary>
public static class ChallengeSelection
{
    public static int SelectedChallenge = 1;
}
