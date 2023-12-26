using HarmonyLib;

namespace PossessedMask.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    public class TerminalPatch
    {
        [HarmonyPatch("Start"), HarmonyPostfix]
        private static void PatchStart(Terminal __instance)
        {
            string name = "MaskedPlayerEnemy";
            SpawnableEnemyWithRarity maskedEnemyZeroChance = new SpawnableEnemyWithRarity();
            maskedEnemyZeroChance.rarity = 0;
            
            foreach (SelectableLevel level in __instance.moonsCatalogueList)
            {
                foreach (SpawnableEnemyWithRarity enemyWithRarity in level.Enemies)
                {
                    if (enemyWithRarity.enemyType.name == name)
                    {
                        maskedEnemyZeroChance.enemyType = enemyWithRarity.enemyType;
                        break;
                    }
                }

                if (maskedEnemyZeroChance.enemyType != null)
                    break;
            }

            if (maskedEnemyZeroChance.enemyType == null)
            {
                Plugin.Log.LogError("Masked enemy isn't allowed to spawn on any level!");
                return;
            }
            
            Plugin.Log.LogMessage($"Masked enemy found: {maskedEnemyZeroChance.enemyType.name}, fixing AI on all levels...");
            foreach (SelectableLevel level in __instance.moonsCatalogueList)
            {
                if (!level.Enemies.Exists(enemy => enemy.enemyType.name == name))
                {
                    Plugin.Log.LogMessage($"Masked enemy not found on {level.PlanetName}, adding zero...");
                    level.Enemies.Add(maskedEnemyZeroChance);
                }
                else
                    Plugin.Log.LogMessage($"Masked enemy found on {level.PlanetName}.");
            }
        }
    }
}