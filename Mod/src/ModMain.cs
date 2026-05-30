using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace SlayTheMissions.Core;

[ModInitializer("ModInit")]
public static class ModMain
{
	public static void ModInit()
	{
		Harmony harmony = new("SlayTheMissions");
		harmony.PatchAll();
	}
}
