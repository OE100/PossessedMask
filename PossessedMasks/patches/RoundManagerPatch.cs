using HarmonyLib;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(RoundManager))]
public class RoundManagerPatch
{
    [HarmonyPatch(nameof(RoundManager.FinishGeneratingLevel)), HarmonyPostfix]
    private static void FinishGeneratingLevelPostfix(RoundManager __instance)
    {
        Utils.InsideAINodes = __instance.insideAINodes;
        Utils.OutsideAINodes = __instance.outsideAINodes;
    }
}