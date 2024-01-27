using HarmonyLib;
using PossessedMasks.mono;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
    [HarmonyPatch(nameof(StartOfRound.Awake)), HarmonyPostfix]
    private static void AwakePostfix()
    {
        // host only instructions
        if (Utils.HostCheck)
        {
            // spawn network prefab
            Plugin.Log.LogMessage("Spawning network prefab (host only)");
            var networkHandlerHost = Object.Instantiate(Plugin.NetworkPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);
        }
    }
    
    [HarmonyPatch(nameof(StartOfRound.Start)), HarmonyPostfix]
    private static void StartPostfix(StartOfRound __instance)
    {
        // client only instructions
        if (Utils.HostCheck)
        {
            // start the manager
            Plugin.Log.LogMessage("Starting manager (host only)");
            __instance.gameObject.AddComponent<ServerManager>();
            
            // register all if not already done
            Utils.RegisterAll();
        }
    }
}