using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerBPatch
    {
        [HarmonyPatch("Discard_performed")]
        [HarmonyPrefix]
        private static bool PatchDiscard(PlayerControllerB __instance, InputAction.CallbackContext context)
        {
            var current = __instance.ItemSlots[__instance.currentItemSlot];
            if (current == null || current.GetType() != typeof(HauntedMaskItem))
                return true;
            if (__instance.ItemSlots.Count(slot => slot != null) < Plugin.numberOfSlotsFilledToEnableDroppingMask.Value)
                return false;
            return true;
        }
        
        [HarmonyPatch("SwitchToItemSlot")]
        [HarmonyPrefix]
        private static bool PatchSwitchToItemSlot(PlayerControllerB __instance, int slot, GrabbableObject fillSlotWithItem = null)
        {
            GrabbableObject current = __instance.ItemSlots[__instance.currentItemSlot];
            if (current != null && current.GetType() == typeof(HauntedMaskItem) && __instance.activatingItem)
            {
                return false;
            }
            return true;
        }
    }
}