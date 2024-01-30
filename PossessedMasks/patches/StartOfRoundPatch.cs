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
            // spawn network prefabs
            Plugin.Log.LogMessage("Spawning network prefabs (host only)");
            Plugin.NetworkPrefabs.ForEach(prefab =>
            {
                Object.Instantiate(prefab, Vector3.zero, Quaternion.identity)
                    .GetComponent<NetworkObject>()
                    .Spawn(destroyWithScene: false);
            });
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
    
    [HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents)), HarmonyPostfix]
    private static void OnShipLandedMiscEventsPostfix()
    {
        // client only instructions
        if (Utils.HostCheck)
        {
            // find all ai nodes on the map
            Utils.InsideAINodes = GameObject.FindGameObjectsWithTag("AINode");
            Utils.OutsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
        }
    }
}