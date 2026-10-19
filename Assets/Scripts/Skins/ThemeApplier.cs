using UnityEngine;

public static class ThemeApplier
{
    public const string EnvironmentName = "CourtEnvironment";
    public const string CourtName = "Court";
    private const string ThemeInstanceName = "ThemeEnvironment";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private const int FloorCement = 0;
    private const int FloorLight = 1;
    private const int FloorShadow = 2;
    private const int FloorMid = 3;
    private const int FloorKey = 4;
    private const int FloorCrack = 6;

    public static void Apply(GameObject environment, BackgroundTheme theme, Camera camera)
    {
        if (environment == null || theme == null || theme.environmentPrefab == null)
            return;

        Transform root = environment.transform;
        Transform court = null;

        foreach (Transform child in root)
        {
            if (child.name == CourtName)
                court = child;
            else
                child.gameObject.SetActive(false);
        }

        GameObject instance = Object.Instantiate(theme.environmentPrefab, root);
        instance.name = ThemeInstanceName;
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (court != null)
            ApplyFloor(court.GetComponentInChildren<MeshRenderer>(), theme);

        if (camera != null)
            camera.backgroundColor = theme.HorizonColor;
    }

    public static void ApplyFloor(Renderer floor, BackgroundTheme theme)
    {
        if (floor == null || theme == null)
            return;

        MaterialPropertyBlock block = new();
        int count = floor.sharedMaterials.Length;

        void Set(int index, Color color)
        {
            if (index >= count)
                return;

            floor.GetPropertyBlock(block, index);
            block.SetColor(BaseColorId, color);
            floor.SetPropertyBlock(block, index);
        }

        Set(FloorCement, theme.floor);
        Set(FloorLight, theme.floorLight);
        Set(FloorShadow, theme.floorShadow);
        Set(FloorMid, theme.floorMid);
        Set(FloorKey, theme.key);
        Set(FloorCrack, theme.crack);
    }
}
