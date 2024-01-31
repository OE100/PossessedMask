using System.Collections;
using GameNetcodeStuff;
using PossessedMasks.machines.Def;
using PossessedMasks.networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace PossessedMasks.machines.impl.mask;

public class MaskStateManager : MonoBehaviour
{
    private class Data
    {
        // utilities
        public NavMeshHit Hit;
        
        // components
        public HauntedMaskItem Mask;
        public NavMeshAgent Agent;
        public NavMeshAgent DemoAgent;

        // variables
        public Quaternion OriginalRotation;

        public NavMeshPath Path; 
        
        public const float TimeBetweenRetargets = 1f;
        public float TimeUntilRetarget = TimeBetweenRetargets;
        
        public bool Inside;
        public bool Active;
        
        public GameObject AINode;
        
        public PlayerControllerB TargetPlayer;
        public float TimeAroundPlayer;

        public bool IsPrimed;
    }
    
    private enum State
    {
        Initial,
        ChooseNode,
        GoToNode,
        ChooseTarget,
        Flank,
        Lurk,
        Primed,
        Wait
    }

    private Data _data;
    private FiniteStateMachine<State, Data> _finiteStateMachine;
    private NetworkObject _networkObject;

    private void Awake()
    {
        _data = new Data();
        Initialize();
        _networkObject = gameObject.GetComponent<NetworkObject>();
        _finiteStateMachine = new FiniteStateMachine<State, Data>(_data);
        AssignFunctions();
    }

    private void Update()
    {
        CrawlingBehaviour.Instance.SyncLocationServerRpc(_networkObject, 
            _networkObject.transform.position, _networkObject.transform.rotation);
        _finiteStateMachine?.Tick();
    }
    
    private void Initialize()
    {
        // get components
        _data.OriginalRotation = gameObject.transform.rotation;
        
        if (!gameObject.TryGetComponent(out _data.Agent)) 
            (_data.Agent = gameObject.AddComponent<NavMeshAgent>()).enabled = false;
        
        if (!gameObject.TryGetComponent(out _data.Mask))
        {
            Plugin.Log.LogError("MaskStateManager: Mask component not found!");
            DestroyImmediate(this);
            return;
        }
        
        if (!Utils.EnemyPrefabRegistry[typeof(CentipedeAI)].TryGetComponent(out _data.DemoAgent))
        {
            Plugin.Log.LogError("MaskStateManager: DemoAgent not found!");
            DestroyImmediate(this);
            return;
        }
        
        SetNavMeshAgentDefault();
    }
    
    private void SetNavMeshAgentDefault()
    {
        _data.Agent.agentTypeID = _data.DemoAgent.agentTypeID;
        _data.Agent.baseOffset = _data.Agent.baseOffset;
        _data.Agent.acceleration = _data.DemoAgent.acceleration / 2;
        _data.Agent.angularSpeed = _data.DemoAgent.angularSpeed / 2;
        _data.Agent.stoppingDistance = _data.DemoAgent.stoppingDistance / 2;
        _data.Agent.autoBraking = _data.DemoAgent.autoBraking;
        _data.Agent.radius = _data.DemoAgent.radius / 2;
        _data.Agent.height = _data.DemoAgent.height / 2;
        _data.Agent.obstacleAvoidanceType = _data.DemoAgent.obstacleAvoidanceType;
        _data.Agent.avoidancePriority = _data.DemoAgent.avoidancePriority;
        _data.Agent.autoTraverseOffMeshLink = _data.DemoAgent.autoTraverseOffMeshLink;
        _data.Agent.autoRepath = _data.DemoAgent.autoRepath;
        _data.Agent.areaMask = _data.DemoAgent.areaMask;
    }
    
    private void AssignFunctions()
    {
        _finiteStateMachine.AddPreTickAction(PreTick);
        _finiteStateMachine.AddAction(State.Initial, Initial);
        _finiteStateMachine.AddAction(State.ChooseNode, ChooseNode);
        _finiteStateMachine.AddAction(State.GoToNode, GoToNode);
        _finiteStateMachine.AddAction(State.ChooseTarget, ChooseTarget);
        _finiteStateMachine.AddAction(State.Flank, Flank);
        _finiteStateMachine.AddAction(State.Lurk, Lurk);
        _finiteStateMachine.AddAction(State.Primed, Primed);
        _finiteStateMachine.AddAction(State.Wait, Wait);
    }
    
    // pre tick
    private void PreTick(State previousState, State currentState, Data data)
    {
        if (currentState == State.Wait || !data.Mask.playerHeldBy) return;
        
        data.Agent.enabled = false;
        _finiteStateMachine.SwitchStates(State.Wait);
    }
    
    // state functions
    private static State Initial(State previousState, Data data)
    {
        if (!data.Active) return State.Initial;
        if (!data.Mask.hasHitGround) return State.Initial;

        data.Active = false;
        CrawlingBehaviour.Instance.SetObjStateServerRpc(data.Mask.NetworkObject, false);
        data.Inside = data.Mask.previousPlayerHeldBy.isInsideFactory;
        Plugin.Log.LogDebug($"Found navmesh hit: {NavMesh.SamplePosition(data.Mask.transform.position, out data.Hit, float.MaxValue, NavMesh.AllAreas)}");
        CrawlingBehaviour.Instance.SyncLocationServerRpc(data.Mask.NetworkObject, data.Hit.position, data.Mask.transform.rotation);
        data.Agent.speed = data.DemoAgent.speed * 2.5f;
        data.Agent.enabled = true;
        data.Agent.Warp(data.Hit.position);
        data.TargetPlayer = null;
        data.AINode = null;
        
        return State.ChooseNode;
    }
    
    private static State ChooseNode(State previousState, Data data)
    {
        data.Agent.speed = data.DemoAgent.speed * 2.5f;
        var position = data.Mask.transform.position;
        
        var sorted = (data.Inside ? Utils.InsideAINodes : Utils.OutsideAINodes)
            .OrderByDescending(node => Vector3.Distance(position, node.transform.position)).ToList();

        data.Path = new NavMeshPath();
        var found = false;
        for (var i = 0; !found && i < sorted.Count; i++)
        {
            if (!NavMesh.SamplePosition(sorted[i].transform.position, out data.Hit, 20f, NavMesh.AllAreas)) continue;
            if (!data.Agent.CalculatePath(data.Hit.position, data.Path)) continue;
            if (!Utils.PathNotVisibleByPlayer(data.Path)) continue;
            data.AINode = sorted[i];
            found = true;
        }

        return found ? State.GoToNode : State.ChooseNode;
    }

    private static State GoToNode(State previousState, Data data)
    {
        CrawlingBehaviour.Instance.SetEyesFilledServerRpc(data.Mask.NetworkObject, false);
        if (previousState != State.GoToNode)
            data.Agent.SetPath(data.Path);
        
        // try to see if there's a door lock in front of us
        var at = data.Agent.transform;
        if (Physics.Raycast(at.position, at.forward, out var hit, 5f, Physics.DefaultRaycastLayers))
            if (hit.transform.gameObject.TryGetComponent(out DoorLock doorLock))
                CrawlingBehaviour.Instance.StartCoroutine(DoorHandler(doorLock));
        
        if (Vector3.Distance(data.Mask.transform.position, data.Path.corners[^1]) > 5f)
            return State.GoToNode;
        
        return State.ChooseTarget;

        IEnumerator DoorHandler(DoorLock doorLock)
        {
            var obstacle = doorLock.navMeshObstacle;
            if (!obstacle.enabled) yield break;
            obstacle.enabled = false;
            yield return new WaitForSeconds(1f);
            obstacle.enabled = true;
        }
    }

    private static State ChooseTarget(State previousState, Data data)
    {
        var position = data.Mask.transform.position;
        var sorted = Utils.GetActivePlayers(data.Inside)
            .OrderByDescending(player => -1 * Vector3.Distance(player.transform.position, position))
            .ToList();
        
        data.Path = new NavMeshPath();
        var found = false;
        for (var i = 0; !found && i < sorted.Count; i++)
        {
            if (!data.Agent.CalculatePath(sorted[i].transform.position, data.Path)) continue;
            // if (!Utils.PathNotVisibleByPlayer(data.Path)) continue;
            // todo: find a better way of making a path not visible by player
            data.TargetPlayer = sorted[i];
            found = true;
            CrawlingBehaviour.Instance.SetEyesFilledServerRpc(data.Mask.NetworkObject, true);
        }

        return found ? State.Flank : State.ChooseTarget;
    }
    
    private static State Flank(State previousState, Data data)
    {
        if (previousState != State.Flank)
        {
            data.Agent.speed = data.DemoAgent.speed * 1.5f;
            data.Agent.enabled = true;
            data.Agent.SetPath(data.Path);
        }

        var distance = Vector3.Distance(data.Mask.transform.position, data.Path.corners[^1]);
        if (distance <= 40 && data.Path.status == NavMeshPathStatus.PathComplete) return State.Lurk;
        if (distance > 40) return State.Flank;
        return State.ChooseTarget;
    }
    
    private static State Lurk(State previousState, Data data)
    {
        if (previousState != State.Lurk)
        {
            data.Agent.speed = data.DemoAgent.speed;
            data.TimeAroundPlayer = 0f;
        }
        
        data.TimeUntilRetarget -= Time.deltaTime;
        
        var pt = data.TargetPlayer.transform;
        var mt = data.Mask.transform;

        if (data.TargetPlayer.HasLineOfSightToPosition(mt.position, width: 15, range: 10)) return State.ChooseNode;
        
        if (data.TimeUntilRetarget <= 0f)
        {
            if (!Physics.Raycast(pt.position + Vector3.up * 2, -1 * (pt.up + pt.forward), out var hit, float.MaxValue, Physics.DefaultRaycastLayers))
                return State.ChooseTarget;
            if (!NavMesh.SamplePosition(hit.point, out data.Hit, float.MaxValue, NavMesh.AllAreas))
                return State.ChooseTarget;

            data.Path = new NavMeshPath();
            if (!data.Agent.CalculatePath(data.Hit.position, data.Path)) return State.ChooseTarget;
            data.Agent.SetPath(data.Path);
                
            data.TimeUntilRetarget = Data.TimeBetweenRetargets;
        }
        
        var distance = Vector3.Distance(mt.position, pt.position);
        switch (distance)
        {
            case < 15:
                data.TimeAroundPlayer += Time.deltaTime;
                break;
            case > 40:
                return State.ChooseTarget;
        }
        
        
        return data.TimeAroundPlayer > 10f ? State.Primed : State.Lurk;
    }
    
    private static State Primed(State previousState, Data data)
    {
        data.IsPrimed = true;
        data.Agent.enabled = false;
        data.Mask.transform.rotation = data.OriginalRotation;
        return State.Wait;
    }
    
    private static State Wait(State previousState, Data data)
    {
        bool heldByPlayer = data.Mask.playerHeldBy;
        if (data.IsPrimed)
        {
            if (heldByPlayer && !data.Mask.attaching)
                CrawlingBehaviour.Instance.AttachServerRpc(data.Mask.NetworkObject);
            return State.Wait;
        }

        data.Mask.enabled = true;
        data.Active = true;
        
        return heldByPlayer ? State.Wait : State.Initial;
    }
}