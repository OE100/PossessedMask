using GameNetcodeStuff;
using PossessedMasks.mono;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace PossessedMasks;

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

    public static List<GameObject> InsideAINodes;
    public static List<GameObject> OutsideAINodes;

    private static void RegisterEnemyPrefab(Type enemyAI, GameObject prefab)
    {
        if (!typeof(EnemyAI).IsAssignableFrom(enemyAI)) return;
        EnemyPrefabRegistry.TryAdd(enemyAI, prefab);
    }

    private static void SetComedyAndTragedy()
    {
        ComedyItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(item => item.itemName == "Comedy");
        TragedyItem = StartOfRound.Instance.allItemsList.itemsList.FirstOrDefault(item => item.itemName == "Tragedy");
        
        // set the base value of the comedy and tragedy items
        if (!ComedyItem)
            Plugin.Log.LogMessage("Comedy item not found!");
        else
        {
            Plugin.Log.LogMessage("Comedy item found!");
            ComedyItem.minValue = ModConfig.MinMaskItemBaseValue.Value;
            ComedyItem.maxValue = ModConfig.MaxMaskItemBaseValue.Value;
        }
        
        if (!TragedyItem)
            Plugin.Log.LogMessage("Tragedy item not found!");
        else
        {
            Plugin.Log.LogMessage("Tragedy item found!");
            TragedyItem.minValue = ModConfig.MinMaskItemBaseValue.Value;
            TragedyItem.maxValue = ModConfig.MaxMaskItemBaseValue.Value;
        }
    }

    private static void TweakSpawnChanceAndMoons()
    {
        var moons = Terminal.moonsCatalogueList;
        var addPercent = ModConfig.MaskRarityScalingMultiplier.Value;
        
        foreach (var level in moons)
        {
            var rarity = Mathf.Clamp(Mathf.RoundToInt(ModConfig.MaskRarity.Value + 
                                                      (ModConfig.MaskRarityScaling.Value ? addPercent * level.maxTotalScrapValue / 100 : 0)), 0, 100);
            // register enemy prefabs to dictionary
            level.Enemies.ForEach(spawnable =>
            {
                var prefab = spawnable.enemyType.enemyPrefab;
                RegisterEnemyPrefab(prefab.GetComponent<EnemyAI>().GetType(), prefab);
            });

            if (ModConfig.EnableChangeMaskSpawnChance.Value == 0) continue;
            
            // make masks spawnable on all moons and change rarity
            SetMoonSpawn(ComedyItem, rarity, level);
            SetMoonSpawn(TragedyItem, rarity, level);
        }

        return;

        void SetMoonSpawn(Item item, int rarity, SelectableLevel level)
        {
            if (!item) return;
            
            var itemWithRarity = new SpawnableItemWithRarity
            {
                spawnableItem = item,
                rarity = rarity
            };
            
            var levelIndex = level.spawnableScrap.FindIndex(lvlItem => lvlItem.spawnableItem.itemName == item.itemName);

            if (levelIndex != -1)
                level.spawnableScrap[levelIndex] = itemWithRarity;
            else if(ModConfig.EnableChangeMaskSpawnChance.Value == 2)
                level.spawnableScrap.Add(itemWithRarity);
        }
    }
    
    public static void RegisterAll()
    {
        if (_registered || Terminal == null || StartOfRound.Instance == null) return;
        _registered = true;
        
        Plugin.Log.LogMessage("Registering all!");
        
        SetComedyAndTragedy();
        
        TweakSpawnChanceAndMoons();
    }

    public static void PlayRandomAudioClipFromList(AudioSource source, List<AudioClip> clips, float volumeScale = 1f)
    {
        source.PlayOneShot(clips[Random.Range(0, clips.Count)], volumeScale);
    }
    
    public static int MathMod(int muduli, int modulus) => ((muduli % modulus) + modulus) % modulus;
    
    public static bool IsActivePlayer(PlayerControllerB player) => player && player.isPlayerControlled && !player.isPlayerDead;

    public static int ItemCount(PlayerControllerB player) => 
        player.ItemSlots.Count(item => item != null);

    public static List<PlayerControllerB> GetActivePlayers(bool inside)
    {
        return ServerManager.Instance ? ServerManager.Instance.ActivePlayers.FindAll(player => player.isInsideFactory == inside) : [];
    }
    
    public static (bool, T) FindFarthestAwayThingFromPosition<T>(Vector3 position, 
        List<T> things, Func<T, Vector3> getThingPosition)
    {
        if (!things.Any()) return (false, default);
        T farthestAwayThing = default;
        var farthestAwayThingDistance = Mathf.NegativeInfinity;
        var found = false;

        things.ForEach(thing =>
        {
            var distance = Vector3.Distance(position, getThingPosition(thing));
            if (!(distance > farthestAwayThingDistance)) return;
            farthestAwayThingDistance = distance;
            farthestAwayThing = thing;
            found = true;
        });
        
        return (found, farthestAwayThing);
    }
    
    public static (bool, T) FindClosestThingToPosition<T>(Vector3 position, 
        List<T> things, Func<T, Vector3> getThingPosition)
    {
        if (!things.Any()) return (false, default);
        T closestThing = default;
        var closestThingDistance = Mathf.Infinity;
        var found = false;

        things.ForEach(thing =>
        {
            var distance = Vector3.Distance(position, getThingPosition(thing));
            if (!(distance < closestThingDistance)) return;
            closestThingDistance = distance;
            closestThing = thing;
            found = true;
        });
        
        return (found, closestThing);
    }

    public static bool PathNotVisibleByPlayer(NavMeshPath path)
    {
        var corners = path.corners;
        for (var i = 1; i < corners.Length; i++)
            if (Physics.Linecast(corners[i - 1], corners[i], 262144))
                return false;

        return true;
    }
}