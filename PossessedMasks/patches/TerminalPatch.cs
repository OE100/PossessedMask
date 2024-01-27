using HarmonyLib;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatch
{
    [HarmonyPatch(nameof(Terminal.Start)), HarmonyPostfix]
    private static void StartPostfix(Terminal __instance)
    {
        // set the terminal
        Utils.Terminal = __instance;
        
        // host check
        if (Utils.HostCheck)
        {
            // register all if not already done
            Utils.RegisterAll();
        }
    }
}