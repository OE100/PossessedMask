using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PossessedMasks.networking;

public class CrawlingBehaviour : NetworkBehaviour
{
    public static CrawlingBehaviour Instance { get; private set; }
    
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
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestConfigServerRpc()
    {
        if (!Utils.HostCheck) return;
        StartCoroutine(DelayedSendConfig());
    }
    
    private IEnumerator DelayedSendConfig()
    {
        yield return new WaitUntil(() => ModConfig.Loaded);
        SendConfigClientRpc(ModConfig.EnableMaskLurkingMechanic.Value);
    }
    
    [ClientRpc]
    private void SendConfigClientRpc(bool lurkingMechanicEnabled)
    {
        if (Utils.HostCheck) return;
        SharedConfig.LurkingMechanicEnabled = lurkingMechanicEnabled;
    }

    [ServerRpc]
    public void SyncLocationServerRpc(NetworkObjectReference maskRef, Vector3 position, Quaternion rotation)
    {
        SyncLocationClientRpc(maskRef, position, rotation);
    }

    [ClientRpc]
    private void SyncLocationClientRpc(NetworkObjectReference maskRef, Vector3 position, Quaternion rotation)
    {
        if (Utils.HostCheck) return;
        if (!maskRef.TryGet(out var networkObject)) return;
        var obj = networkObject.gameObject;
        obj.transform.position = position;
        obj.transform.rotation = rotation;
    }
    
    [ServerRpc]
    public void SetObjStateServerRpc(NetworkObjectReference objRef, bool state)
    {
        SetObjStateClientRpc(objRef, state);
    }

    [ClientRpc]
    private void SetObjStateClientRpc(NetworkObjectReference objRef, bool state)
    {
        if (!objRef.TryGet(out var networkObject)) return;
        var obj = networkObject.gameObject.GetComponent<GrabbableObject>();
        obj.enabled = state;
    }

    [ServerRpc]
    public void SetEyesFilledServerRpc(NetworkObjectReference maskRef, bool state)
    {
        SetEyesFilledClientRpc(maskRef, state);
    }

    [ClientRpc]
    private void SetEyesFilledClientRpc(NetworkObjectReference maskRef, bool state)
    {
        if (!maskRef.TryGet(out var networkObject)) return;
        var obj = networkObject.gameObject.GetComponent<GrabbableObject>();
        if (obj is HauntedMaskItem mask)
        {
            mask.maskEyesFilled.enabled = state;
        }
    }

    [ServerRpc]
    public void PossessUponPickupServerRpc(NetworkObjectReference maskRef)
    {
        PossessUponPickupClientRpc(maskRef);
    }

    [ClientRpc]
    private void PossessUponPickupClientRpc(NetworkObjectReference maskRef)
    {
        if (!maskRef.TryGet(out var networkObject)) return;
        var mask = networkObject.gameObject.GetComponent<HauntedMaskItem>();
        StartCoroutine(DelayedPossessUponPickup(mask));
    }

    private static IEnumerator DelayedPossessUponPickup(HauntedMaskItem mask)
    {
        yield return new WaitUntil(() => mask.playerHeldBy);
        yield return new WaitForEndOfFrame();
        mask.BeginAttachment();
    }
}