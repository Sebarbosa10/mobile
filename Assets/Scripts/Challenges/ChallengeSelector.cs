using UnityEngine;

/// <summary>
/// Los tres desafíos conviven en la misma escena, cada uno bajo su
/// propio GameObject raíz (inactivo por defecto). Este componente
/// activa solo el que se eligió en el menú (ver ChallengeSelection) y
/// deja el resto apagado, así sus controladores ni siquiera llegan a
/// correr Awake/Start.
/// </summary>
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
