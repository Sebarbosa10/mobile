using System.Collections.Generic;
using UnityEngine;

public static class UIShapes
{
    private const int Supersample = 4;

    private static readonly Dictionary<(int, int), Sprite> rings = new();

    public static Sprite Ring(float radius, float thickness)
    {
        (int, int) key = (Mathf.RoundToInt(radius * 10f), Mathf.RoundToInt(thickness * 10f));

        if (rings.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        int border = Mathf.CeilToInt(radius) + 1;
        int size = border * 2 + 2;

        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = $"Ring_{radius}_{thickness}",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float coverage = 0f;

                for (int sy = 0; sy < Supersample; sy++)
                {
                    for (int sx = 0; sx < Supersample; sx++)
                    {
                        float px = x + (sx + 0.5f) / Supersample - half;
                        float py = y + (sy + 0.5f) / Supersample - half;
                        float outer = RoundedBox(px, py, half, half, radius);

                        if (outer <= 0f && outer > -thickness)
                            coverage += 1f;
                    }
                }

                byte alpha = (byte)Mathf.RoundToInt(coverage / (Supersample * Supersample) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));

        sprite.name = texture.name;
        rings[key] = sprite;
        return sprite;
    }

    private static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
    {
        float qx = Mathf.Abs(x) - (halfWidth - radius);
        float qy = Mathf.Abs(y) - (halfHeight - radius);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
    }
}
