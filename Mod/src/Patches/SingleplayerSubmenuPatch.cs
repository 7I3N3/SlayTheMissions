using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;


namespace SlayTheMissions.Patches;

[HarmonyPatch(typeof(NSingleplayerSubmenu), nameof(NSingleplayerSubmenu._Ready))]
public static class SingleplayerSubmenuPatch
{
    private static NSubmenuButton _dailyButton;
    private static NSubmenuButton _customButton;

    [HarmonyPostfix]
    public static void PostFix(NSingleplayerSubmenu __instance)
    {
        Setup(__instance);

        MainMenuPatch.ModeToggled += OnModeToggled;
    }

    private static void Setup(NSingleplayerSubmenu submenu)
    {
        var dailyField = AccessTools.Field(typeof(NMainMenu), "_dailyButton");
        var customField = AccessTools.Field(typeof(NMainMenu), "_customButton");
        _dailyButton = dailyField.GetValue(submenu) as NSubmenuButton;
        _customButton = customField.GetValue(submenu) as NSubmenuButton;
    }

    private static void OnModeToggled(bool isNormalMode)
    {
        if (isNormalMode)
        {
            _dailyButton.SetEnabled(true);
            _customButton.SetEnabled(true);
        }
        else
        {
            _dailyButton.SetEnabled(false);
            _customButton.SetEnabled(false);
        }
    }
}