using UnityEngine;
using UnityEngine.Rendering;

public sealed class CourtMaterials
{
    public static readonly Color Cemento = Hex("#5F5D7C");
    public static readonly Color CementoLuz = Hex("#7A7392");
    public static readonly Color CementoSombra = Hex("#585674");
    public static readonly Color CementoMedio = Hex("#666485");
    public static readonly Color Grieta = Hex("#4C4A68");
    public static readonly Color Llave = Hex("#41598F");
    public static readonly Color Lineas = Hex("#EFE7D8");

    public static readonly Color Tablero = Hex("#EDE4D0");
    public static readonly Color TableroGastado = Hex("#C8B99A");
    public static readonly Color TableroSombra = Hex("#E3D8C0");
    public static readonly Color TableroBorde = Hex("#B9AB8C");
    public static readonly Color Oxido = Hex("#D4B697");
    public static readonly Color Recuadro = Hex("#E3701F");

    public static readonly Color Aro = Hex("#E0452B");
    public static readonly Color Soporte = Hex("#B8381F");
    public static readonly Color Cadena = Hex("#D5DAE3");
    public static readonly Color AroRed = Hex("#AEB5C2");

    public static readonly Color Poste = Hex("#2A2140");
    public static readonly Color PosteFilo = Hex("#4A3B6B");
    public static readonly Color HubLado = Hex("#3A2D5C");
    public static readonly Color HubTapa = Hex("#5A4A86");

    public static readonly Color Reja = Hex("#241540");
    public static readonly Color Arboles = Hex("#2E1B4D");
    public static readonly Color EdificiosCerca = Hex("#4A2468");
    public static readonly Color EdificiosLejos = Hex("#7A3382");

    public static readonly Color CieloCenit = Hex("#2B1B5E");
    public static readonly Color CieloVioleta = Hex("#6A2C8C");
    public static readonly Color CieloRosa = Hex("#C9437A");
    public static readonly Color CieloHorizonte = Hex("#F2793A");
    public static readonly Color Sol = Hex("#FFD071");
    public static readonly Color SolHalo = Hex("#FFB35C", 0.35f);
    public static readonly Color Nube1 = Hex("#B23C7E", 0.7f);
    public static readonly Color Nube2 = Hex("#D9527C", 0.7f);
    public static readonly Color Nube3 = Hex("#EE6F6A", 0.7f);

    public static readonly Color Pelota = Hex("#F28A1E");
    public static readonly Color PelotaCostura = Hex("#3B1A0A");

    public static readonly Color SombraTinte = Hex("#2A2140");
    public static readonly Color Ambiente = Hex("#6A5A9E");
    public static readonly Color LuzSol = Hex("#FFC58A");
    public static readonly Color Filo = Hex("#FFD9A0");
    public static readonly Color Exito = Hex("#40D959");
    public static readonly Color CheckTrazo = Hex("#10241A");
    public static readonly Color SombraPlana = Hex("#2A2140", 0.18f);
    public static readonly Color SombraPelota = Hex("#2A2140", 0.35f);

    public Material MCemento, MCementoLuz, MCementoSombra, MCementoMedio, MGrieta, MLlave, MLineas;
    public Material MTablero, MTableroGastado, MTableroSombra, MTableroBorde, MOxido, MRecuadro, MFilo;
    public Material MAro, MSoporte, MCadena, MAroRed;
    public Material MPoste, MPosteFilo, MHubLado, MHubTapa;
    public Material MReja, MArboles, MEdificiosCerca, MEdificiosLejos;
    public Material MCielo, MSol, MSolHalo, MNube1, MNube2, MNube3;
    public Material MPelota, MPelotaCostura, MPelotaContorno;
    public Material MSombraPlana, MSombraPelota, MBrilloAro, MExito, MCheckTrazo;

    private Shader toon;
    private Shader unlit;
    private Shader outline;

    public static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        color.a = alpha;
        return color;
    }

    public static CourtMaterials Build()
    {
        CourtMaterials m = new()
        {
            toon = CourtAssets.FindShader("Court/FlatToon"),
            unlit = CourtAssets.FindShader("Court/FlatUnlit"),
            outline = CourtAssets.FindShader("Court/Outline")
        };

        m.MCemento = m.Toon("M_Cemento", Cemento);
        m.MCementoLuz = m.Toon("M_CementoLuz", CementoLuz, decal: 1);
        m.MCementoSombra = m.Toon("M_CementoSombra", CementoSombra, decal: 1);
        m.MCementoMedio = m.Toon("M_CementoMedio", CementoMedio, decal: 1);
        m.MGrieta = m.Toon("M_Grieta", Grieta, decal: 3);
        m.MLlave = m.Toon("M_Llave", Llave, decal: 2);
        m.MLineas = m.Toon("M_Lineas", Lineas, decal: 4);

        m.MTablero = m.Graphic("M_Tablero", Tablero);
        m.MTableroGastado = m.Graphic("M_TableroGastado", TableroGastado);
        m.MTableroSombra = m.Graphic("M_TableroSombra", TableroSombra);
        m.MTableroBorde = m.Graphic("M_TableroBorde", TableroBorde);
        m.MOxido = m.Graphic("M_Oxido", Oxido);
        m.MRecuadro = m.Graphic("M_Recuadro", Recuadro);
        m.MFilo = m.Graphic("M_Filo", Filo);
        m.MSoporte = m.Toon("M_Soporte", Soporte);
        m.MPoste = m.Graphic("M_Poste", Poste);
        m.MPosteFilo = m.Graphic("M_PosteFilo", PosteFilo);
        m.MHubLado = m.Toon("M_HubLado", HubLado);
        m.MHubTapa = m.Graphic("M_HubTapa", HubTapa);

        m.MAro = m.Toon("M_Aro", Aro, rim: true);
        m.MCadena = m.Toon("M_Cadena", Cadena, vertexColor: true, shadowStrength: 0.15f);
        m.MCadena.SetFloat("_Cull", (float)CullMode.Off);
        m.MAroRed = m.Toon("M_AroRed", AroRed, shadowStrength: 0.15f);

        m.MReja = m.Graphic("M_Reja", Reja);
        m.MArboles = m.Toon("M_Arboles", Arboles, shadowStrength: 0.15f);
        m.MEdificiosCerca = m.Graphic("M_EdificiosCerca", EdificiosCerca);
        m.MEdificiosLejos = m.Graphic("M_EdificiosLejos", EdificiosLejos);

        m.MCielo = m.Unlit("M_Cielo", Color.white, vertexColor: true, queue: 2450);
        m.MSol = m.Unlit("M_Sol", Sol, queue: 2450);
        m.MSolHalo = m.Transparent("M_SolHalo", SolHalo, 3000);
        m.MNube1 = m.Transparent("M_Nube1", Nube1, 3001);
        m.MNube2 = m.Transparent("M_Nube2", Nube2, 3001);
        m.MNube3 = m.Transparent("M_Nube3", Nube3, 3001);

        m.MPelota = m.Toon("M_Pelota", Pelota, rim: true, shadowStrength: 0.18f, threshold: -0.1f);
        m.MPelotaCostura = m.Graphic("M_PelotaCostura", PelotaCostura);
        m.MPelotaContorno = CourtAssets.SaveMaterial("M_PelotaContorno", m.outline, mat =>
        {
            mat.SetColor("_BaseColor", PelotaCostura);
            mat.SetFloat("_OutlineWidth", 0.022f);
        });

        m.MSombraPlana = m.Transparent("M_SombraPlana", SombraPlana, 2990, offset: true);
        m.MSombraPelota = m.Transparent("M_SombraPelota", SombraPelota, 2991, offset: true);
        m.MBrilloAro = m.Transparent("M_BrilloAro", Hex("#40D959", 0.35f), 3002);
        m.MExito = m.Unlit("M_Exito", Exito);
        m.MCheckTrazo = m.Unlit("M_CheckTrazo", CheckTrazo);

        return m;
    }

    private Material Toon(
        string name,
        Color color,
        int decal = 0,
        bool rim = false,
        bool vertexColor = false,
        float shadowStrength = 0.3f,
        float threshold = 0f)
    {
        return CourtAssets.SaveMaterial(name, toon, mat =>
        {
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_ShadowTint", SombraTinte);
            mat.SetColor("_AmbientColor", Ambiente);
            mat.SetColor("_RimColor", Filo);
            mat.SetFloat("_ShadowStrength", shadowStrength);
            mat.SetFloat("_LightThreshold", threshold);
            mat.SetFloat("_LightSoftness", 0.06f);
            mat.SetFloat("_LightInfluence", 0.25f);
            mat.SetFloat("_AmbientInfluence", 0.15f);
            mat.SetFloat("_Cull", (float)CullMode.Back);
            SetKeyword(mat, "_RIM", "_UseRim", rim);
            SetKeyword(mat, "_VERTEX_COLOR", "_UseVertexColor", vertexColor);

            mat.SetFloat("_OffsetFactor", -decal);
            mat.SetFloat("_OffsetUnits", -decal);
            mat.renderQueue = 2000 + decal;
        });
    }

    private Material Graphic(string name, Color color)
    {
        Material mat = Toon(name, color);
        mat.SetFloat("_LightThreshold", -1f);
        mat.SetFloat("_LightSoftness", 0.001f);
        mat.SetFloat("_LightInfluence", 0.1f);
        return mat;
    }

    private Material Unlit(string name, Color color, bool vertexColor = false, int queue = 2000)
    {
        return CourtAssets.SaveMaterial(name, unlit, mat =>
        {
            mat.SetColor("_BaseColor", color);
            SetKeyword(mat, "_VERTEX_COLOR", "_UseVertexColor", vertexColor);
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.SetFloat("_OffsetFactor", 0f);
            mat.SetFloat("_OffsetUnits", 0f);
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = queue;
        });
    }

    private Material Transparent(string name, Color color, int queue, bool offset = false)
    {
        return CourtAssets.SaveMaterial(name, unlit, mat =>
        {
            mat.SetColor("_BaseColor", color);
            SetKeyword(mat, "_VERTEX_COLOR", "_UseVertexColor", false);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_OffsetFactor", offset ? -4f : 0f);
            mat.SetFloat("_OffsetUnits", offset ? -4f : 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = queue;
        });
    }

    private static void SetKeyword(Material mat, string keyword, string toggleProperty, bool on)
    {
        mat.SetFloat(toggleProperty, on ? 1f : 0f);

        if (on)
            mat.EnableKeyword(keyword);
        else
            mat.DisableKeyword(keyword);
    }
}
