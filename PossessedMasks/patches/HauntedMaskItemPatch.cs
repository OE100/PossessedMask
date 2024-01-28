using HarmonyLib;
using PossessedMasks.mono;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(HauntedMaskItem))]
public class HauntedMaskItemPatch
{
    [HarmonyPatch(nameof(HauntedMaskItem.DiscardItem)), HarmonyPrefix]
    private static void DiscardItemPrefix(HauntedMaskItem __instance)
    {
        if (Utils.HostCheck)
        {
            if (Utils.InLevel)
            {
                var crawlingComponent = __instance.gameObject.GetComponent<CrawlingComponent>();
                if (crawlingComponent)
                    crawlingComponent.Inside = __instance.playerHeldBy.isInsideFactory;
                else
                    __instance.gameObject.AddComponent<CrawlingComponent>().Inside =
                        __instance.playerHeldBy.isInsideFactory;
            }
        }
    }
}