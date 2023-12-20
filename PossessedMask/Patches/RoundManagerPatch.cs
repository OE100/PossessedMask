using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    public class RoundManagerPatch
    {
        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPrefix]
        private static void PatchSpawnScrapInLevel(RoundManager __instance)
        {
            if (__instance.scrapValueMultiplier < 1f)
            {
                SpawnableItemWithRarity comedy = __instance.currentLevel.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Comedy");
                comedy.spawnableItem.minValue = Mathf.RoundToInt(comedy.spawnableItem.minValue / __instance.scrapValueMultiplier);
                comedy.spawnableItem.maxValue = Mathf.RoundToInt(comedy.spawnableItem.maxValue / __instance.scrapValueMultiplier);
                
                SpawnableItemWithRarity tragedy = __instance.currentLevel.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Tragedy");
                tragedy.spawnableItem.minValue = Mathf.RoundToInt(tragedy.spawnableItem.minValue / __instance.scrapValueMultiplier);
                tragedy.spawnableItem.maxValue = Mathf.RoundToInt(tragedy.spawnableItem.maxValue / __instance.scrapValueMultiplier);
            }
        }
        
        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPostfix]
        private static void PatchSpawnScrapInLevelPostfix(RoundManager __instance)
        {
            if (__instance.scrapValueMultiplier < 1f)
            {
                SpawnableItemWithRarity comedy = __instance.currentLevel.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Comedy");
                comedy.spawnableItem.minValue = Mathf.RoundToInt(comedy.spawnableItem.minValue * __instance.scrapValueMultiplier);
                comedy.spawnableItem.maxValue = Mathf.RoundToInt(comedy.spawnableItem.maxValue * __instance.scrapValueMultiplier);
                
                SpawnableItemWithRarity tragedy = __instance.currentLevel.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Tragedy");
                tragedy.spawnableItem.minValue = Mathf.RoundToInt(tragedy.spawnableItem.minValue * __instance.scrapValueMultiplier);
                tragedy.spawnableItem.maxValue = Mathf.RoundToInt(tragedy.spawnableItem.maxValue * __instance.scrapValueMultiplier);
            }
            
        }
    }
}