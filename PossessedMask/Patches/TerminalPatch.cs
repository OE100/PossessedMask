using System;
using System.Linq;
using HarmonyLib;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    public class TerminalPatch
    {
        internal static SpawnableItemWithRarity comedy = null;
        internal static SpawnableItemWithRarity tragedy = null;
        
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void PatchSelectableLevels(Terminal __instance)
        {
            SelectableLevel dine;
            try
            {
                dine = __instance.moonsCatalogueList.First(level => level.PlanetName == "7 Dine");
            } catch (InvalidOperationException)
            {
                Plugin.Log.LogWarning("7 Dine not found in selectable levels, skipping patching min and max values of Comedy and Tragedy");
                return;
            }
            
            try
            {
                comedy = dine.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Comedy");
                comedy.spawnableItem.minValue = Plugin.minMaskItemBaseValue.Value;
                comedy.spawnableItem.maxValue = Plugin.maxMaskItemBaseValue.Value;
            } catch (InvalidOperationException)
            {
                Plugin.Log.LogWarning("Comedy not found in 7 Dine, skipping patching min and max values of Comedy");
            }
            
            try
            {
                tragedy = dine.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Tragedy");
                tragedy.spawnableItem.minValue = Plugin.minMaskItemBaseValue.Value;
                tragedy.spawnableItem.maxValue = Plugin.maxMaskItemBaseValue.Value;
            }
            catch (InvalidOperationException)
            {
                Plugin.Log.LogWarning("Tragedy not found in 7 Dine, skipping patching min and max values of Tragedy");
            }

            if (tragedy == null && comedy == null)
            {
                Plugin.Log.LogWarning("Both masks not found on diner, skipping patching min and max values of Comedy and Tragedy");
                return;
            }
            
            foreach (SelectableLevel level in __instance.moonsCatalogueList)
            {
                int comedyInd = level.spawnableScrap.FindIndex(rarity => rarity.spawnableItem.itemName == "Comedy");
                int tragedyInd = level.spawnableScrap.FindIndex(rarity => rarity.spawnableItem.itemName == "Tragedy");

                if (tragedy != null)
                {
                    if (tragedyInd == -1)
                        level.spawnableScrap.Add(tragedy);
                    else
                        level.spawnableScrap[tragedyInd] = tragedy;
                }

                if (comedy != null)
                {
                    if (comedyInd == -1)
                        level.spawnableScrap.Add(comedy);
                    else
                        level.spawnableScrap[comedyInd] = comedy;
                }
            }
        }
    }
}