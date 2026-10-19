using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float minTopInset;

    [SerializeField, Min(0f)]
    private float minBottomInset;

    private RectTransform rectTransform;
    private Canvas rootCanvas;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float lastScaleFactor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    public void SetMinimumInsets(float top, float bottom)
    {
        minTopInset = top;
        minBottomInset = bottom;
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea ||
            Screen.width != lastScreenSize.x ||
            Screen.height != lastScreenSize.y ||
            !Mathf.Approximately(ScaleFactor, lastScaleFactor))
        {
            Apply();
        }
    }

    private float ScaleFactor
    {
        get
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            return rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        }
    }

    private void Apply()
    {
        if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        float scale = ScaleFactor;

        float bottomInset = Mathf.Max(safeArea.yMin, minBottomInset * scale);
        float topInset = Mathf.Max(Screen.height - safeArea.yMax, minTopInset * scale);

        rectTransform.anchorMin = new Vector2(
            safeArea.xMin / Screen.width,
            bottomInset / Screen.height);

        rectTransform.anchorMax = new Vector2(
            safeArea.xMax / Screen.width,
            1f - topInset / Screen.height);

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastScaleFactor = scale;
    }
}
