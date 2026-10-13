using UnityEngine;

/// <summary>
/// Ajusta este RectTransform para que ocupe solo el área "segura" de
/// la pantalla (fuera de notches, cámaras perforadas, barra de
/// gestos, esquinas redondeadas, etc.), recalculando sus anchors a
/// partir de Screen.safeArea. El HUD tiene que ir parentado bajo este
/// rect (no directo al Canvas) para que nada quede tapado en equipos
/// con recortes de pantalla.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // La safe area puede cambiar en runtime (rotación de pantalla,
        // notch que aparece/desaparece en algunos Android).
        if (Screen.safeArea != lastSafeArea ||
            Screen.width != lastScreenSize.x ||
            Screen.height != lastScreenSize.y)
        {
            Apply();
        }
    }

    private void Apply()
    {
        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}
