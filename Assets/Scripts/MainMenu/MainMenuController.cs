using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Navegación desde el menú principal. No arma UI: las pantallas del
/// menú (ver MainMenuScreen y ChallengeSelectScreen) llaman a estos
/// métodos desde sus botones.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "SampleScene";

    [SerializeField]
    private string challengesSceneName = "Challenges";

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void PlayChallenge(int challengeNumber)
    {
        if (!ChallengeProgress.IsUnlocked(challengeNumber))
            return;

        ChallengeSelection.SelectedChallenge = challengeNumber;

        SceneManager.LoadScene(challengesSceneName);
    }
}
