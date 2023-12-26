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
            
            foreach (SelectableLevel level in __instance.moonsCatalogueList)
            {
                if (!level.Enemies.Exists(enemy => enemy.enemyType.name == name))
                {
                    level.Enemies.Add(maskedEnemyZeroChance);
                }
            }
        }
    }
}