using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace PossessedMasks.networking;

public class PossessedBehaviour : NetworkBehaviour
{
    public static PossessedBehaviour Instance { get; private set; }
    
    public readonly NetworkVariable<int> ItemCount = new();
    public readonly NetworkVariable<bool> TwoHandedBehaviour = new();

    private static bool IsMyPlayer(ulong ownerId)
    {
        Plugin.Log.LogDebug($"Local Owner ID: {StartOfRound.Instance.localPlayerController.OwnerClientId}, Owner ID: {ownerId}");
        return StartOfRound.Instance.localPlayerController &&
            StartOfRound.Instance.localPlayerController.OwnerClientId == ownerId;
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instance = null;
    }

    [ServerRpc]
    public void SetNetworkVariablesServerRpc(int itemCount, bool twoHandedBehaviour)
    {
        ItemCount.Value = itemCount;
        TwoHandedBehaviour.Value = twoHandedBehaviour;
    }
    
    [ServerRpc]
    public void StartPossessionServerRpc(ulong ownerId)
    {
        StartPossessionClientRpc(ownerId);
    }
    
    [ClientRpc]
    private void StartPossessionClientRpc(ulong ownerId)
    {
        if (IsMyPlayer(ownerId))
        {
            if (!StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer ||
                StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer is not HauntedMaskItem mask) 
                return;

            if (Random.Range(0f, 1f) >= 0.33)
                Utils.PlayRandomAudioClipFromList(mask.maskAudio, Utils.PossessionSounds);
            mask.ActivateItemServerRpc(true, true);
            mask.ItemActivate(true, true);
        }
        else
            Plugin.Log.LogDebug("StartPossessionClientRPC: not owner");
    }

    [ServerRpc]
    public void StopPossessionServerRpc(ulong ownerId)
    {
        StopPossessionClientRpc(ownerId);
    }
    
    [ClientRpc]
    private void StopPossessionClientRpc(ulong ownerId)
    {
        if (IsMyPlayer(ownerId))
        {
            if (!StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer ||
                StartOfRound.Instance.localPlayerController.currentlyHeldObjectServer is not HauntedMaskItem mask) 
                return;
        
            mask.ActivateItemServerRpc(false, false);
            mask.ItemActivate(false, false);
        }
        else 
            Plugin.Log.LogDebug("DeactivateMaskClientRPC: not owner");
    }

    [ServerRpc]
    public void SwitchSlotServerRpc(ulong ownerId, bool forward, int maskSlot)
    {
        SwitchSlotClientRpc(ownerId, forward, maskSlot);
    }
    
    [ClientRpc]
    private void SwitchSlotClientRpc(ulong ownerId, bool forward, int maskSlot)
    {
        if (IsMyPlayer(ownerId))
            StartCoroutine(DelayedSlotSwitch(forward, maskSlot));
        else
            Plugin.Log.LogDebug("SwitchSlotClientRPC: not owner");
    }

    private IEnumerator DelayedSlotSwitch(bool forward, int maskSlot)
    {
        var localPlayer = StartOfRound.Instance.localPlayerController;
        var potentialMask = localPlayer.ItemSlots[maskSlot];
            
        // play sound from the mask
        if (Random.Range(0f, 1f) >= 0.33)
            if (potentialMask && potentialMask is HauntedMaskItem mask)
                Utils.PlayRandomAudioClipFromList(mask.maskAudio, Utils.SlotSwitchSounds);

        // disable some actions until done
        localPlayer.playerActions.FindAction("ActivateItem").Disable();
        localPlayer.playerActions.FindAction("SwitchItem").Disable();
        
        // disable held item if it's being used
        var currentHeld = localPlayer.ItemSlots[localPlayer.currentItemSlot];
        if (currentHeld && (currentHeld.isBeingUsed || localPlayer.activatingItem))
        {
            currentHeld.UseItemOnClient(false);
            yield return new WaitForEndOfFrame();
        }
        
        // drop 2 handed item if it's being held and 2 handed item behaviour is enabled
        if (TwoHandedBehaviour.Value && localPlayer.twoHanded)
        {
            var heldObject = localPlayer.currentlyHeldObject;
            yield return StartCoroutine(localPlayer.waitToEndOfFrameToDiscard());
            yield return new WaitForEndOfFrame();
            heldObject.EnableItemMeshes(true);
        }
            
        // switch toward mask
        localPlayer.SwitchToItemSlot(localPlayer.NextItemSlot(forward));
        localPlayer.SwitchItemSlotsServerRpc(forward);
        
        // enable some actions
        localPlayer.playerActions.FindAction("ActivateItem").Enable();
        localPlayer.playerActions.FindAction("SwitchItem").Enable();
    } 

    internal void Discard_with_check_performed(InputAction.CallbackContext context)
    {
        var localPlayer = StartOfRound.Instance.localPlayerController;
        var heldObject = localPlayer.currentlyHeldObject;
        if (Utils.InLevel && heldObject == null && heldObject is HauntedMaskItem &&
            Utils.ItemCount(localPlayer) < ItemCount.Value) return;
        localPlayer.Discard_performed(context);
    }

    internal void Interact_with_check_performed(InputAction.CallbackContext context)
    {
        var localPlayer = StartOfRound.Instance.localPlayerController;
        var heldObject = localPlayer.currentlyHeldObject;
        if (Utils.InLevel && heldObject && heldObject is HauntedMaskItem &&
            (localPlayer.activatingItem || Utils.ItemCount(localPlayer) < ItemCount.Value)) return;
        localPlayer.Interact_performed(context);
    }
}