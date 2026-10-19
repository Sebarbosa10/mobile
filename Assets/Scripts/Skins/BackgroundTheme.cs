using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundTheme", menuName = "Última Luz/Background Theme")]
public sealed class BackgroundTheme : ScriptableObject
{
    public string id;
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("Paleta del tema para la UI (5 cuadrados de la vista previa).")]
    public Color[] palette = new Color[5];

    [Tooltip("Degradé del cielo, de arriba al horizonte.")]
    public Color[] skyColors = new Color[2];

    [Tooltip("Fondo del tema (cielo + siluetas + elementos). Vacío = el entorno de la escena (Plaza).")]
    public GameObject environmentPrefab;

    [Tooltip("Materiales que usa el prefab de entorno (informativo).")]
    public Material[] environmentMaterials = Array.Empty<Material>();

    [Header("Piso de la cancha (colores opacos)")]
    public Color floor;
    public Color floorLight;
    public Color floorShadow;
    public Color floorMid;
    public Color crack;

    [Tooltip("Llave ya mezclada sobre el piso (por ejemplo #2EC4E6 al 35%).")]
    public Color key;

    public UnlockCondition unlock = UnlockCondition.Default();

    [Tooltip("Miniatura 956×380 (Tools > Capture Theme Thumbnails).")]
    public Texture2D thumbnail;

    [Tooltip("Triángulos del prefab de entorno (lo calcula el builder).")]
    public int triangleCount;

    public Color HorizonColor =>
        skyColors != null && skyColors.Length > 0 ? skyColors[skyColors.Length - 1] : Color.black;
}
