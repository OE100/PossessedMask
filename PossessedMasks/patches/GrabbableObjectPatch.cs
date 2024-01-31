using HarmonyLib;
using PossessedMasks.machines.impl.mask;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(GrabbableObject))]
public class GrabbableObjectPatch
{
    [HarmonyPatch(nameof(GrabbableObject.Start)), HarmonyPostfix]
    private static void StartPostfix(GrabbableObject __instance)
    {
        if (!Utils.HostCheck) return;
        if (!SharedConfig.LurkingMechanicEnabled) return;

        if (__instance is HauntedMaskItem)
            __instance.gameObject.AddComponent<MaskStateManager>();
    }
}