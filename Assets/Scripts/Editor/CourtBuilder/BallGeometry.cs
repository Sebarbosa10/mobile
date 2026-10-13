using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prefabs Ball (visual de la pelota) y BallShadow (blob en el piso).
///
/// La pelota es una icoesfera de nivel 2 (320 caras) facetada, de
/// diámetro 1: se cuelga de la pelota de gameplay, que ya está escalada
/// a 0.381. Las costuras son cintas apenas por encima de la superficie
/// (submesh 1). El contorno es un casco invertido aparte, con normales
/// suaves para que no se abra en las aristas.
/// </summary>
public static class BallGeometry
{
    private const float Radius = 0.5f;
    private const int Level = 2;

    public static GameObject BuildBall(CourtMaterials m)
    {
        List<Vector3[]> faces = Icosphere(Level);

        // Cuerpo facetado + costuras.
        MeshKit body = new(2);
        body.Sub(0);
        foreach (Vector3[] f in faces)
            body.Tri(f[0] * Radius, f[1] * Radius, f[2] * Radius, f[0] + f[1] + f[2]);

        body.Sub(1);
        AddSeams(body);

        // Contorno: misma icoesfera con vértices compartidos (normales suaves).
        MeshKit outline = new();
        Dictionary<Vector3Int, int> shared = new();

        int Shared(Vector3 p)
        {
            Vector3Int key = Vector3Int.RoundToInt(p * 10000f);
            if (!shared.TryGetValue(key, out int index))
            {
                index = outline.Vertex(p * Radius, p);
                shared[key] = index;
            }
            return index;
        }

        foreach (Vector3[] f in faces)
        {
            // Orden horario visto desde afuera (el frente de Unity); el
            // shader dibuja la cara de atrás (Cull Front).
            int a = Shared(f[0]), b = Shared(f[1]), c = Shared(f[2]);
            if (Vector3.Dot(Vector3.Cross(f[1] - f[0], f[2] - f[0]), f[0]) >= 0f)
                outline.Index(a, b, c);
            else
                outline.Index(a, c, b);
        }

        GameObject root = new("Ball");
        GameObject bodyObject = CourtAssets.MeshObject("Body", root.transform,
            CourtAssets.SaveMesh(body.ToMesh("Ball"), "Ball"), m.MPelota, m.MPelotaCostura);
        CourtAssets.MeshObject("Outline", root.transform,
            CourtAssets.SaveMesh(outline.ToMesh("BallOutline"), "Ball_Outline"), m.MPelotaContorno);

        BallColor color = root.AddComponent<BallColor>();
        SerializedObject so = new(color);
        so.FindProperty("bodyRenderer").objectReferenceValue = bodyObject.GetComponent<Renderer>();
        so.FindProperty("bodyMaterialIndex").intValue = 0;
        so.FindProperty("popTarget").objectReferenceValue = root.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        return CourtAssets.SavePrefab(root, "Ball");
    }

    public static GameObject BuildBallShadow(CourtMaterials m)
    {
        GameObject root = new("BallShadow");
        Mesh disc = CourtAssets.SaveMesh(CourtGeometry.Disc(20, Vector3.up).ToMesh("Blob"), "BallShadow_Blob");
        GameObject blob = CourtAssets.MeshObject("Blob", root.transform, disc, m.MSombraPelota);
        blob.transform.localScale = Vector3.one * 0.5f; // disco de radio 1 → diámetro 1

        BallBlobShadow shadow = root.AddComponent<BallBlobShadow>();
        SerializedObject so = new(shadow);
        so.FindProperty("size").floatValue = 0.55f;
        so.FindProperty("color").colorValue = CourtMaterials.SombraPelota;
        so.ApplyModifiedPropertiesWithoutUndo();

        return CourtAssets.SavePrefab(root, "BallShadow");
    }

    /// <summary>
    /// Costuras de pelota de básquet: ecuador, meridiano y dos curvas
    /// laterales. Todo girado para que en reposo se vean en tres cuartos
    /// (si no, las laterales quedarían de canto como líneas rectas).
    /// </summary>
    private static void AddSeams(MeshKit kit)
    {
        Quaternion orient = Quaternion.Euler(20f, 35f, 0f);
        const float side = 0.62f;
        float sideRadius = Mathf.Sqrt(1f - side * side);

        Func<float, Vector3>[] curves =
        {
            t => new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)),
            t => new Vector3(0f, Mathf.Cos(t), Mathf.Sin(t)),
            t => new Vector3(side, Mathf.Cos(t) * sideRadius, Mathf.Sin(t) * sideRadius),
            t => new Vector3(-side, Mathf.Cos(t) * sideRadius, Mathf.Sin(t) * sideRadius)
        };

        const int segments = 32;
        const float halfWidth = 0.015f;          // costura de ~3% del diámetro, como en el mockup
        const float lift = Radius * 1.012f;      // apenas por encima de las caras planas

        foreach (Func<float, Vector3> curve in curves)
        {
            for (int i = 0; i < segments; i++)
            {
                Vector3 p0 = orient * curve(i / (float)segments * Mathf.PI * 2f);
                Vector3 p1 = orient * curve((i + 1) / (float)segments * Mathf.PI * 2f);
                Vector3 outward = (p0 + p1).normalized;
                Vector3 across = Vector3.Cross(p1 - p0, outward).normalized * halfWidth;
                Vector3 a = p0.normalized * lift;
                Vector3 b = p1.normalized * lift;
                kit.Quad(a - across, b - across, b + across, a + across, outward);
            }
        }
    }

    /// <summary>Icoesfera unitaria: lista de triángulos (posiciones normalizadas).</summary>
    private static List<Vector3[]> Icosphere(int level)
    {
        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
        Vector3[] v =
        {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
            new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
            new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1)
        };

        int[] f =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        List<Vector3[]> faces = new();
        for (int i = 0; i < f.Length; i += 3)
            faces.Add(new[] { v[f[i]].normalized, v[f[i + 1]].normalized, v[f[i + 2]].normalized });

        for (int l = 0; l < level; l++)
        {
            List<Vector3[]> next = new();

            foreach (Vector3[] face in faces)
            {
                Vector3 ab = (face[0] + face[1]).normalized;
                Vector3 bc = (face[1] + face[2]).normalized;
                Vector3 ca = (face[2] + face[0]).normalized;
                next.Add(new[] { face[0], ab, ca });
                next.Add(new[] { face[1], bc, ab });
                next.Add(new[] { face[2], ca, bc });
                next.Add(new[] { ab, bc, ca });
            }

            faces = next;
        }

        return faces;
    }
}
