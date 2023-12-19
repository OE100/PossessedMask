using BepInEx;
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

        private void Awake()
        {
            Log = this.Logger;
            Log.LogInfo($"'{NAME}' is loading...");

            if (Instance == null)
                Instance = this;

            harmony.PatchAll();

            Log.LogInfo($"'{NAME}' loaded!");
        }
    }
}