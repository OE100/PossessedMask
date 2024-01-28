using HarmonyLib;
using PossessedMasks.mono;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(HauntedMaskItem))]
public class HauntedMaskItemPatch
{
    [HarmonyPatch(nameof(HauntedMaskItem.DiscardItem)), HarmonyPrefix]
    private static void DiscardItemPrefix(HauntedMaskItem __instance)
    {
        Plugin.Log.LogDebug("DiscardItemPrefix");
        if (Utils.HostCheck)
        {
            Plugin.Log.LogDebug("DiscardItemPrefix: host check passed");
            var crawlingComponent = __instance.gameObject.GetComponent<CrawlingComponent>();
            if (crawlingComponent)
            {
                Plugin.Log.LogDebug("DiscardItemPrefix: crawling component found");
                crawlingComponent.Inside = __instance.playerHeldBy.isInsideFactory;
            }
            else
            {
                Plugin.Log.LogDebug("DiscardItemPrefix: crawling component not found");
                __instance.gameObject.AddComponent<CrawlingComponent>().Inside =
                    __instance.playerHeldBy.isInsideFactory;
            }
        }
    }
}