using HarmonyLib;
using PossessedMasksRewrite.mono;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PossessedMasksRewrite.patches;

[HarmonyPatch(typeof(StartOfRound))]
public class StartOfRoundPatch
{
    [HarmonyPatch(nameof(StartOfRound.Awake)), HarmonyPostfix]
    private static void AwakePostfix()
    {
        Plugin.Log.LogDebug("StartOfRound Awake");
        
        // host only instructions
        if (Utils.HostCheck)
        {
            // spawn network prefab
            var networkHandlerHost = Object.Instantiate(Plugin.NetworkPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);
            
            // register all
            Utils.RegisterAll();
        }
    }
    
    [HarmonyPatch(nameof(StartOfRound.Start)), HarmonyPostfix]
    private static void StartPostfix(StartOfRound __instance)
    {
        Plugin.Log.LogDebug("StartOfRound Start");
        
        // client only instructions
        if (Utils.HostCheck)
        {
            // start the manager
            __instance.gameObject.AddComponent<ServerManager>();
        }
    }
}