using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SkinSetup
{
    [MenuItem("Tools/Skins/Build All")]
    public static void BuildAll()
    {
        if (CourtAssets.LoadPrefab("Ball") == null || CourtAssets.LoadPrefab("Court") == null)
        {
            Debug.LogError("[Skins] Primero corré Tools > Build Court Scene (faltan los prefabs de la escena).");
            return;
        }

        SkinCapture.RunInTempScene(report =>
        {
            report.AppendLine("Pelotas:");
            List<BallSkin> balls = BallSkinBuilder.BuildAll(report);

            report.AppendLine("Fondos:");
            List<BackgroundTheme> themes = ThemeBuilder.BuildAll(report);

            SkinDatabase db = SaveDatabase(balls, themes);
            AssetDatabase.SaveAssets();

            report.AppendLine("Atlas:");
            report.AppendLine(PaletteAtlasBuilder.Build());

            report.AppendLine("Capturas:");
            SkinCapture.CaptureBallIcons(db, report);
            SkinCapture.CaptureThemeThumbnails(db, report);
        });
    }

    private static SkinDatabase SaveDatabase(List<BallSkin> balls, List<BackgroundTheme> themes)
    {
        SkinDatabase db = ScriptableObject.CreateInstance<SkinDatabase>();
        db.balls = balls.Where(b => b != null).ToArray();
        db.themes = themes.Where(t => t != null).ToArray();
        db.previewShadowPrefab = CourtAssets.LoadPrefab("BallShadow");
        return SkinAssets.SaveAsset(db, SkinAssets.DatabasePath);
    }
}
