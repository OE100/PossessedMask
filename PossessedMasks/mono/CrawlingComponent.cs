using System.Collections;
using GameNetcodeStuff;
using PossessedMasks.networking;
using UnityEngine;
using UnityEngine.AI;

namespace PossessedMasks.mono;

public class CrawlingComponent : MonoBehaviour
{
    private const float TimeBetweenRetargets = 2f;
    
    private HauntedMaskItem _mask;
    public bool? Inside = null;
    private NavMeshAgent _agent;
    private float _timeUntilRetarget = TimeBetweenRetargets;
    private PlayerControllerB _targetPlayer = null;
    private bool _warped = false;

    private void Start()
    {
        _mask = gameObject.GetComponent<HauntedMaskItem>();
        StartCoroutine(DelayedSetupAgent());
        _agent = gameObject.AddComponent<NavMeshAgent>();
        SetNavMeshAgentDefault();
    }

    private IEnumerator DelayedSetupAgent()
    {
        yield return new WaitUntil(() => _mask.hasHitGround);
        SetNavMeshAgentDefault();
        while (true)
        {
            yield return new WaitUntil(() => !_mask.playerHeldBy && (!_warped || !_agent.isOnNavMesh));
            _targetPlayer = null;
            NavMesh.SamplePosition(_mask.transform.position, out var hit, float.MaxValue, NavMesh.AllAreas);
            CrawlingBehaviour.Instance.SyncLocationServerRpc(_mask.NetworkObject, hit.position, _mask.transform.rotation);
            _agent.Warp(hit.position);
            _warped = true;
        }
    }
    
    private void SetNavMeshAgentDefault()
    {
        var centipedeAgent = Utils.EnemyPrefabRegistry[typeof(CentipedeAI)].GetComponent<NavMeshAgent>();
        _agent.agentTypeID = centipedeAgent.agentTypeID;
        _agent.baseOffset = _agent.baseOffset;
        _agent.speed = centipedeAgent.speed / 2;
        _agent.acceleration = centipedeAgent.acceleration / 2;
        _agent.angularSpeed = centipedeAgent.angularSpeed / 2;
        _agent.stoppingDistance = centipedeAgent.stoppingDistance / 2;
        _agent.autoBraking = centipedeAgent.autoBraking;
        _agent.radius = centipedeAgent.radius / 2;
        _agent.height = centipedeAgent.height / 2;
        _agent.obstacleAvoidanceType = centipedeAgent.obstacleAvoidanceType;
        _agent.avoidancePriority = centipedeAgent.avoidancePriority;
        _agent.autoTraverseOffMeshLink = centipedeAgent.autoTraverseOffMeshLink;
        _agent.autoRepath = centipedeAgent.autoRepath;
        _agent.areaMask = centipedeAgent.areaMask;
    }

    private void Update()
    {
        if (Inside == null || !_warped) return;
        if (_mask.playerHeldBy)
        {
            CrawlingBehaviour.Instance.SetMaskStateServerRpc(_mask.NetworkObject, true);
            _agent.enabled = false;
            return;
        }
        
        if (_mask.enabled)
            CrawlingBehaviour.Instance.SetMaskStateServerRpc(_mask.NetworkObject, false);
        _agent.enabled = true;
        SyncLocationToClients();
        if (!_mask || !_mask.previousPlayerHeldBy) return;
        _timeUntilRetarget -= Time.deltaTime;

        if (_timeUntilRetarget <= 0f) Retarget();
        if (_targetPlayer) SetTargetDestination();
    }

    private void SetTargetDestination()
    {
        // move towards target
        _agent.destination = _targetPlayer.transform.position;
    }

    private void Retarget()
    {
        // check if we have a valid target player, if not, find one
        if (_targetPlayer == null || _targetPlayer.isInsideFactory != Inside)
            _targetPlayer = Utils.FindFarthestAwayPlayer(transform.position, Inside!.Value);
        
        _timeUntilRetarget = TimeBetweenRetargets;
    }

    private void SyncLocationToClients()
    {
        CrawlingBehaviour.Instance.SyncLocationServerRpc(_mask.NetworkObject, transform.position, transform.rotation);
    }
}