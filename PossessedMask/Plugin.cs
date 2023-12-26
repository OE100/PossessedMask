using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace PossessedMask
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(GUID);

        private const string GUID = "oe.tweaks.qol.possessedmask";
        private const string NAME = "Possessed Mask";
        private const string VERSION = "1.0.0";

        internal static Plugin Instance;

        internal static BepInEx.Logging.ManualLogSource Log;

        // config
        
        internal static ConfigEntry<bool> enableMaskPossessionMechanic; // if true, mask possession mechanic will be enabled
        internal static ConfigEntry<bool> enableMaskSwitchSlotMechanic; // if true, mask switching to your active slot mechanic will be enabled
        internal static ConfigEntry<int> enableChangeMaskSpawnChance; // if 0, will not override mask spawn chance, scrap value and available levels. if 1, will override spawn chance, scrap value but not available levels, if 2 will override all of them. 
        
        internal static ConfigEntry<int> numberOfSlotsFilledToEnableDroppingMask; // number of slots filled to enable dropping mask (-1 to disable) 
        
        internal static ConfigEntry<float> timeToStartSwitchingSlots; // after this much time with mask in inventory start switching slots
        
        internal static ConfigEntry<float> timeToStartPossession; // after this much time with mask in inventory start possessions

        internal static ConfigEntry<bool> twoHandedItemBehaviour; // if true, 2 handed items will be dropped when switching slots
        
        internal static ConfigEntry<int> maskRarity; // rarity of mask item (0 - 100, 0 - doesn't spawn, 100 - spawns a lot)
        internal static ConfigEntry<bool> maskRarityScaling; // if true, mask rarity will be scaled up by moon rank
        internal static ConfigEntry<float> maskRarityScalingMultiplier; // multiplier for mask rarity scaling
        
        internal static ConfigEntry<int> minMaskItemBaseValue; // minimum base value of mask item (affected by multipliers >1)
        internal static ConfigEntry<int> maxMaskItemBaseValue; // maximum base value of mask item (affected by multipliers >1)
        
        internal static ConfigEntry<float> minTimeToSwitchSlots; // minimum time between switching slots
        internal static ConfigEntry<float> maxTimeToSwitchSlots; // maximum time between switching slots
        internal static ConfigEntry<float> deltaTimeToSwitchSlots; // time to subtract from min and max each time a switch happens
        internal static ConfigEntry<float> minSwitchingSlotTime; // the number at which subtracting from min and max stops
        
        internal static ConfigEntry<float> minTimeToPossess; // minimum time between possessions
        internal static ConfigEntry<float> maxTimeToPossess; // maximum time between possessions
        internal static ConfigEntry<float> deltaTimeToPossess; // time to subtract from min and max each time a possession happens
        internal static ConfigEntry<float> minPossessingTime; // the number at which subtracting from min and max stops
        
        internal static ConfigEntry<float> minTimeToPossessPlayer; // minimum time of actual possession
        internal static ConfigEntry<float> maxTimeToPossessPlayer; // maximum time of actual possession
        internal static ConfigEntry<float> deltaTimeToPossessPlayer; // time to add to min and max each time a possession happens
        internal static ConfigEntry<float> maxPossessingPlayerTime; // the number at which adding to min and max stops
        
        private void Awake()
        {
            Log = this.Logger;
            Log.LogInfo($"'{NAME}' is loading...");

            if (Instance == null)
                Instance = this;

            // config
            
            enableMaskPossessionMechanic = Config.Bind("Mechanics", "EnableMaskPossessionMechanic", true, "If true, mask possession mechanic will be enabled");
            enableMaskSwitchSlotMechanic = Config.Bind("Mechanics", "EnableMaskSwitchSlotMechanic", true, "If true, mask switching to your active slot mechanic will be enabled");
            enableChangeMaskSpawnChance = Config.Bind("Mechanics", "EnableChangeMaskSpawnChance", 2, "If 0, will not override mask spawn chance, scrap value and available levels.\nIf 1, will override spawn chance, scrap value but not available levels.\nif 2 will override all of them.");
            
            numberOfSlotsFilledToEnableDroppingMask = Config.Bind("General", "NumberOfSlotsFilledToEnableDroppingMask", 3, "Number of inventory slots that need to be filled to enable dropping a mask");
            
            timeToStartSwitchingSlots = Config.Bind("General", "TimeToStartSwitchingSlots", 5f, "After this much time with mask in inventory start switching slots");
            
            timeToStartPossession = Config.Bind("General", "TimeToStartPossession", 10f, "After this much time with mask in inventory start possessions");
            
            twoHandedItemBehaviour = Config.Bind("General", "TwoHandedItemBehaviour", true, "If true, 2 handed items will be dropped when switching slots");
            
            maskRarity = Config.Bind("Mask Rarity", "MaskRarity", 35, "Rarity of mask item 0 - 100 (0 - doesn't spawn, 100 - spawns a lot)");
            maskRarityScaling = Config.Bind("Mask Rarity", "MaskRarityScaling", true, "If true, mask rarity will be scaled up by moon rank");
            maskRarityScalingMultiplier = Config.Bind("Mask Rarity", "MaskRarityScalingMultiplier", 0.5f, "Multiplier for mask rarity scaling");
            
            minMaskItemBaseValue = Config.Bind("Mask Item Base Value", "MinMaskItemBaseValue", 100, "Minimum base value of mask item (affected by multipliers >1)");
            maxMaskItemBaseValue = Config.Bind("Mask Item Base Value", "MaxMaskItemBaseValue", 150, "Maximum base value of mask item (affected by multipliers >1)");
            
            minTimeToSwitchSlots = Config.Bind("Time To Switch Slots", "MinTimeToSwitchSlots", 12f, "Minimum time between switching slots");
            maxTimeToSwitchSlots = Config.Bind("Time To Switch Slots", "MaxTimeToSwitchSlots", 20f, "Maximum time between switching slots");
            deltaTimeToSwitchSlots = Config.Bind("Time To Switch Slots", "DeltaTimeToSwitchSlots", 0.5f, "Time to subtract from min and max each time a switch happens");
            minSwitchingSlotTime = Config.Bind("Time To Switch Slots", "MinSwitchingSlotTime", 3f, "The number at which subtracting from min and max stops");
            
            minTimeToPossess = Config.Bind("Time To Possess", "MinTimeToPossess", 20f, "Minimum time between possessions");
            maxTimeToPossess = Config.Bind("Time To Possess", "MaxTimeToPossess", 30f, "Maximum time between possessions");
            deltaTimeToPossess = Config.Bind("Time To Possess", "DeltaTimeToPossess", 1f, "Time to subtract from min and max each time a possession happens");
            minPossessingTime = Config.Bind("Time To Possess", "MinPossessingTime", 10f, "The number at which subtracting from min and max stops");
            
            minTimeToPossessPlayer = Config.Bind("Item Activation Time", "MinTimeToPossessPlayer", 2f, "Minimum time of actual possession");
            maxTimeToPossessPlayer = Config.Bind("Item Activation Time", "MaxTimeToPossessPlayer", 4f, "Maximum time of actual possession");
            deltaTimeToPossessPlayer = Config.Bind("Item Activation Time", "DeltaTimeToPossessPlayer", 1f, "Time to add to min and max each time a possession happens");
            maxPossessingPlayerTime = Config.Bind("Item Activation Time", "MaxPossessingPlayerTime", 9f, "The number at which adding to min and max stops");
            
            
            
            harmony.PatchAll();

            Log.LogInfo($"'{NAME}' loaded!");
        }
    }
}