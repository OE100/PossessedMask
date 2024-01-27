using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PossessedMasks;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony harmony = new(Guid);

    public const string Guid = "oe.tweaks.qol.possessedmasks";
    internal const string Name = "Possessed Masks";
    public const string Version = "2.0.2";

    internal static Plugin Instance;

    internal static GameObject NetworkPrefab;

    internal static ManualLogSource Log;

    private AssetBundle _ab;
    
    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"'{Name}' is loading...");

        if (Instance == null)
            Instance = this;
        
        ModConfig.Init(Config);
        
        _ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(Assembly.GetExecutingAssembly().GetManifestResourceNames()[0]));

        if (_ab == null)
        {
            Log.LogError("Failed to load asset bundle");
            return;
        }
        
        Log.LogInfo("Asset bundle loaded");
        Utils.PossessionSounds.Add(_ab.LoadAsset<AudioClip>("possession1"));
        Utils.PossessionSounds.Add(_ab.LoadAsset<AudioClip>("possession2"));
        Utils.PossessionSounds.Add(_ab.LoadAsset<AudioClip>("possession3"));
        Utils.SlotSwitchSounds.Add(_ab.LoadAsset<AudioClip>("slot1"));
        Utils.SlotSwitchSounds.Add(_ab.LoadAsset<AudioClip>("slot2"));

        NetworkPrefab = _ab.LoadAsset<GameObject>("PMNetPrefab.prefab");
        
        InitializeNetworkRoutine();
        
        harmony.PatchAll();
        
        Log.LogInfo($"'{Name}' loaded!");
    }

    private void OnDestroy()
    {
        Instance = null;
    }


    private void InitializeNetworkRoutine()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }
}