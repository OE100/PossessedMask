using System.Collections;
using GameNetcodeStuff;
using PossessedMasks.networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace PossessedMasks.mono;

public class CrawlingComponent : MonoBehaviour
{
    private enum AIState
    {
        Initial,
        ChooseTarget,
        Flank,
        Lurk,
        Primed
    }
    
    private const float TimeBetweenRetargets = 2f;
    
    private Quaternion _originalRotation;
    private HauntedMaskItem _mask;
    private NavMeshAgent _demoAgent;
    private NavMeshAgent _agent;
    private NavMeshPath _path = null;
    public bool? Inside = null;
    private float _timeUntilRetarget = TimeBetweenRetargets;
    private bool _warped = false;

    // ai components
    private AIState _prevAIState;
    private AIState _aiState;
    private NavMeshHit _hit;
    private float _timeAroundPlayer = 0f;
    private PlayerControllerB _targetPlayer = null;
    private GameObject _targetAINode = null;
    private bool _targetedAINode = false;
    private bool _primed;


    private void Start()
    {
        GameObject obj;
        _mask = (obj = gameObject).GetComponent<HauntedMaskItem>();
        _originalRotation = obj.transform.rotation;
        _demoAgent = Utils.EnemyPrefabRegistry[typeof(CentipedeAI)].GetComponent<NavMeshAgent>();
        StartCoroutine(DelayedSetupAgent());
    }

    private IEnumerator DelayedSetupAgent()
    {
        yield return new WaitUntil(() => _mask.hasHitGround);
        while (true)
        {
            yield return new WaitUntil(() => !_mask.playerHeldBy && !_agent);
            _agent = gameObject.AddComponent<NavMeshAgent>();
            yield return new WaitUntil(() => !_warped || !_agent.isOnNavMesh);
            _targetPlayer = null;
            NavMesh.SamplePosition(_mask.transform.position, out var hit, float.MaxValue, NavMesh.AllAreas);
            CrawlingBehaviour.Instance.SyncLocationServerRpc(_mask.NetworkObject, hit.position, _mask.transform.rotation);
            _agent.Warp(hit.position);
            _warped = true;
            SetNavMeshAgentDefault();
        }
    }
    
    private void SetNavMeshAgentDefault()
    {
        _agent.agentTypeID = _demoAgent.agentTypeID;
        _agent.baseOffset = _agent.baseOffset;
        _agent.speed = _demoAgent.speed / 2;
        _agent.acceleration = _demoAgent.acceleration / 2;
        _agent.angularSpeed = _demoAgent.angularSpeed / 2;
        _agent.stoppingDistance = _demoAgent.stoppingDistance / 2;
        _agent.autoBraking = _demoAgent.autoBraking;
        _agent.radius = _demoAgent.radius / 2;
        _agent.height = _demoAgent.height / 2;
        _agent.obstacleAvoidanceType = _demoAgent.obstacleAvoidanceType;
        _agent.avoidancePriority = _demoAgent.avoidancePriority;
        _agent.autoTraverseOffMeshLink = _demoAgent.autoTraverseOffMeshLink;
        _agent.autoRepath = _demoAgent.autoRepath;
        _agent.areaMask = _demoAgent.areaMask;
        _agent.enabled = true;
    }

    private void Update()
    {
        if (_primed) return;
        
        if (Inside == null || _mask.playerHeldBy || !_agent) return;

        if (_mask.enabled)
            CrawlingBehaviour.Instance.SetObjStateServerRpc(_mask.NetworkObject, false);

        SyncLocationToClients();

        if (!_mask || !_mask.previousPlayerHeldBy) return;

        if (_aiState is AIState.Flank or AIState.Lurk)
            _timeUntilRetarget -= Time.deltaTime;
        
        if (_timeUntilRetarget <= 0f)
        {
            _targetPlayer = null;
            SetState(AIState.ChooseTarget);
        }
        
        DoInterval();
    }

    private void DoInterval()
    {
        NavMeshHit hit;
        if (_aiState == AIState.Initial)
        {
            _agent.speed = _demoAgent.speed * 1.5f;
            if (!_targetAINode)
                FindFarthestAINode();
            else if (!_targetedAINode)
            {
                NavMesh.SamplePosition(_targetAINode.transform.position, out hit, float.MaxValue,
                    NavMesh.AllAreas);
                _agent.SetDestination(hit.position);
                _targetedAINode = true;
            }

            if (Vector3.Distance(transform.position, _targetAINode.transform.position) < 10f)
            {
                _targetAINode = null;
                _targetedAINode = false;
                SetState(AIState.ChooseTarget);
            }
        }
        else if (_aiState == AIState.ChooseTarget)
        {
            if (!_targetPlayer)
                FindClosestPlayer();
            else
            {
                CrawlingBehaviour.Instance.SetEyesFilledServerRpc(_mask.NetworkObject, true);
                SetState(AIState.Flank);
            }
        }
        else if (_aiState == AIState.Flank)
        {
            CrawlingBehaviour.Instance.SetEyesFilledServerRpc(_mask.NetworkObject, true);

            if (_path == null ||
                _path.status == NavMeshPathStatus.PathInvalid ||
                _path.status == NavMeshPathStatus.PathComplete)
                CalculateFlankPath();

            var distance = Vector3.Distance(transform.position, _targetPlayer.transform.position);
            if (distance < 40)
                SetState(AIState.Lurk);
            else if (_targetPlayer.HasLineOfSightToPosition(transform.position))
            {
                _agent.enabled = false;
                _mask.transform.rotation = _originalRotation;
            }
            else
                _agent.enabled = true;
        }
        else if (_aiState == AIState.Lurk)
        {
            if (_timeAroundPlayer > 10f)
            {
                SetState(AIState.Primed);
                return;
            }

            var distance = Vector3.Distance(transform.position, _targetPlayer.transform.position);

            if (distance < 10) _timeAroundPlayer += Time.deltaTime;
            else if (distance > 40) SetState(AIState.Flank);

            if (_prevAIState != _aiState) _agent.speed = _demoAgent.speed;

            var tt = _targetPlayer.transform;
            if (NavMesh.SamplePosition(tt.position - tt.forward * 2 - tt.up * 2, out hit, 5f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }
        else if (_aiState == AIState.Primed)
        {
            ReturnToItem();
            StartCoroutine(PrimeAndWait());
        }
        else
        {
            ReturnToItem();
            DestroyImmediate(this);
        }
    }

    private void CalculateFlankPath()
    {
        if (!NavMesh.SamplePosition(transform.position - transform.forward * 2 - transform.up * 2, out var hit, 5f, NavMesh.AllAreas)) return;

        _path = new NavMeshPath();
        _agent.CalculatePath(hit.position, _path);
        _agent.SetPath(_path);
    }

    private void FindFarthestAINode()
    {
        var aiNodes = Inside!.Value ? Utils.InsideAINodes : Utils.OutsideAINodes;
        var (found, aiNode) = Utils.FindFarthestAwayThingFromPosition(transform.position, 
            aiNodes.ToList(), node => node.transform.position);
        if (found) _targetAINode = aiNode;
    }
    
    private void FindClosestPlayer()
    {
        // todo: change and remove conditional
        // check if we have a valid target player, if not, find one
        if (_targetPlayer == null || _targetPlayer.isInsideFactory != Inside)
        {
            var (found, player) = Utils.FindClosestThingToPosition(transform.position,
                Utils.GetActivePlayers(Inside!.Value), player => player.transform.position);
            if (found)
                _targetPlayer = player;
        }
        
        _timeUntilRetarget = TimeBetweenRetargets;
    }

    private void SetState(AIState newState)
    {
        _prevAIState = _aiState;
        _aiState = newState;
    }
    
    private void SyncLocationToClients()
    {
        CrawlingBehaviour.Instance.SyncLocationServerRpc(_mask.NetworkObject, transform.position, transform.rotation);
    }

    private void OnDestroy()
    {
        ReturnToItem();
    }

    private void ReturnToItem(bool ray = false)
    {
        CrawlingBehaviour.Instance.SetEyesFilledServerRpc(_mask.NetworkObject, false);
        _timeAroundPlayer = 0;
        DestroyImmediate(_agent);
        _agent = null;
        _warped = false;
        gameObject.transform.rotation = _originalRotation;
        if (ray)
        {
            if (Physics.Raycast(transform.position + transform.up, transform.up * -1, out var rayHit))
                gameObject.transform.position = rayHit.transform.position;
            else
                gameObject.transform.position += Vector3.up * 0.5f;
        }
        CrawlingBehaviour.Instance.StartCoroutine(DelayedSetState(gameObject.GetComponent<NetworkObject>(), true));
    }

    private static IEnumerator DelayedSetState(NetworkObject obj, bool state)
    {
        yield return new WaitForEndOfFrame();
        CrawlingBehaviour.Instance.SetObjStateServerRpc(obj, state);
    }

    private IEnumerator PrimeAndWait()
    {
        _primed = true;
        yield return new WaitUntil(() => _mask.playerHeldBy);
        var waitForEndOfFrame = new WaitForEndOfFrame();
        yield return waitForEndOfFrame;
        yield return waitForEndOfFrame;
        _mask.AttachServerRpc();
    }
}