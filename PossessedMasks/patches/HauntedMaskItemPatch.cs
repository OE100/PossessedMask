using HarmonyLib;
using PossessedMasks.machines.impl.mask;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(HauntedMaskItem))]
public class HauntedMaskItemPatch
{
    [HarmonyPatch(nameof(HauntedMaskItem.Start)), HarmonyPostfix]
    private static void StartPostfix(HauntedMaskItem __instance)
    {
        if (!Utils.HostCheck) return;
        if (!SharedConfig.LurkingMechanicEnabled) return;
        
        __instance.gameObject.AddComponent<MaskStateManager>();
    }
}