using HarmonyLib;

namespace PossessedMasksRewrite.patches;

[HarmonyPatch(typeof(HUDManager))]
public class HUDManagerPatch
{
    public static int PlayerObjectId { get; private set; }
    
    [HarmonyPatch(nameof(HUDManager.SetSavedValues)), HarmonyPrefix]
    private static void SetSavedValuesPrefix(int playerObjectId)
    {
        PlayerObjectId = playerObjectId;
    }
}