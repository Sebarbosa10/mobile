using UnityEngine;
using UnityEngine.SceneManagement;

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
