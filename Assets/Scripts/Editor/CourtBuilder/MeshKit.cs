using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class MeshKit
{
    private readonly List<Vector3> vertices = new();
    private readonly List<Vector3> normals = new();
    private readonly List<Color> colors = new();
    private readonly List<Vector2> uvs = new();
    private readonly List<List<int>> subMeshes = new();
    private readonly bool linearColors;
    private bool hasUVs;

    private int current;

    public MeshKit(int subMeshCount = 1)
    {
        for (int i = 0; i < subMeshCount; i++)
            subMeshes.Add(new List<int>());

        linearColors = PlayerSettings.colorSpace == ColorSpace.Linear;
    }

    public int TriangleCount
    {
        get
        {
            int count = 0;
            foreach (List<int> sub in subMeshes)
                count += sub.Count / 3;
            return count;
        }
    }

    public MeshKit Sub(int index)
    {
        current = index;
        return this;
    }

    public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 facing, Color? color = null)
    {
        Color col = color ?? Color.white;
        TriColors(a, b, c, col, col, col, facing);
    }

    public void TriColors(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc, Vector3 facing)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);

        if (normal.sqrMagnitude < 1e-12f)
            return;

        if (Vector3.Dot(normal, facing) < 0f)
        {
            (b, c) = (c, b);
            (cb, cc) = (cc, cb);
            normal = -normal;
        }

        normal.Normalize();
        List<int> indices = subMeshes[current];

        int start = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        colors.Add(ToShaderColor(ca));
        colors.Add(ToShaderColor(cb));
        colors.Add(ToShaderColor(cc));

        for (int i = 0; i < 3; i++)
        {
            normals.Add(normal);
            uvs.Add(Vector2.zero);
        }

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
    }

    public void TriUV(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 facing)
    {
        int start = vertices.Count;
        TriColors(a, b, c, Color.white, Color.white, Color.white, facing);

        if (vertices.Count == start)
            return;

        bool flipped = vertices[start + 1] != b;
        uvs[start] = ua;
        uvs[start + 1] = flipped ? uc : ub;
        uvs[start + 2] = flipped ? ub : uc;
        hasUVs = true;
    }

    public void QuadColors(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd, Vector3 facing)
    {
        TriColors(a, b, c, ca, cb, cc, facing);
        TriColors(a, c, d, ca, cc, cd, facing);
    }

    public int Vertex(Vector3 position, Vector3 normal)
    {
        vertices.Add(position);
        normals.Add(normal.normalized);
        colors.Add(ToShaderColor(Color.white));
        uvs.Add(Vector2.zero);
        return vertices.Count - 1;
    }

    public void Index(int a, int b, int c)
    {
        List<int> indices = subMeshes[current];
        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }

    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing, Color? color = null)
    {
        Tri(a, b, c, facing, color);
        Tri(a, c, d, facing, color);
    }

    public void Fan(IReadOnlyList<Vector3> points, Vector3 facing, Color? color = null)
    {
        for (int i = 1; i < points.Count - 1; i++)
            Tri(points[0], points[i], points[i + 1], facing, color);
    }

    public void SolidQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 solidCenter, Color? color = null)
    {
        Vector3 faceCenter = (a + b + c + d) * 0.25f;
        Quad(a, b, c, d, faceCenter - solidCenter, color);
    }

    public void Box(Vector3 center, Vector3 size, BoxFaces faces = BoxFaces.All, Quaternion? rotation = null)
    {
        Quaternion rot = rotation ?? Quaternion.identity;
        Vector3 h = size * 0.5f;

        Vector3 P(float x, float y, float z) => center + rot * new Vector3(x * h.x, y * h.y, z * h.z);

        Vector3 lbf = P(-1, -1, -1), rbf = P(1, -1, -1), rtf = P(1, 1, -1), ltf = P(-1, 1, -1);
        Vector3 lbb = P(-1, -1, 1), rbb = P(1, -1, 1), rtb = P(1, 1, 1), ltb = P(-1, 1, 1);

        if ((faces & BoxFaces.Front) != 0) SolidQuad(lbf, rbf, rtf, ltf, center);
        if ((faces & BoxFaces.Back) != 0) SolidQuad(lbb, rbb, rtb, ltb, center);
        if ((faces & BoxFaces.Left) != 0) SolidQuad(lbf, lbb, ltb, ltf, center);
        if ((faces & BoxFaces.Right) != 0) SolidQuad(rbf, rbb, rtb, rtf, center);
        if ((faces & BoxFaces.Top) != 0) SolidQuad(ltf, rtf, rtb, ltb, center);
        if ((faces & BoxFaces.Bottom) != 0) SolidQuad(lbf, rbf, rbb, lbb, center);
    }

    public void GroundRibbon(IReadOnlyList<Vector3> points, float width, float y, bool closed = false, IReadOnlyList<Vector2> gaps = null)
    {
        List<Vector3> path = new(points);

        if (closed)
            path.Add(points[0]);

        float total = 0f;
        for (int i = 0; i < path.Count - 1; i++)
            total += Vector3.Distance(path[i], path[i + 1]);

        float walked = 0f;
        float half = width * 0.5f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 a = path[i];
            Vector3 b = path[i + 1];
            float length = Vector3.Distance(a, b);

            if (length < 1e-5f)
                continue;

            float t0 = walked / total;
            float t1 = (walked + length) / total;
            walked += length;

            List<Vector2> pieces = new() { new Vector2(t0, t1) };

            if (gaps != null)
            {
                foreach (Vector2 gap in gaps)
                {
                    List<Vector2> next = new();
                    foreach (Vector2 piece in pieces)
                    {
                        if (gap.y <= piece.x || gap.x >= piece.y)
                        {
                            next.Add(piece);
                            continue;
                        }

                        if (gap.x > piece.x) next.Add(new Vector2(piece.x, gap.x));
                        if (gap.y < piece.y) next.Add(new Vector2(gap.y, piece.y));
                    }
                    pieces = next;
                }
            }

            Vector3 dir = (b - a) / length;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized * half;

            float overlap = Mathf.Min(half, length * 0.5f);

            foreach (Vector2 piece in pieces)
            {
                float s0 = Mathf.InverseLerp(t0, t1, piece.x);
                float s1 = Mathf.InverseLerp(t0, t1, piece.y);
                Vector3 p0 = a + (b - a) * s0 - dir * (s0 <= 0f ? overlap : 0f);
                Vector3 p1 = a + (b - a) * s1 + dir * (s1 >= 1f ? overlap : 0f);
                p0.y = y;
                p1.y = y;
                Quad(p0 - side, p1 - side, p1 + side, p0 + side, Vector3.up);
            }
        }
    }

    public Mesh ToMesh(string name, int subMeshLimit = -1)
    {
        if (subMeshLimit >= 0 && subMeshLimit < subMeshes.Count)
            subMeshes.RemoveRange(subMeshLimit, subMeshes.Count - subMeshLimit);

        Mesh mesh = new() { name = name };

        if (vertices.Count > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);

        if (hasUVs)
            mesh.SetUVs(0, uvs);

        mesh.subMeshCount = subMeshes.Count;

        for (int i = 0; i < subMeshes.Count; i++)
            mesh.SetTriangles(subMeshes[i], i, false);

        mesh.RecalculateBounds();
        return mesh;
    }

    private Color ToShaderColor(Color color) =>
        linearColors ? color.linear : color;
}

[System.Flags]
public enum BoxFaces
{
    Front = 1,
    Back = 2,
    Left = 4,
    Right = 8,
    Top = 16,
    Bottom = 32,
    All = Front | Back | Left | Right | Top | Bottom,
    NoBottom = All & ~Bottom,
    Visible = Front | Left | Right | Top
}
