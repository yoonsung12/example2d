public class StateMachine
{
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    public void Update()      => CurrentState?.Update();
    public void FixedUpdate() => CurrentState?.FixedUpdate();
}
