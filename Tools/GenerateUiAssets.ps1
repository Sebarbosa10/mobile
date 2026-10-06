<#
  Genera los sprites de la UI de "Última Luz" (PNG blancos para teñir con
  Image.color, salvo fondos), sus .meta de importación (Sprite 2D and UI,
  Full Rect, sin mipmaps, bordes 9-slice) y los assets UISkin / UIPalette
  que los referencian.

  Las medidas, radios y bordes salen del design system del mockup. Los
  GUID son deterministas (o se reusan si el .meta ya existe), así que se
  puede volver a correr sin romper referencias.

  Uso (desde la raíz del proyecto, con Unity cerrado o sin foco):
      powershell -ExecutionPolicy Bypass -File Tools\GenerateUiAssets.ps1
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class UltimaLuzUiGen
{
    // ---------- helpers de dibujo ----------

    static Bitmap New(int w, int h, out Graphics g)
    {
        var b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        g = Graphics.FromImage(b);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);
        return b;
    }

    static GraphicsPath RRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath();
        float d = r * 2f;
        p.AddArc(x, y, d, d, 180, 90);
        p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Color Hex(string hex, int alpha = 255)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(alpha,
            int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
            int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
            int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber));
    }

    static Pen Stroke(Color c, float width)
    {
        var p = new Pen(c, width);
        p.StartCap = LineCap.Round;
        p.EndCap = LineCap.Round;
        p.LineJoin = LineJoin.Round;
        return p;
    }

    // Íconos: se dibujan sobre la grilla de 24 del mockup, escalada a 96.
    static Bitmap Icon(Action<Graphics, Pen, Brush> draw, float strokeWidth = 2f)
    {
        Graphics g;
        var b = New(96, 96, out g);
        g.ScaleTransform(4f, 4f);
        using (var pen = Stroke(Color.White, strokeWidth))
        using (var brush = new SolidBrush(Color.White))
            draw(g, pen, brush);
        g.Dispose();
        return b;
    }

    // ---------- definición de sprites ----------

    public class SpriteDef
    {
        public string Name;
        public string Field;        // nombre del campo en UISkin
        public int L, T, R, B;      // bordes 9-slice
        public Func<Bitmap> Draw;
    }

    public static List<SpriteDef> Definitions()
    {
        var list = new List<SpriteDef>();
        Action<string, string, int, int, int, int, Func<Bitmap>> add =
            (name, field, l, t, r, b, draw) => list.Add(new SpriteDef { Name = name, Field = field, L = l, T = t, R = r, B = b, Draw = draw });

        add("btn_base", "buttonBase", 40, 40, 40, 52, () =>
        {
            Graphics g; var b = New(128, 140, out g);
            g.FillPath(new SolidBrush(Hex("#B3B3B3")), RRect(0, 12, 128, 128, 36));
            g.FillPath(Brushes.White, RRect(0, 0, 128, 128, 36));
            return b;
        });

        add("btn_base_pressed", "buttonPressed", 40, 40, 40, 44, () =>
        {
            Graphics g; var b = New(128, 132, out g);
            g.FillPath(new SolidBrush(Hex("#B3B3B3")), RRect(0, 4, 128, 128, 36));
            g.FillPath(Brushes.White, RRect(0, 0, 128, 128, 36));
            return b;
        });

        // Teñido a Noche (#1E1638) el labio queda en ~#120D24.
        add("panel", "panel", 48, 48, 48, 62, () =>
        {
            Graphics g; var b = New(144, 158, out g);
            g.FillPath(new SolidBrush(Hex("#999999")), RRect(0, 14, 144, 144, 48));
            g.FillPath(Brushes.White, RRect(0, 0, 144, 144, 48));
            return b;
        });

        add("panel_borde", "panelBorder", 48, 48, 48, 48, () =>
        {
            Graphics g; var b = New(144, 144, out g);
            var p = RRect(0, 0, 144, 144, 48);
            p.AddPath(RRect(8, 8, 128, 128, 40), false);
            p.FillMode = FillMode.Alternate;
            g.FillPath(Brushes.White, p);
            return b;
        });

        add("chip", "chip", 28, 28, 28, 28, () =>
        {
            Graphics g; var b = New(64, 64, out g);
            g.FillPath(Brushes.White, RRect(0, 0, 64, 64, 28));
            return b;
        });

        add("pill", "pill", 31, 31, 31, 31, () =>
        {
            Graphics g; var b = New(64, 64, out g);
            g.FillEllipse(Brushes.White, 0, 0, 64, 64);
            return b;
        });

        add("ring_340", "ring340", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(340, 340, out g);
            g.DrawEllipse(new Pen(Color.White, 22), 20, 20, 300, 300);
            return b;
        });

        add("ring_200", "ring200", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(200, 200, out g);
            g.DrawEllipse(new Pen(Color.White, 18), 14, 14, 172, 172);
            return b;
        });

        add("disc", "disc", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(256, 256, out g);
            g.FillEllipse(Brushes.White, 1, 1, 254, 254);
            return b;
        });

        add("pip_empty", "pipEmpty", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(48, 48, out g);
            g.DrawEllipse(new Pen(Color.White, 6), 3, 3, 42, 42);
            return b;
        });

        add("icon_pause", "iconPause", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            g.FillPath(br, RRect(5, 4, 5, 16, 1.5f));
            g.FillPath(br, RRect(14, 4, 5, 16, 1.5f));
        }));

        add("icon_lock", "iconLock", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            g.DrawPath(pen, RRect(5, 11, 14, 10, 2));
            var p = new GraphicsPath();
            p.AddLine(8, 11, 8, 7);
            p.AddArc(8, 3, 8, 8, 180, 180);
            p.AddLine(16, 7, 16, 11);
            g.DrawPath(pen, p);
        }));

        add("icon_trophy", "iconTrophy", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            var cup = new GraphicsPath();
            cup.AddLine(7, 4, 17, 4);
            cup.AddLine(17, 4, 17, 9);
            cup.AddArc(7, 4, 10, 10, 0, 180);
            cup.CloseFigure();
            g.DrawPath(pen, cup);

            var left = new GraphicsPath();
            left.AddLine(7, 6, 4, 6);
            left.AddLine(4, 6, 4, 7);
            left.AddArc(4, 4, 6, 6, 180, -90);
            g.DrawPath(pen, left);

            var right = new GraphicsPath();
            right.AddLine(17, 6, 20, 6);
            right.AddLine(20, 6, 20, 7);
            right.AddArc(14, 4, 6, 6, 0, 90);
            g.DrawPath(pen, right);

            g.DrawLine(pen, 12, 14, 12, 18);
            g.DrawLine(pen, 8, 20, 16, 20);
        }));

        add("icon_clock", "iconClock", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            g.DrawEllipse(pen, 4, 5, 16, 16);
            g.DrawLines(pen, new[] { new PointF(12, 9), new PointF(12, 13), new PointF(14.5f, 15.5f) });
            g.DrawLine(pen, 9.5f, 2.5f, 14.5f, 2.5f);
        }));

        add("icon_hoop", "iconHoop", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            g.DrawEllipse(pen, 3, 4, 18, 6);
            g.DrawLine(pen, 3, 7, 7, 20);
            g.DrawLine(pen, 21, 7, 17, 20);
            g.DrawLine(pen, 8, 9.7f, 9.5f, 20);
            g.DrawLine(pen, 16, 9.7f, 14.5f, 20);
            g.DrawLine(pen, 12, 10, 12, 20);
            g.DrawLine(pen, 7, 20, 17, 20);
        }));

        add("icon_shake", "iconShake", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            g.DrawPath(pen, RRect(8, 3, 8, 18, 2));
            g.DrawLine(pen, 4, 8, 4, 16);
            g.DrawLine(pen, 20, 8, 20, 16);
            g.DrawLine(pen, 1.5f, 10, 1.5f, 14);
            g.DrawLine(pen, 22.5f, 10, 22.5f, 14);
        }));

        add("icon_check", "iconCheck", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            g.DrawLines(pen, new[] { new PointF(5, 12.5f), new PointF(9.5f, 17), new PointF(19, 7.5f) });
        }, 3f));

        add("icon_play", "iconPlay", 0, 0, 0, 0, () => Icon((g, pen, br) =>
        {
            g.FillPolygon(br, new[] { new PointF(8, 5), new PointF(19, 12), new PointF(8, 19) });
        }));

        add("fx_burst", "fxBurst", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(512, 512, out g);
            float[] angles = { -75, -45, -15, 15, 45, 75, 105, -105 };
            float[] tips = { 210, 240, 200, 200, 240, 210, 220, 220 };
            float[] bases = { 120, 125, 120, 120, 125, 120, 130, 130 };
            for (int i = 0; i < angles.Length; i++)
            {
                g.ResetTransform();
                g.TranslateTransform(256, 256);
                g.RotateTransform(angles[i]);
                g.FillPolygon(Brushes.White, new[] { new PointF(0, -tips[i]), new PointF(13, -bases[i]), new PointF(-13, -bases[i]) });
            }
            return b;
        });

        add("fx_ring", "fxRing", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(256, 256, out g);
            g.DrawEllipse(new Pen(Color.White, 14), 14, 14, 228, 228);
            return b;
        });

        // Confeti triangular. El confeti rectangular del mockup es un Image
        // sin sprite, así que no necesita textura propia.
        add("fx_confetti_tri", "fxConfettiTriangle", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(48, 48, out g);
            g.FillPolygon(Brushes.White, new[] { new PointF(6, 8), new PointF(44, 20), new PointF(14, 42) });
            return b;
        });

        add("hint_chevron", "hintChevron", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(160, 96, out g);
            g.DrawLines(Stroke(Color.White, 18), new[] { new PointF(20, 78), new PointF(80, 18), new PointF(140, 78) });
            return b;
        });

        add("hint_finger", "hintFinger", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(160, 160, out g);
            g.DrawEllipse(new Pen(Color.FromArgb(128, 255, 255, 255), 8), 6, 6, 148, 148);
            g.FillEllipse(new SolidBrush(Color.FromArgb(230, 255, 255, 255)), 30, 30, 100, 100);
            return b;
        });

        // Fondos: ya van con color (no se tiñen).
        add("bg_skyline", "backgroundSkyline", 0, 0, 0, 0, () =>
        {
            Graphics g; var b = New(1080, 420, out g);
            g.FillPolygon(new SolidBrush(Hex("#3A2470")), Points(
                0,420, 0,150, 70,150, 70,90, 150,90, 150,170, 210,170, 210,40, 300,40, 300,120, 380,120, 380,200,
                700,200, 700,110, 780,110, 780,30, 860,30, 860,140, 940,140, 940,80, 1020,80, 1020,160, 1080,160, 1080,420));
            g.FillPolygon(new SolidBrush(Hex("#231650")), Points(
                0,420, 0,260, 120,260, 120,210, 200,210, 200,280, 330,280, 330,240, 420,240, 420,300, 660,300,
                660,250, 760,250, 760,285, 880,285, 880,225, 990,225, 990,275, 1080,275, 1080,420));
            return b;
        });

        add("sky_gradient", "backgroundSky", 0, 0, 0, 0, () =>
        {
            var b = new Bitmap(4, 256, PixelFormat.Format32bppArgb);
            Color[] stops = { Hex("#2B1B5E"), Hex("#6A2C8C"), Hex("#C9437A"), Hex("#F2793A") };
            float[] at = { 0f, 0.38f, 0.72f, 1f };
            for (int y = 0; y < 256; y++)
            {
                float t = y / 255f;
                int s = 0;
                while (s < at.Length - 2 && t > at[s + 1]) s++;
                float k = (t - at[s]) / (at[s + 1] - at[s]);
                Color c = Color.FromArgb(255,
                    (int)Math.Round(stops[s].R + (stops[s + 1].R - stops[s].R) * k),
                    (int)Math.Round(stops[s].G + (stops[s + 1].G - stops[s].G) * k),
                    (int)Math.Round(stops[s].B + (stops[s + 1].B - stops[s].B) * k));
                for (int x = 0; x < 4; x++)
                    b.SetPixel(x, y, c);
            }
            return b;
        });

        return list;
    }

    static PointF[] Points(params float[] xy)
    {
        var pts = new PointF[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++)
            pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
        return pts;
    }

    // ---------- GUID y .meta ----------

    public static string GuidFor(string metaPath, string seed)
    {
        if (File.Exists(metaPath))
        {
            foreach (var line in File.ReadAllLines(metaPath))
                if (line.StartsWith("guid: "))
                    return line.Substring(6).Trim();
        }
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("ultima-luz/" + seed));
            var sb = new StringBuilder();
            foreach (var x in hash) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    public static string SpriteMeta(string guid, SpriteDef d)
    {
        // spriteBorder: x = izquierda, y = abajo, z = derecha, w = arriba.
        return
"fileFormatVersion: 2\n" +
"guid: " + guid + "\n" +
"TextureImporter:\n" +
"  internalIDToNameTable: []\n" +
"  externalObjects: {}\n" +
"  serializedVersion: 13\n" +
"  mipmaps:\n" +
"    mipMapMode: 0\n" +
"    enableMipMap: 0\n" +
"    sRGBTexture: 1\n" +
"    linearTexture: 0\n" +
"    fadeOut: 0\n" +
"    borderMipMap: 0\n" +
"    mipMapsPreserveCoverage: 0\n" +
"    alphaTestReferenceValue: 0.5\n" +
"    mipMapFadeDistanceStart: 1\n" +
"    mipMapFadeDistanceEnd: 3\n" +
"  bumpmap:\n" +
"    convertToNormalMap: 0\n" +
"    externalNormalMap: 0\n" +
"    heightScale: 0.25\n" +
"    normalMapFilter: 0\n" +
"    flipGreenChannel: 0\n" +
"  isReadable: 0\n" +
"  streamingMipmaps: 0\n" +
"  streamingMipmapsPriority: 0\n" +
"  vTOnly: 0\n" +
"  ignoreMipmapLimit: 0\n" +
"  grayScaleToAlpha: 0\n" +
"  generateCubemap: 6\n" +
"  cubemapConvolution: 0\n" +
"  seamlessCubemap: 0\n" +
"  textureFormat: 1\n" +
"  maxTextureSize: 2048\n" +
"  textureSettings:\n" +
"    serializedVersion: 2\n" +
"    filterMode: 1\n" +
"    aniso: 1\n" +
"    mipBias: 0\n" +
"    wrapU: 1\n" +
"    wrapV: 1\n" +
"    wrapW: 1\n" +
"  nPOTScale: 0\n" +
"  lightmap: 0\n" +
"  compressionQuality: 50\n" +
"  spriteMode: 1\n" +
"  spriteExtrude: 1\n" +
"  spriteMeshType: 0\n" +
"  alignment: 0\n" +
"  spritePivot: {x: 0.5, y: 0.5}\n" +
"  spritePixelsToUnits: 100\n" +
"  spriteBorder: {x: " + d.L + ", y: " + d.B + ", z: " + d.R + ", w: " + d.T + "}\n" +
"  spriteGenerateFallbackPhysicsShape: 0\n" +
"  alphaUsage: 1\n" +
"  alphaIsTransparency: 1\n" +
"  spriteTessellationDetail: -1\n" +
"  textureType: 8\n" +
"  textureShape: 1\n" +
"  singleChannelComponent: 0\n" +
"  flipbookRows: 1\n" +
"  flipbookColumns: 1\n" +
"  maxTextureSizeSet: 0\n" +
"  compressionQualitySet: 0\n" +
"  textureFormatSet: 0\n" +
"  ignorePngGamma: 0\n" +
"  applyGammaDecoding: 0\n" +
"  swizzle: 50462976\n" +
"  cookieLightType: 0\n" +
"  platformSettings:\n" +
"  - serializedVersion: 4\n" +
"    buildTarget: DefaultTexturePlatform\n" +
"    maxTextureSize: 2048\n" +
"    resizeAlgorithm: 0\n" +
"    textureFormat: -1\n" +
"    textureCompression: 0\n" +
"    compressionQuality: 50\n" +
"    crunchedCompression: 0\n" +
"    allowsAlphaSplitting: 0\n" +
"    overridden: 0\n" +
"    ignorePlatformSupport: 0\n" +
"    androidETC2FallbackOverride: 0\n" +
"    forceMaximumCompressionQuality_BC6H_BC7: 0\n" +
"  spriteSheet:\n" +
"    serializedVersion: 2\n" +
"    sprites: []\n" +
"    outline: []\n" +
"    customData: \n" +
"    physicsShape: []\n" +
"    bones: []\n" +
"    spriteID: 5e97eb03825dee720800000000000000\n" +
"    internalID: 0\n" +
"    vertices: []\n" +
"    indices: \n" +
"    edges: []\n" +
"    weights: []\n" +
"    secondaryTextures: []\n" +
"    spriteCustomMetadata:\n" +
"      entries: []\n" +
"    nameFileIdTable: {}\n" +
"  mipmapLimitGroupName: \n" +
"  pSDRemoveMatte: 0\n" +
"  userData: \n" +
"  assetBundleName: \n" +
"  assetBundleVariant: \n";
    }

    // Genera todo y devuelve "campo=guid" por sprite.
    public static Dictionary<string, string> Run(string spritesDir)
    {
        Directory.CreateDirectory(spritesDir);
        var result = new Dictionary<string, string>();

        foreach (var d in Definitions())
        {
            string png = Path.Combine(spritesDir, d.Name + ".png");
            string meta = png + ".meta";

            using (var bmp = d.Draw())
                bmp.Save(png, ImageFormat.Png);

            string guid = GuidFor(meta, "Assets/UI/Sprites/" + d.Name + ".png");
            File.WriteAllText(meta, SpriteMeta(guid, d), new UTF8Encoding(false));
            result[d.Field] = guid;
        }

        return result;
    }
}
'@

function Write-Utf8([string]$path, [string]$text) {
    [IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), (New-Object Text.UTF8Encoding $false))
}

function Get-MetaGuid([string]$assetPath, [string]$seed) {
    return [UltimaLuzUiGen]::GuidFor("$assetPath.meta", $seed)
}

# ---------- sprites ----------

$spritesDir = Join-Path $root 'Assets\UI\Sprites'
$spriteGuids = [UltimaLuzUiGen]::Run($spritesDir)
Write-Host "Sprites generados: $($spriteGuids.Count)"

# ---------- fuentes (.meta con GUID fijo para poder referenciarlas) ----------

$fonts = [ordered]@{
    displayFont = @{ File = 'Anton-Regular.ttf';   Name = 'Anton' }
    bodyFont    = @{ File = 'Barlow-SemiBold.ttf'; Name = 'Barlow SemiBold' }
    labelFont   = @{ File = 'Barlow-Bold.ttf';     Name = 'Barlow' }
}

$fontGuids = @{}
foreach ($field in $fonts.Keys) {
    $f = $fonts[$field]
    $path = Join-Path $root "Assets\UI\Fonts\$($f.File)"
    $guid = Get-MetaGuid $path "Assets/UI/Fonts/$($f.File)"
    if (-not (Test-Path "$path.meta")) {
        Write-Utf8 "$path.meta" @"
fileFormatVersion: 2
guid: $guid
TrueTypeFontImporter:
  externalObjects: {}
  serializedVersion: 4
  fontSize: 16
  forceTextureCase: -2
  characterSpacing: 0
  characterPadding: 1
  includeFontData: 1
  fontNames:
  - $($f.Name)
  fallbackFontReferences: []
  customCharacters:
  fontRenderingMode: 0
  ascentCalculationMode: 1
  useLegacyBoundsCalculation: 0
  shouldRoundAdvanceValue: 1
  userData:
  assetBundleName:
  assetBundleVariant:

"@
    }
    $fontGuids[$field] = $guid
}

# ---------- scripts de UISkin / UIPalette (GUID fijo para los .asset) ----------

function Ensure-ScriptMeta([string]$relPath) {
    $path = Join-Path $root $relPath
    $guid = Get-MetaGuid $path ($relPath -replace '\\', '/')
    if (-not (Test-Path "$path.meta")) {
        Write-Utf8 "$path.meta" @"
fileFormatVersion: 2
guid: $guid
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:

"@
    }
    return $guid
}

$skinScriptGuid = Ensure-ScriptMeta 'Assets\Scripts\UI\Theme\UISkin.cs'
$paletteScriptGuid = Ensure-ScriptMeta 'Assets\Scripts\UI\Theme\UIPalette.cs'

# ---------- UIPalette.asset ----------

function ColorYaml([string]$hex, [double]$alpha = 1) {
    $h = $hex.TrimStart('#')
    $r = [Convert]::ToInt32($h.Substring(0, 2), 16) / 255
    $g = [Convert]::ToInt32($h.Substring(2, 2), 16) / 255
    $b = [Convert]::ToInt32($h.Substring(4, 2), 16) / 255
    $inv = [Globalization.CultureInfo]::InvariantCulture
    return '{r: ' + $r.ToString('0.######', $inv) + ', g: ' + $g.ToString('0.######', $inv) +
           ', b: ' + $b.ToString('0.######', $inv) + ', a: ' + $alpha.ToString('0.##', $inv) + '}'
}

$palette = [ordered]@{
    primary          = '#3269C2'; primaryLip = '#234A8A'; primaryPressed = '#2A59A5'
    secondary        = '#E3701F'; secondaryLip = '#9F4E16'; secondaryPressed = '#C15F1A'
    neutral          = '#4D4D4D'; neutralLip = '#363636'; neutralPressed = '#414141'
    success          = '#40D959'; onSuccess = '#10241A'
    urgent           = '#E63333'
    background       = '#2B1B5E'
    night            = '#1E1638'; nightLip = '#120D24'
    surface          = '#2A2140'
    locked           = '#3A3A46'; lockedLip = '#26262F'; lockedText = '#B9B9C2'; lockedSubtext = '#D6D6DC'
    sun              = '#FFD071'
    burst            = '#FFF3C4'
    track            = '#3A2D5C'
    pipEmpty         = '#8E86A8'
    labelMuted       = '#D6D0E8'
    confettiPink     = '#F04E8A'
    text             = '#FFFFFF'
}

$paletteDir = Join-Path $root 'Assets\UI\Resources'
New-Item -ItemType Directory -Force $paletteDir | Out-Null
$palettePath = Join-Path $paletteDir 'UIPalette.asset'
$paletteGuid = Get-MetaGuid $palettePath 'Assets/UI/Resources/UIPalette.asset'

$sb = New-Object Text.StringBuilder
[void]$sb.Append(@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $paletteScriptGuid, type: 3}
  m_Name: UIPalette
  m_EditorClassIdentifier:

"@)
foreach ($k in $palette.Keys) { [void]$sb.Append("  ${k}: $(ColorYaml $palette[$k])`n") }
[void]$sb.Append("  overlay: $(ColorYaml '#000000' 0.75)`n")
Write-Utf8 $palettePath $sb.ToString()
if (-not (Test-Path "$palettePath.meta")) {
    Write-Utf8 "$palettePath.meta" "fileFormatVersion: 2`nguid: $paletteGuid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
}

# ---------- UISkin.asset ----------

# Shader SDF móvil de TMP Essential Resources (GUID fijo del paquete): se
# referencia para que entre al build aunque los Font Assets se creen en runtime.
$tmpMobileSdfShaderGuid = 'fe393ace9b354375a9cb14cdbbc28be4'

$skinPath = Join-Path $paletteDir 'UISkin.asset'
$skinGuid = Get-MetaGuid $skinPath 'Assets/UI/Resources/UISkin.asset'

$sb = New-Object Text.StringBuilder
[void]$sb.Append(@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $skinScriptGuid, type: 3}
  m_Name: UISkin
  m_EditorClassIdentifier:
  palette: {fileID: 11400000, guid: $paletteGuid, type: 2}
  sdfShader: {fileID: 4800000, guid: $tmpMobileSdfShaderGuid, type: 3}

"@)
foreach ($field in $fontGuids.Keys) { [void]$sb.Append("  ${field}: {fileID: 12800000, guid: $($fontGuids[$field]), type: 3}`n") }
foreach ($field in ($spriteGuids.Keys | Sort-Object)) { [void]$sb.Append("  ${field}: {fileID: 21300000, guid: $($spriteGuids[$field]), type: 3}`n") }
Write-Utf8 $skinPath $sb.ToString()
if (-not (Test-Path "$skinPath.meta")) {
    Write-Utf8 "$skinPath.meta" "fileFormatVersion: 2`nguid: $skinGuid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
}

Write-Host "UISkin y UIPalette generados en Assets/UI/Resources"
