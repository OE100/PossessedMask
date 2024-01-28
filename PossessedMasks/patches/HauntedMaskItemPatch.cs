using HarmonyLib;
using PossessedMasks.mono;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(HauntedMaskItem))]
public class HauntedMaskItemPatch
{
    [HarmonyPatch(nameof(HauntedMaskItem.DiscardItem)), HarmonyPrefix]
    private static void DiscardItemPostfix(HauntedMaskItem __instance)
    {
        if (!Utils.HostCheck) return;
        var crawlingComponent = __instance.gameObject.GetComponent<CrawlingComponent>();
        if (crawlingComponent)
            crawlingComponent.inside = __instance.playerHeldBy.isInsideFactory;
        else
            __instance.gameObject.AddComponent<CrawlingComponent>().inside = __instance.playerHeldBy.isInsideFactory;
    }
}