using GameNetcodeStuff;
using HarmonyLib;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(PlayerControllerB))]
public class PlayerControllerBPatch
{
    [HarmonyPatch(nameof(PlayerControllerB.Discard_performed)), HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static bool Discard_performedPrefix(PlayerControllerB __instance)
    {
        var localPlayer = StartOfRound.Instance.localPlayerController;
        var heldObject = localPlayer.currentlyHeldObjectServer;
        if (Utils.InLevel && heldObject is HauntedMaskItem && Utils.ItemCount(localPlayer) < SharedConfig.ItemCount) 
            return false;
        return true;
    }
    
    [HarmonyPatch(nameof(PlayerControllerB.Interact_performed)), HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static bool Interact_performedPrefix(PlayerControllerB __instance)
    {
        var localPlayer = StartOfRound.Instance.localPlayerController;
        var heldObject = localPlayer.currentlyHeldObjectServer;
        if (Utils.InLevel && heldObject is HauntedMaskItem && 
            (localPlayer.activatingItem || Utils.ItemCount(localPlayer) < SharedConfig.ItemCount)) 
            return false;
        return true;
    }
}