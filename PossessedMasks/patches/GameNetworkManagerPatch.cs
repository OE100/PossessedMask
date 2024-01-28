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
            Plugin.Log.LogMessage("Discard override complete");
        }
        catch (Exception)
        {
            Plugin.Log.LogMessage("Didn't override discard");
        }
        
        try
        {
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").performed -=
                StartOfRound.Instance.localPlayerController.Interact_performed;
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").performed +=
                PossessedBehaviour.Instance.Interact_with_check_performed;
            Plugin.Log.LogMessage("Interact override complete");
        } catch (Exception)
        {
            Plugin.Log.LogMessage("Didn't override interact");
        }
    }
}