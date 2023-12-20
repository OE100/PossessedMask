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
        
        internal static ConfigEntry<float> timeToStartSwitchingSlots; // after this much time with mask in inventory start switching slots
        
        internal static ConfigEntry<float> timeToStartPossession; // after this much time with mask in inventory start possessions

        internal static ConfigEntry<bool> twoHandedItemBehaviour; // if true, 2 handed items will be dropped when switching slots
        
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
            
            timeToStartSwitchingSlots = Config.Bind("General", "TimeToStartSwitchingSlots", 5f, "After this much time with mask in inventory start switching slots");
            
            timeToStartPossession = Config.Bind("General", "TimeToStartPossession", 10f, "After this much time with mask in inventory start possessions");
            
            twoHandedItemBehaviour = Config.Bind("General", "TwoHandedItemBehaviour", true, "If true, 2 handed items will be dropped when switching slots");
            
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