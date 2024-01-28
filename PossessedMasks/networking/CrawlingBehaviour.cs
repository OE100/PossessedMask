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
    public void SyncLocationServerRpc(NetworkObjectReference maskRef, Vector3 position)
    {
        SyncLocationClientRpc(maskRef, position);
    }

    [ClientRpc]
    private void SyncLocationClientRpc(NetworkObjectReference maskRef, Vector3 position)
    {
        if (!maskRef.TryGet(out var networkObject)) return;
        networkObject.gameObject.transform.position = position;
    }
}