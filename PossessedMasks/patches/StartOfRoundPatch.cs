using System.Collections;
using HarmonyLib;
using PossessedMasks.mono;
using PossessedMasks.networking;
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
            
            // set network variables
            __instance.StartCoroutine(DelayedSetNetworkVariables());
        }
    }

    private static IEnumerator DelayedSetNetworkVariables()
    {
        yield return new WaitUntil(() => ModConfig.Loaded);
        yield return new WaitUntil(() => PossessedBehaviour.Instance != null);
        PossessedBehaviour.Instance.SetNetworkVariablesServerRpc(
            ModConfig.NumberOfSlotsFilledToEnableDroppingMask.Value,
            ModConfig.TwoHandedItemBehaviour.Value);
    }
}