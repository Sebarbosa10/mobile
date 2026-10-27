using System;
using TMPro;
using UnityEngine;

public sealed class PauseScreen : UIScreen
{
    private OverlayPanel overlay;

    public event Action ResumeRequested;
    public event Action RestartRequested;
    public event Action ExitRequested;

    public static PauseScreen Create(RectTransform parent)
    {
        OverlayPanel overlay = OverlayPanel.Create(parent, "PauseScreen", 64f);

        PauseScreen screen = overlay.gameObject.AddComponent<PauseScreen>();
        screen.overlay = overlay;

        RectTransform content = overlay.Content;

        TextMeshProUGUI title = UIKit.CreateText(
            content, "Title", "PAUSA", UIFont.Display, 160f, UIKit.Palette.text, 6f);
        title.rectTransform.sizeDelta = new Vector2(720f, 184f);
        overlay.Add(title, 184f);

        UIButton resume = UIButton.Create(content, "ResumeButton", "REANUDAR", ButtonVariant.Primary);
        overlay.Add(resume, resume.LayoutHeight, 12f + 48f);
        resume.OnClick.AddListener(() => screen.ResumeRequested?.Invoke());
        resume.ClickSound = null; // al reanudar ya suena ui_pause_out

        UIButton restart = UIButton.Create(content, "RestartButton", "REINICIAR", ButtonVariant.Secondary);
        overlay.Add(restart, restart.LayoutHeight, 48f);
        restart.OnClick.AddListener(() => screen.RestartRequested?.Invoke());

        UIButton menu = UIButton.Create(content, "MenuButton", "MENÚ", ButtonVariant.Neutral);
        overlay.Add(menu, menu.LayoutHeight, 48f);
        menu.OnClick.AddListener(() => screen.ExitRequested?.Invoke());

        UIButton music = UIButton.Create(content, "MusicToggle", MusicLabel(), ButtonVariant.Neutral);
        overlay.Add(music, music.LayoutHeight, 48f);
        music.OnClick.AddListener(() =>
        {
            AudioManager.MusicVolume = AudioManager.MusicVolume > 0f ? 0f : 1f;
            music.SetText(MusicLabel());
        });

        UIButton sfx = UIButton.Create(content, "SfxToggle", SfxLabel(), ButtonVariant.Neutral);
        overlay.Add(sfx, sfx.LayoutHeight, 24f);
        sfx.OnClick.AddListener(() =>
        {
            AudioManager.SfxVolume = AudioManager.SfxVolume > 0f ? 0f : 1f;
            sfx.SetText(SfxLabel());
        });

        overlay.Finish(92f);

        return screen;
    }

    private static string MusicLabel() =>
        AudioManager.MusicVolume > 0f ? "MÚSICA: SÍ" : "MÚSICA: NO";

    private static string SfxLabel() =>
        AudioManager.SfxVolume > 0f ? "EFECTOS: SÍ" : "EFECTOS: NO";

    public override void Show()
    {
        overlay.Show();
    }

    public override void Hide()
    {
        overlay.Hide();
    }
}
