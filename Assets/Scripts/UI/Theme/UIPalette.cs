using UnityEngine;

[CreateAssetMenu(fileName = "UIPalette", menuName = "Última Luz/UI Palette")]
public sealed class UIPalette : ScriptableObject
{
    [Header("Botón primario (acción)")]
    public Color primary;
    public Color primaryLip;
    public Color primaryPressed;

    [Header("Botón secundario (reintentar)")]
    public Color secondary;
    public Color secondaryLip;
    public Color secondaryPressed;

    [Header("Botón neutro (volver)")]
    public Color neutral;
    public Color neutralLip;
    public Color neutralPressed;

    [Header("Estados")]
    public Color success;
    [Tooltip("Texto e íconos sobre el verde de éxito (nunca blanco).")]
    public Color onSuccess;
    public Color urgent;

    [Header("Fondos y superficies")]
    [Tooltip("Índigo: fondo de menús.")]
    public Color background;
    [Tooltip("Noche: paneles, chips y sombra de títulos.")]
    public Color night;
    public Color nightLip;
    public Color surface;

    [Header("Bloqueado / deshabilitado")]
    public Color locked;
    public Color lockedLip;
    public Color lockedText;
    public Color lockedSubtext;

    [Header("Acentos")]
    [Tooltip("Sol: récord, +1, progreso.")]
    public Color sun;
    [Tooltip("Destello de encestada.")]
    public Color burst;
    public Color track;
    public Color pipEmpty;
    public Color labelMuted;
    public Color confettiPink;

    [Header("Texto y overlay")]
    public Color text;
    public Color overlay;
}
