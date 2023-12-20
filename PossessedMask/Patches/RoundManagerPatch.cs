using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    public class RoundManagerPatch
    {
        internal static SpawnableItemWithRarity comedy = null;
        internal static SpawnableItemWithRarity tragedy = null;
        
        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPrefix]
        private static void PatchSpawnScrapInLevel(RoundManager __instance)
        {
            if (__instance.scrapValueMultiplier < 1f)
            {
                try
                {
                    comedy = __instance.currentLevel.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Comedy");
                } catch (InvalidOperationException)
                {
                    Plugin.Log.LogWarning("Comedy not found in current level, trying to add");
                    if (TerminalPatch.comedy != null)
                    {
                        Plugin.Log.LogInfo("Added successfully!");
                        __instance.currentLevel.spawnableScrap.Add(TerminalPatch.comedy);
                        comedy = TerminalPatch.comedy;
                    }
                    else
                    {
                        Plugin.Log.LogWarning("Comedy not found in 7 Dine, skipping...");
                        comedy = null;
                    }
                }

                if (comedy != null)
                {
                    comedy.spawnableItem.minValue = Mathf.RoundToInt(comedy.spawnableItem.minValue / __instance.scrapValueMultiplier);
                    comedy.spawnableItem.maxValue = Mathf.RoundToInt(comedy.spawnableItem.maxValue / __instance.scrapValueMultiplier);
                }
                
                try
                {
                    tragedy = __instance.currentLevel.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Tragedy");
                } catch (InvalidOperationException)
                {
                    Plugin.Log.LogWarning("Tragedy not found in current level, trying to add");
                    if (TerminalPatch.tragedy != null)
                    {
                        Plugin.Log.LogInfo("Added successfully!");
                        __instance.currentLevel.spawnableScrap.Add(TerminalPatch.tragedy);
                        tragedy = TerminalPatch.comedy;
                        tragedy.spawnableItem.minValue = Mathf.RoundToInt(tragedy.spawnableItem.minValue / __instance.scrapValueMultiplier);
                        tragedy.spawnableItem.maxValue = Mathf.RoundToInt(tragedy.spawnableItem.maxValue / __instance.scrapValueMultiplier);
                    }
                    else
                    {
                        Plugin.Log.LogWarning("Tragedy not found in 7 Dine, skipping...");
                        comedy = null;
                    }
                }

                if (tragedy != null)
                {
                    tragedy.spawnableItem.minValue = Mathf.RoundToInt(tragedy.spawnableItem.minValue / __instance.scrapValueMultiplier);
                    tragedy.spawnableItem.maxValue = Mathf.RoundToInt(tragedy.spawnableItem.maxValue / __instance.scrapValueMultiplier);
                }
            }
        }
        
        [HarmonyPatch("SpawnScrapInLevel")]
        [HarmonyPostfix]
        private static void PatchSpawnScrapInLevelPostfix(RoundManager __instance)
        {
            if (__instance.scrapValueMultiplier < 1f)
            {
                if (comedy != null)
                {
                    comedy.spawnableItem.minValue = Mathf.RoundToInt(comedy.spawnableItem.minValue * __instance.scrapValueMultiplier);
                    comedy.spawnableItem.maxValue = Mathf.RoundToInt(comedy.spawnableItem.maxValue * __instance.scrapValueMultiplier);
                }

                if (tragedy != null)
                {
                    tragedy.spawnableItem.minValue = Mathf.RoundToInt(tragedy.spawnableItem.minValue * __instance.scrapValueMultiplier);
                    tragedy.spawnableItem.maxValue = Mathf.RoundToInt(tragedy.spawnableItem.maxValue * __instance.scrapValueMultiplier);
                }
            }
            
        }
    }
}