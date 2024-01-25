using System;
using System.Collections;
using HarmonyLib;
using PossessedMasksRewrite.networking;
using Unity.Netcode;
using UnityEngine;

namespace PossessedMasksRewrite.patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public class GameNetworkManagerPatch
{
    [HarmonyPatch(nameof(GameNetworkManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void StartPostfix(GameNetworkManager __instance)
    {
        Plugin.Log.LogDebug("GameNetworkManager Start");
        
        // register network prefab
        NetworkManager.Singleton.AddNetworkPrefab(Plugin.NetworkPrefab);
        
        // start coroutine to modify player
        __instance.StartCoroutine(DelayedModifyPlayer());
    }

    private static IEnumerator DelayedModifyPlayer()
    {
        yield return new WaitUntil(() => PossessedBehaviour.Instance);
        yield return new WaitUntil(() => StartOfRound.Instance);
        yield return new WaitUntil(() => IngamePlayerSettings.Instance);
        yield return new WaitUntil(() => StartOfRound.Instance.localPlayerController);
        yield return new WaitUntil(() => IngamePlayerSettings.Instance.playerInput);
        
        try
        {
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Discard").performed -=
                StartOfRound.Instance.localPlayerController.Discard_performed;
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Discard").performed +=
                PossessedBehaviour.Instance.Discard_with_check_performed;
            Plugin.Log.LogDebug("Discard override complete");
        }
        catch (Exception)
        {
            Plugin.Log.LogDebug("Didn't override discard");
        }
        
        try
        {
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").performed -=
                StartOfRound.Instance.localPlayerController.Interact_performed;
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").performed +=
                PossessedBehaviour.Instance.Interact_with_check_performed;
            Plugin.Log.LogDebug("Interact override complete");
        } catch (Exception)
        {
            Plugin.Log.LogDebug("Didn't override interact");
        }
    }
}