using System;
using UnityEngine;

[Serializable]
public struct BallVariant
{
    public Color[] colors;

    public BallVariant(params Color[] colors)
    {
        this.colors = colors;
    }

    public Color Primary =>
        colors != null && colors.Length > 0 ? colors[0] : Color.white;

    public Color Secondary =>
        colors != null && colors.Length > 1 ? colors[1] : Primary;

    public bool HasSecondary =>
        colors != null && colors.Length > 1;
}

[CreateAssetMenu(fileName = "BallSkin", menuName = "Última Luz/Ball Skin")]
public sealed class BallSkin : ScriptableObject
{
    public string id;
    public string displayName;

    [Tooltip("Parte visual: mesh, materiales, contorno y BallColor.")]
    public GameObject prefab;

    [Tooltip("Mesh del cuerpo (informativo; el prefab ya lo usa).")]
    public Mesh mesh;

    [Tooltip("Materiales del cuerpo, uno por submesh (informativo).")]
    public Material[] materials = Array.Empty<Material>();

    [Tooltip("Las 4 variantes que rota la sacudida.")]
    public BallVariant[] variants = new BallVariant[4];

    public UnlockCondition unlock = UnlockCondition.Default();

    [Tooltip("Ícono para la grilla (render del modelo).")]
    public Sprite icon;
}
