using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PossessedMasksRewrite;

public static class Utils
{
    public static Item TragedyItem;
    public static Item ComedyItem;
    public static Dictionary<Type, GameObject> EnemyPrefabRegistry = new();
    
    public static List<AudioClip> PossessionSounds = [];
    public static List<AudioClip> SlotSwitchSounds = [];

    public static Terminal Terminal;
    private static bool _registered;

    public static bool HostCheck => NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
    public static bool InLevel =>
        StartOfRound.Instance && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevelID != 3;
    
    public static void RegisterEnemyPrefab(Type enemyAI, GameObject prefab)
    {
        if (!typeof(EnemyAI).IsAssignableFrom(enemyAI)) return;
        EnemyPrefabRegistry.TryAdd(enemyAI, prefab);
    }
    
    public static void RegisterAll()
    {
        if (_registered || !Terminal || !StartOfRound.Instance) return;
        _registered = true;
        
        Plugin.Log.LogDebug("Registering all!");
        
        ComedyItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(item => item.itemName == "Comedy");
        TragedyItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(item => item.itemName == "Tragedy");
        
        // set the base value of the comedy and tragedy items
        if (!ComedyItem)
            Plugin.Log.LogDebug("Comedy item not found!");
        else
        {
            Plugin.Log.LogDebug("Comedy item found!");
            ComedyItem.minValue = ModConfig.MinMaskItemBaseValue.Value;
            ComedyItem.maxValue = ModConfig.MaxMaskItemBaseValue.Value;
        }
        
        if (!TragedyItem)
            Plugin.Log.LogDebug("Tragedy item not found!");
        else
        {
            Plugin.Log.LogDebug("Tragedy item found!");
            TragedyItem.minValue = ModConfig.MinMaskItemBaseValue.Value;
            TragedyItem.maxValue = ModConfig.MaxMaskItemBaseValue.Value;
        }
        
        var moons = Terminal.moonsCatalogueList;
        var addPercent = ModConfig.MaskRarityScalingMultiplier.Value;
        
        foreach (var level in moons)
        {
            Plugin.Log.LogDebug($"Registering on level: {level.PlanetName}");
            var rarity = Mathf.Clamp(Mathf.RoundToInt(ModConfig.MaskRarity.Value + 
                                                      (ModConfig.MaskRarityScaling.Value ? addPercent * level.maxTotalScrapValue / 100 : 0)), 0, 100);
            // register enemy prefabs to dictionary
            level.Enemies.ForEach(spawnable =>
            {
                var prefab = spawnable.enemyType.enemyPrefab;
                RegisterEnemyPrefab(prefab.GetComponent<EnemyAI>().GetType(), prefab);
            });

            // make masks spawnable on all moons and change rarity
            if (ComedyItem)
            {
                var comedyWithRarity = new SpawnableItemWithRarity
                {
                    spawnableItem = ComedyItem,
                    rarity = rarity
                };

                var levelIndex =
                    level.spawnableScrap.FindIndex(item => item.spawnableItem.itemName == ComedyItem.itemName);
                
                if (levelIndex == -1 && ModConfig.EnableChangeMaskSpawnChance.Value == 2)
                    level.spawnableScrap.Add(comedyWithRarity);
                else
                    level.spawnableScrap[levelIndex] = comedyWithRarity;
            }
            
            if (TragedyItem)
            {
                var tragedyWithRarity = new SpawnableItemWithRarity
                {
                    spawnableItem = TragedyItem,
                    rarity = rarity
                };

                var levelIndex =
                    level.spawnableScrap.FindIndex(item => item.spawnableItem.itemName == TragedyItem.itemName);
                
                if (levelIndex == -1 && ModConfig.EnableChangeMaskSpawnChance.Value == 2)
                    level.spawnableScrap.Add(tragedyWithRarity);
                else
                    level.spawnableScrap[levelIndex] = tragedyWithRarity;
            }
        }
    }

    public static void PlayRandomAudioClipFromList(AudioSource source, List<AudioClip> clips, float volumeScale = 1f)
    {
        source.PlayOneShot(clips[Random.Range(0, clips.Count)], volumeScale);
    }
    
    public static int MathMod(int muduli, int modulus) => ((muduli % modulus) + modulus) % modulus;
    
    public static bool IsActivePlayer(PlayerControllerB player) => player && player.isPlayerControlled && !player.isPlayerDead;

    public static int ItemCount(PlayerControllerB player) => 
        player.ItemSlots.Count(item => item != null);
}