using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public enum UIFont
{
    Display,

    Body,

    Label
}

[CreateAssetMenu(fileName = "UISkin", menuName = "Última Luz/UI Skin")]
public sealed class UISkin : ScriptableObject
{
    private const int SamplingPointSize = 90;
    private const int AtlasPadding = 6;
    private const int AtlasSize = 1024;

    [Header("Tema")]
    public UIPalette palette;

    [Tooltip("Shader SDF móvil de TMP. Se referencia para que entre al build.")]
    public Shader sdfShader;

    [Header("Fuentes")]
    public Font displayFont;
    public Font bodyFont;
    public Font labelFont;

    [Header("Botones y paneles (9-slice)")]
    public Sprite buttonBase;
    public Sprite buttonPressed;
    public Sprite panel;
    public Sprite panelBorder;
    public Sprite chip;
    public Sprite pill;

    [Header("HUD")]
    public Sprite ring340;
    public Sprite ring200;
    public Sprite disc;
    public Sprite pipEmpty;

    [Header("Íconos")]
    public Sprite iconPause;
    public Sprite iconLock;
    public Sprite iconTrophy;
    public Sprite iconClock;
    public Sprite iconHoop;
    public Sprite iconShake;
    public Sprite iconCheck;
    public Sprite iconPlay;

    [Header("Efectos y onboarding")]
    public Sprite fxBurst;
    public Sprite fxRing;
    public Sprite fxConfettiTriangle;
    public Sprite hintChevron;
    public Sprite hintFinger;

    [Header("Fondos")]
    public Sprite backgroundSky;
    public Sprite backgroundSkyline;

    private static UISkin instance;

    private readonly Dictionary<UIFont, TMP_FontAsset> fontAssets = new();

    public static UISkin Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<UISkin>("UISkin");

            return instance;
        }
    }

    public static UIPalette Palette =>
        Instance.palette;

    public TMP_FontAsset GetFont(UIFont font)
    {
        if (fontAssets.TryGetValue(font, out TMP_FontAsset cached) && cached != null)
            return cached;

        Font source = font switch
        {
            UIFont.Display => displayFont,
            UIFont.Body => bodyFont,
            _ => labelFont
        };

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
            source,
            SamplingPointSize,
            AtlasPadding,
            GlyphRenderMode.SDFAA,
            AtlasSize,
            AtlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        asset.name = $"{source.name} SDF";

        if (sdfShader != null && asset.material != null)
            asset.material.shader = sdfShader;

        fontAssets[font] = asset;

        return asset;
    }
}
