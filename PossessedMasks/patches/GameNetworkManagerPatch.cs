using System.Collections;
using HarmonyLib;
using PossessedMasks.networking;
using Unity.Netcode;
using UnityEngine;

namespace PossessedMasks.patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public class GameNetworkManagerPatch
{
    [HarmonyPatch(nameof(GameNetworkManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void StartPostfix(GameNetworkManager __instance)
    {
        // register network prefab
        Plugin.NetworkPrefabs.ForEach(prefab => NetworkManager.Singleton.AddNetworkPrefab(prefab));
    }
}