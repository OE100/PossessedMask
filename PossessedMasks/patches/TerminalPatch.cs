using HarmonyLib;
using Unity.Netcode;

namespace PossessedMasksRewrite.patches;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatch
{
    [HarmonyPatch(nameof(Terminal.Awake)), HarmonyPostfix]
    private static void StartPostfix(Terminal __instance)
    {
        Plugin.Log.LogDebug("Terminal Awake");
        Utils.Terminal = __instance;
        // register all if not already done
        Utils.RegisterAll();
    }
}