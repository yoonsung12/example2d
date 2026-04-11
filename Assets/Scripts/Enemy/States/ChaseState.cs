using UnityEngine;

public class ChaseState : IState
{
    private readonly EnemyAI _ai;

    public ChaseState(EnemyAI ai) => _ai = ai;

    public void Enter()  { }
    public void Exit()   { }

    public void Update()
    {
        if (!_ai.IsPlayerInRange(_ai.DetectionRange))
        {
            _ai.ChangeState(new IdleState(_ai));
            return;
        }
        if (_ai.IsPlayerInRange(_ai.AttackRange))
            _ai.ChangeState(new AttackState(_ai));
    }

    public void FixedUpdate()
    {
        if (_ai.PlayerTransform == null) return;
        float dir = _ai.PlayerTransform.position.x > _ai.transform.position.x ? 1f : -1f;
        _ai.Enemy.Movement?.Move(dir);
    }
}
