using System.Linq;
using HarmonyLib;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    public class TerminalPatch
    {
        
        
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void PatchSelectableLevels(Terminal __instance)
        {
            /*
            Plugin.Log.LogMessage("Debug:");
            foreach (string name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Plugin.Log.LogMessage("Resource: " + name);
            }
            */
            Plugin.Log.LogMessage($"Patching masks weights...");
            SelectableLevel dine = __instance.moonsCatalogueList.First(level => level.PlanetName == "7 Dine");
            SpawnableItemWithRarity comedyDine = dine.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Comedy");
            SpawnableItemWithRarity tragedyDine = dine.spawnableScrap.First(rarity => rarity.spawnableItem.itemName == "Tragedy");
            comedyDine.spawnableItem.minValue = tragedyDine.spawnableItem.minValue = 90;
            comedyDine.spawnableItem.maxValue = tragedyDine.spawnableItem.maxValue = 170;
            
            foreach (SelectableLevel level in __instance.moonsCatalogueList)
            {
                int comedyInd = level.spawnableScrap.FindIndex(rarity => rarity.spawnableItem.itemName == "Comedy");
                int tragedyInd = level.spawnableScrap.FindIndex(rarity => rarity.spawnableItem.itemName == "Tragedy");

                if (tragedyInd == -1)
                    level.spawnableScrap.Add(tragedyDine);
                else
                    level.spawnableScrap[tragedyInd] = tragedyDine;
                
                if (comedyInd == -1)
                    level.spawnableScrap.Add(comedyDine);
                else
                    level.spawnableScrap[comedyInd] = comedyDine;
            }
        }
    }
}