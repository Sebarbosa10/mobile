using UnityEngine;

public sealed class ChallengeSelector : MonoBehaviour
{
    [Tooltip("Índice 0 = Desafío 1, índice 1 = Desafío 2, etc.")]
    [SerializeField]
    private GameObject[] challengeRoots = System.Array.Empty<GameObject>();

    private void Awake()
    {
        int selectedIndex = ChallengeSelection.SelectedChallenge - 1;

        for (int i = 0; i < challengeRoots.Length; i++)
        {
            if (challengeRoots[i] == null)
                continue;

            challengeRoots[i].SetActive(i == selectedIndex);
        }
    }
}
