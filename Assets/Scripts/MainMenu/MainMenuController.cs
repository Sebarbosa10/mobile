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
            new Vector2(0, -480), 70, FontStyle.Bold);
        title.text = "DESAFIOS";

        // CreateText ancla al borde superior: estas Y se miden desde arriba.
        Text progress = UguiFactory.CreateText(
            challengePanel.transform, "Progress",
            new Vector2(0, -620), 36, FontStyle.Normal);
        progress.text =
            $"Encestadas en el endless: {ChallengeProgress.TotalBaskets}\n" +
            $"Cada {ChallengeProgress.BasketsPerUnlock} desbloqueás un desafío";

        CreateChallengeButton(challengePanel.transform, 1, new Vector2(0, 0));
        CreateChallengeButton(challengePanel.transform, 2, new Vector2(0, -180));
        CreateChallengeButton(challengePanel.transform, 3, new Vector2(0, -360));

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
        Vector2 anchoredPosition)
    {
        bool available = ChallengeProgress.IsUnlocked(number);

        Color color = available
            ? new Color(0.19607843f, 0.4117647f, 0.7607843f, 1f)
            : new Color(0.35f, 0.35f, 0.35f, 1f);

        string label = available
            ? $"DESAFIO {number}"
            : $"DESAFIO {number}\n<size=32>BLOQUEADO · {ChallengeProgress.RequiredBaskets(number)} ENCESTADAS</size>";

        Button button = UguiFactory.CreateButton(
            parent, $"Challenge{number}Button",
            anchoredPosition, new Vector2(600, 150),
            label, color);

        button.interactable = available;

        if (available)
            button.onClick.AddListener(() => SelectChallenge(number));
    }
}
