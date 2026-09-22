using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "SampleScene";

    [SerializeField]
    private string challengesSceneName = "Challenges";

    private GameObject challengePanel;

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenChallenges()
    {
        if (challengePanel == null)
            BuildChallengePanel();

        challengePanel.SetActive(true);
    }

    public void CloseChallenges()
    {
        if (challengePanel != null)
            challengePanel.SetActive(false);
    }

    private void SelectChallenge(int challengeNumber)
    {
        ChallengeSelection.SelectedChallenge = challengeNumber;

        SceneManager.LoadScene(challengesSceneName);
    }

    private void BuildChallengePanel()
    {
        UguiFactory.EnsureEventSystem();

        GameObject canvasObject = UguiFactory.CreateCanvas(transform, "ChallengeSelectCanvas");

        challengePanel = new GameObject("ChallengePanel", typeof(Image));
        challengePanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = challengePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        challengePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        Text title = UguiFactory.CreateText(
            challengePanel.transform, "Title",
            new Vector2(0, 260), 70, FontStyle.Bold);
        title.text = "DESAFIOS";

        CreateChallengeButton(challengePanel.transform, 1, new Vector2(0, 60), available: true);
        CreateChallengeButton(challengePanel.transform, 2, new Vector2(0, -120), available: true);
        CreateChallengeButton(challengePanel.transform, 3, new Vector2(0, -300), available: true);

        Button backButton = UguiFactory.CreateButton(
            challengePanel.transform, "BackButton",
            new Vector2(0, -520), new Vector2(480, 140),
            "VOLVER", new Color(0.3f, 0.3f, 0.3f, 1f));

        backButton.onClick.AddListener(CloseChallenges);

        challengePanel.SetActive(false);
    }

    private void CreateChallengeButton(
        Transform parent,
        int number,
        Vector2 anchoredPosition,
        bool available)
    {
        Color color = available
            ? new Color(0.19607843f, 0.4117647f, 0.7607843f, 1f)
            : new Color(0.35f, 0.35f, 0.35f, 1f);

        string label = available
            ? $"DESAFIO {number}"
            : $"DESAFIO {number} (PRONTO)";

        Button button = UguiFactory.CreateButton(
            parent, $"Challenge{number}Button",
            anchoredPosition, new Vector2(600, 150),
            label, color);

        button.interactable = available;

        if (available)
            button.onClick.AddListener(() => SelectChallenge(number));
    }
}
