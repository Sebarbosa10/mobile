using UnityEngine;

public sealed class ChallengeSelector : MonoBehaviour
{
    [Tooltip("Raíces de la escena. Cada desafío de ChallengeCatalog indica cuál usa.")]
    [SerializeField]
    private GameObject[] challengeRoots = System.Array.Empty<GameObject>();

    private void Awake()
    {
        int selectedIndex = ChallengeCatalog.Get(ChallengeSelection.SelectedChallenge).RootIndex;

        for (int i = 0; i < challengeRoots.Length; i++)
        {
            if (challengeRoots[i] == null)
                continue;

            challengeRoots[i].SetActive(i == selectedIndex);
        }
    }
}
