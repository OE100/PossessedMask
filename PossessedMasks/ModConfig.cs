using BepInEx.Configuration;

namespace PossessedMasks;

public class ModConfig
{
    internal static bool Loaded { get; private set; }
    
    internal static ConfigEntry<bool> EnableMaskPossessionMechanic; // if true, mask possession mechanic will be enabled
    internal static ConfigEntry<bool> EnableMaskSwitchSlotMechanic; // if true, mask switching to your active slot mechanic will be enabled
    internal static ConfigEntry<int> EnableChangeMaskSpawnChance; // if 0, will not override mask spawn chance, scrap value and available levels. if 1, will override spawn chance, scrap value but not available levels, if 2 will override all of them. 
    
    internal static ConfigEntry<int> NumberOfSlotsFilledToEnableDroppingMask; // number of slots filled to enable dropping mask (-1 to disable) 
    
    internal static ConfigEntry<float> TimeToStartSwitchingSlots; // after this much time with mask in inventory start switching slots
    
    internal static ConfigEntry<float> TimeToStartPossession; // after this much time with mask in inventory start possessions

    internal static ConfigEntry<bool> TwoHandedItemBehaviour; // if true, 2 handed items will be dropped when switching slots
    
    internal static ConfigEntry<int> MaskRarity; // rarity of mask item (0 - 100, 0 - doesn't spawn, 100 - spawns a lot)
    internal static ConfigEntry<bool> MaskRarityScaling; // if true, mask rarity will be scaled up by moon rank
    internal static ConfigEntry<float> MaskRarityScalingMultiplier; // multiplier for mask rarity scaling
    
    internal static ConfigEntry<int> MinMaskItemBaseValue; // minimum base value of mask item (affected by multipliers >1)
    internal static ConfigEntry<int> MaxMaskItemBaseValue; // maximum base value of mask item (affected by multipliers >1)
    
    internal static ConfigEntry<float> MinTimeToSwitchSlots; // minimum time between switching slots
    internal static ConfigEntry<float> MaxTimeToSwitchSlots; // maximum time between switching slots
    internal static ConfigEntry<float> DeltaTimeToSwitchSlots; // time to subtract from min and max each time a switch happens
    internal static ConfigEntry<float> MinSwitchingSlotTime; // the number at which subtracting from min and max stops
    
    internal static ConfigEntry<float> MinTimeToPossess; // minimum time between possessions
    internal static ConfigEntry<float> MaxTimeToPossess; // maximum time between possessions
    internal static ConfigEntry<float> DeltaTimeToPossess; // time to subtract from min and max each time a possession happens
    internal static ConfigEntry<float> MinPossessingTime; // the number at which subtracting from min and max stops
    
    internal static ConfigEntry<float> MinTimeToPossessPlayer; // minimum time of actual possession
    internal static ConfigEntry<float> MaxTimeToPossessPlayer; // maximum time of actual possession
    internal static ConfigEntry<float> DeltaTimeToPossessPlayer; // time to add to min and max each time a possession happens
    internal static ConfigEntry<float> MaxPossessingPlayerTime; // the number at which adding to min and max stops
    
    internal static void Init(ConfigFile config)
    {
        EnableMaskPossessionMechanic = config.Bind("Mechanics", "EnableMaskPossessionMechanic", true, "If true, mask possession mechanic will be enabled");
        EnableMaskSwitchSlotMechanic = config.Bind("Mechanics", "EnableMaskSwitchSlotMechanic", true, "If true, mask switching to your active slot mechanic will be enabled");
        EnableChangeMaskSpawnChance = config.Bind("Mechanics", "EnableChangeMaskSpawnChance", 2, "If 0, will not override mask spawn chance, scrap value and available levels.\nIf 1, will override spawn chance, scrap value but not available levels.\nif 2 will override all of them.");
        
        NumberOfSlotsFilledToEnableDroppingMask = config.Bind("General", "NumberOfSlotsFilledToEnableDroppingMask", 3, "Number of inventory slots that need to be filled to enable dropping a mask");
        
        TimeToStartSwitchingSlots = config.Bind("General", "TimeToStartSwitchingSlots", 5f, "After this much time with mask in inventory start switching slots");
        
        TimeToStartPossession = config.Bind("General", "TimeToStartPossession", 10f, "After this much time with mask in inventory start possessions");
        
        TwoHandedItemBehaviour = config.Bind("General", "TwoHandedItemBehaviour", true, "If true, 2 handed items will be dropped when switching slots");
        
        MaskRarity = config.Bind("Mask Rarity", "MaskRarity", 35, "Rarity of mask item 0 - 100 (0 - doesn't spawn, 100 - spawns a lot)");
        MaskRarityScaling = config.Bind("Mask Rarity", "MaskRarityScaling", true, "If true, mask rarity will be scaled up by moon rank");
        MaskRarityScalingMultiplier = config.Bind("Mask Rarity", "MaskRarityScalingMultiplier", 0.5f, "Multiplier for mask rarity scaling");
        
        MinMaskItemBaseValue = config.Bind("Mask Item Base Value", "MinMaskItemBaseValue", 100, "Minimum base value of mask item (affected by multipliers >1)");
        MaxMaskItemBaseValue = config.Bind("Mask Item Base Value", "MaxMaskItemBaseValue", 150, "Maximum base value of mask item (affected by multipliers >1)");
        
        MinTimeToSwitchSlots = config.Bind("Time To Switch Slots", "MinTimeToSwitchSlots", 12f, "Minimum time between switching slots");
        MaxTimeToSwitchSlots = config.Bind("Time To Switch Slots", "MaxTimeToSwitchSlots", 20f, "Maximum time between switching slots");
        DeltaTimeToSwitchSlots = config.Bind("Time To Switch Slots", "DeltaTimeToSwitchSlots", 0.5f, "Time to subtract from min and max each time a switch happens");
        MinSwitchingSlotTime = config.Bind("Time To Switch Slots", "MinSwitchingSlotTime", 3f, "The number at which subtracting from min and max stops");
        
        MinTimeToPossess = config.Bind("Time To Possess", "MinTimeToPossess", 20f, "Minimum time between possessions");
        MaxTimeToPossess = config.Bind("Time To Possess", "MaxTimeToPossess", 30f, "Maximum time between possessions");
        DeltaTimeToPossess = config.Bind("Time To Possess", "DeltaTimeToPossess", 1f, "Time to subtract from min and max each time a possession happens");
        MinPossessingTime = config.Bind("Time To Possess", "MinPossessingTime", 10f, "The number at which subtracting from min and max stops");
        
        MinTimeToPossessPlayer = config.Bind("Item Activation Time", "MinTimeToPossessPlayer", 2f, "Minimum time of actual possession");
        MaxTimeToPossessPlayer = config.Bind("Item Activation Time", "MaxTimeToPossessPlayer", 4f, "Maximum time of actual possession");
        DeltaTimeToPossessPlayer = config.Bind("Item Activation Time", "DeltaTimeToPossessPlayer", 1f, "Time to add to min and max each time a possession happens");
        MaxPossessingPlayerTime = config.Bind("Item Activation Time", "MaxPossessingPlayerTime", 9f, "The number at which adding to min and max stops");
        
        Loaded = true;
    }
}