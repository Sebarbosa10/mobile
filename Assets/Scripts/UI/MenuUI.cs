using UnityEngine;

public sealed class MenuUI : MonoBehaviour
{
    private UIManager ui;
    private MainMenuController controller;

    private MainMenuScreen mainMenu;
    private ChallengeSelectScreen challengeSelect;
    private CustomizeScreen customize;

    public static MenuUI Create(MainMenuController controller)
    {
        UIManager ui = UIManager.Create("MenuUI");

        MenuUI menu = ui.gameObject.AddComponent<MenuUI>();
        menu.ui = ui;
        menu.controller = controller;

        menu.mainMenu = ui.Register(MainMenuScreen.Create(ui.Root));
        menu.challengeSelect = ui.Register(ChallengeSelectScreen.Create(ui.Root));
        menu.customize = ui.Register(CustomizeScreen.Create(ui.Root));

        menu.mainMenu.PlayRequested += controller.PlayGame;
        menu.mainMenu.ChallengesRequested += menu.OpenChallenges;
        menu.mainMenu.CustomizeRequested += () => ui.ShowOnly(menu.customize);
        menu.customize.BackRequested += () => ui.ShowOnly(menu.mainMenu);

        menu.challengeSelect.BackRequested += menu.CloseChallenges;
        menu.challengeSelect.ChallengeSelected += definition => controller.PlayChallenge(definition.Number);

        ui.ShowOnly(menu.mainMenu);

        return menu;
    }

    private void OpenChallenges()
    {
        ui.ShowOnly(challengeSelect);
    }

    private void CloseChallenges()
    {
        ui.ShowOnly(mainMenu);
    }
}
