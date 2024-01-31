namespace PossessedMasks.machines.Def;

public sealed class FiniteStateMachine<TState, TData>(TData data)
    where TState : Enum
{
    private readonly Dictionary<TState, Func<TState, TData, TState>> _stateActions = new();
    public Action<TState, TState, TData> PreTickActions;
    
    public TState PreviousState { get; private set; }
    public TState CurrentState { get; private set; }

    public void AddPreTickAction(Action<TState, TState, TData> action)
    {
        PreTickActions += action;
    }
    
    public void AddAction(TState state, Func<TState, TData, TState> function)
    {
        if (_stateActions.TryAdd(state, function)) return;
        _stateActions[state] = function;
    }

    private void Reset()
    {
        SwitchStates(default);
    }
    
    public void Tick()
    {
        PreTickActions?.Invoke(PreviousState, CurrentState, data);
        if (!_stateActions.TryGetValue(CurrentState, out var action))
        {
            Plugin.Log.LogError($"No action for state {CurrentState.ToString()}, restarting to initial state!");
            Reset();
            return;
        }
        
        SwitchStates(action.Invoke(PreviousState, data));
    }

    public void SwitchStates(TState newState)
    {
        PreviousState = CurrentState;
        CurrentState = newState;
    }
}