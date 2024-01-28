using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

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
}