using GameNetcodeStuff;
using PossessedMasks.networking;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace PossessedMasks.mono;

public class CrawlingComponent : MonoBehaviour
{
    private const float TimeBetweenRetargets = 2f;
    
    private HauntedMaskItem _mask;
    public bool inside;
    private NavMeshAgent _agent;
    private float _timeUntilRetarget = TimeBetweenRetargets;
    private PlayerControllerB _targetPlayer = null;

    private void Start()
    {
        _mask = gameObject.GetComponent<HauntedMaskItem>();
        _agent = gameObject.AddComponent<NavMeshAgent>();
        _agent.speed = 1f;
    }

    private void Update()
    {
        SyncLocationToClients();
        if (!_mask || _mask.playerHeldBy || !_mask.previousPlayerHeldBy) return;
        _timeUntilRetarget -= Time.deltaTime;

        if (_timeUntilRetarget <= 0f) Retarget();
        if (_targetPlayer) SetTargetDestination();
    }

    private void SetTargetDestination()
    {
        // move towards target
        _agent.SetDestination(_targetPlayer.transform.position);
    }

    private void Retarget()
    {
        // check if we have a valid target player, if not, find one
        if (_targetPlayer == null || _targetPlayer.isInsideFactory != inside)
            _targetPlayer = Utils.FindFarthestAwayPlayer(transform.position, inside);
        
        _timeUntilRetarget = TimeBetweenRetargets;
    }

    private void SyncLocationToClients()
    {
        CrawlingBehaviour.Instance.SyncLocationServerRpc(_mask.NetworkObject, transform.position);
    }
}