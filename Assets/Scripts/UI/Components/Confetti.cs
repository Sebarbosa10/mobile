using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Confeti de "¡Nuevo récord!": rectángulos (Image sin sprite) y
/// triángulos (fx_confetti_tri) en los colores de la paleta, que caen
/// girando mientras el panel está visible. Usa tiempo no escalado.
/// </summary>
public sealed class Confetti : MonoBehaviour
{
    private const int PieceCount = 26;

    private readonly List<Piece> pieces = new();

    private RectTransform area;

    public static Confetti Create(Transform parent)
    {
        RectTransform rect = UIKit.CreateRect(parent, "Confetti");
        UIKit.Stretch(rect);

        Confetti confetti = rect.gameObject.AddComponent<Confetti>();
        confetti.area = rect;
        confetti.Build();

        return confetti;
    }

    private void Build()
    {
        UIPalette palette = UIKit.Palette;

        Color[] colors =
        {
            palette.sun,
            palette.primary,
            palette.success,
            palette.secondary,
            palette.confettiPink
        };

        for (int i = 0; i < PieceCount; i++)
        {
            bool triangle = i % 3 == 0;

            Image image = UIKit.CreateImage(
                area, $"Piece{i}",
                triangle ? UIKit.Skin.fxConfettiTriangle : null,
                colors[i % colors.Length]);

            image.rectTransform.sizeDelta = triangle ? new Vector2(40f, 40f) : new Vector2(44f, 20f);

            pieces.Add(new Piece
            {
                Rect = image.rectTransform,
                Speed = Random.Range(220f, 420f),
                Spin = Random.Range(-220f, 220f),
                Sway = Random.Range(20f, 60f),
                Phase = Random.Range(0f, Mathf.PI * 2f)
            });
        }
    }

    private void OnEnable()
    {
        // Arranca repartido por toda la pantalla, como en el mockup.
        Rect bounds = area.rect;

        foreach (Piece piece in pieces)
        {
            piece.X = Random.Range(bounds.xMin, bounds.xMax);
            piece.Y = Random.Range(bounds.yMin, bounds.yMax);
            piece.Rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            Place(piece, 0f);
        }
    }

    private void Update()
    {
        Rect bounds = area.rect;
        float delta = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        foreach (Piece piece in pieces)
        {
            piece.Y -= piece.Speed * delta;

            if (piece.Y < bounds.yMin - 60f)
            {
                piece.Y = bounds.yMax + 60f;
                piece.X = Random.Range(bounds.xMin, bounds.xMax);
            }

            piece.Rect.Rotate(0f, 0f, piece.Spin * delta);
            Place(piece, time);
        }
    }

    private static void Place(Piece piece, float time)
    {
        float sway = Mathf.Sin(time * 2f + piece.Phase) * piece.Sway;
        piece.Rect.anchoredPosition = new Vector2(piece.X + sway, piece.Y);
    }

    private sealed class Piece
    {
        public RectTransform Rect;
        public float X;
        public float Y;
        public float Speed;
        public float Spin;
        public float Sway;
        public float Phase;
    }
}
