using UnityEngine;

public class IdleState : IState
{
    private readonly EnemyAI _ai;
    private float _timer;
    private const float IdleDuration = 2f;

    public IdleState(EnemyAI ai) => _ai = ai;

    public void Enter()  => _timer = IdleDuration;
    public void Exit()   { }
    public void FixedUpdate() { }

    public void Update()
    {
        if (_ai.IsPlayerInRange(_ai.DetectionRange))
        {
            _ai.ChangeState(new ChaseState(_ai));
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f && _ai.PatrolPoints.Length > 0)
            _ai.ChangeState(new PatrolState(_ai));
    }
}
