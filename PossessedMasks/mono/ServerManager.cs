using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using PossessedMasksRewrite.networking;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PossessedMasksRewrite.mono;

public class ServerManager : MonoBehaviour
{
    private int _currIndex;
    private bool _usingData = false;
    private readonly Dictionary<PlayerControllerB, PlayerProps> _playerProps = new();
    private readonly List<PlayerControllerB> _activePlayers = [];

    private float _avgFrameTime;

    private bool ShouldUpdate => Utils.InLevel && _activePlayers.Count > 0;
    
    private class PlayerProps
    {
        public float TimeUntilSlotSwitch = Random.Range(ModConfig.MinTimeToSwitchSlots.Value,
            ModConfig.MaxTimeToSwitchSlots.Value) + ModConfig.TimeToStartSwitchingSlots.Value;
        private float _timeUntilSlotSwitchDelta = 0;
        
        public bool Possessing = false;
        public float TimeUntilPossession = Random.Range(ModConfig.MinTimeToPossess.Value, 
            ModConfig.MaxTimeToPossess.Value) + ModConfig.TimeToStartPossession.Value;

        private float _timeUntilPossessionDelta = 0;

        public float TimeUntilUnpossession = 0;
        private float _timeUntilUnpossessionDelta = 0;

        public void GenerateTimeUntilSlotSwitch()
        {
            _timeUntilSlotSwitchDelta += ModConfig.DeltaTimeToSwitchSlots.Value;
            TimeUntilSlotSwitch = Mathf.Clamp(Random.Range(ModConfig.MinTimeToSwitchSlots.Value - _timeUntilSlotSwitchDelta,
                ModConfig.MaxTimeToSwitchSlots.Value - _timeUntilSlotSwitchDelta), ModConfig.MinSwitchingSlotTime.Value, float.MaxValue);
        }
        
        public void GenerateTimeUntilPossession()
        {
            _timeUntilPossessionDelta += ModConfig.DeltaTimeToPossess.Value;
            TimeUntilPossession = Mathf.Clamp(Random.Range(ModConfig.MinTimeToPossess.Value - _timeUntilPossessionDelta, 
                ModConfig.MaxTimeToPossess.Value - _timeUntilPossessionDelta), ModConfig.MinPossessingTime.Value, float.MaxValue);
        }
        
        public void GenerateTimeUntilUnpossession()
        {
            _timeUntilUnpossessionDelta += ModConfig.DeltaTimeToPossessPlayer.Value;
            TimeUntilUnpossession = Mathf.Clamp(Random.Range(ModConfig.MinTimeToPossessPlayer.Value + _timeUntilUnpossessionDelta, 
                ModConfig.MaxTimeToPossessPlayer.Value + _timeUntilUnpossessionDelta), float.MinValue, ModConfig.MaxPossessingPlayerTime.Value);
        }
    }
    
    private void Start()
    {
        StartCoroutine(WaitUntilAllowed());
    }
    
    private void Update()
    {
        _avgFrameTime = (_avgFrameTime + Time.deltaTime) / 2;
        
        if (!ShouldUpdate)
        {
            if (_activePlayers.Count == 0 && _currIndex == 0) return;

            _activePlayers.Clear();
            _currIndex = 0;

            return;
        }

        var player = _activePlayers[_currIndex];
        if (!_usingData && Utils.IsActivePlayer(player))
        {
            _usingData = true;
            DoInterval(player);
            _usingData = false;
            _currIndex = (_currIndex + 1) % _activePlayers.Count;
        }
        else
        {
            _activePlayers.Remove(player);
            _currIndex = 0;
        }
    }
    
    private void DoInterval(PlayerControllerB player)
    {
        // check for mask
        var (slot, forward) = FindClosestMask(player);
        // if player doesn't have mask, return
        if (slot == -1) return;

        if (!_playerProps.TryGetValue(player, out var props)) return;

        var delta = _avgFrameTime * _activePlayers.Count;
        if (ModConfig.EnableMaskSwitchSlotMechanic.Value)
            props.TimeUntilSlotSwitch -= delta;
        if (ModConfig.EnableMaskPossessionMechanic.Value)
        {
            props.TimeUntilPossession -= delta;
            props.TimeUntilUnpossession -= delta;
        }

        // if player is possessing, check if they should stop
        if (props is { Possessing: true, TimeUntilUnpossession: <= 0f })
        {
            // roll new random timers
            props.GenerateTimeUntilPossession();
            props.GenerateTimeUntilSlotSwitch();
            // stop possession
            PossessedBehaviour.Instance.StopPossessionServerRpc(player.OwnerClientId);
            props.Possessing = false;
        }
        // if player is holding a mask, and time to possess has come
        else if (slot == player.currentItemSlot && props is { Possessing: false, TimeUntilPossession: <= 0f })
        {
            // roll random timer for unpossession
            props.GenerateTimeUntilUnpossession();
            // start possession
            PossessedBehaviour.Instance.StartPossessionServerRpc(player.OwnerClientId);
            props.Possessing = true;
        }
        else if (slot != player.currentItemSlot && props is { Possessing: false, TimeUntilSlotSwitch: <= 0f })
        {
            // roll random timer for slot switch
            props.GenerateTimeUntilSlotSwitch();
            // switch to mask
            PossessedBehaviour.Instance.SwitchSlotServerRpc(player.OwnerClientId,
                forward,
                slot);
        }
    }

    private IEnumerator NewLevel()
    {
        yield return new WaitUntil(() => !_usingData);
        _usingData = true;
        _activePlayers.Clear();
        _playerProps.Clear();
        _activePlayers.AddRange(StartOfRound.Instance.allPlayerScripts.Where(Utils.IsActivePlayer));
        _activePlayers.ForEach(player => _playerProps[player] = new PlayerProps());
        _usingData = false;
    }
    
    private IEnumerator WaitUntilAllowed()
    {
        Plugin.Log.LogDebug("Waiting until allowed");
        yield return new WaitUntil(() => Utils.InLevel);
        Plugin.Log.LogDebug("Allowed!");
        yield return new WaitForEndOfFrame();
        StartCoroutine(NewLevel());
        Plugin.Log.LogDebug("Waiting until not allowed");
        yield return new WaitUntil(() => !Utils.InLevel);
        Plugin.Log.LogDebug("Not allowed!");
        yield return new WaitForEndOfFrame();
        StartCoroutine(WaitUntilAllowed());
    }
    
    private static (int, bool) FindClosestMask(PlayerControllerB player)
    {
        var active = player.currentItemSlot;
        var numOfSlots = player.ItemSlots.Length;
        for (var i = 0; i <= numOfSlots / 2; i++)
        {
            var checkInd = Utils.MathMod(active + i, numOfSlots);
            if (player.ItemSlots[checkInd] != null && player.ItemSlots[checkInd] is HauntedMaskItem)
                return (checkInd, true);

            checkInd = Utils.MathMod(active - i, numOfSlots);
            if (player.ItemSlots[checkInd] != null && player.ItemSlots[checkInd] is HauntedMaskItem)
                return (checkInd, false);
        }

        return (-1, false);
    }
}