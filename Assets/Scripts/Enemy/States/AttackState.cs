using UnityEngine;

public class AttackState : IState
{
    private readonly EnemyAI _ai;
    private float _attackTimer;
    private const float AttackCooldown = 1.5f;

    public AttackState(EnemyAI ai) => _ai = ai;

    public void Enter()
    {
        _attackTimer = 0f;
        _ai.Enemy.Movement?.Move(0f);
    }

    public void Exit() { }

    public void Update()
    {
        if (!_ai.IsPlayerInRange(_ai.AttackRange))
        {
            _ai.ChangeState(new ChaseState(_ai));
            return;
        }

        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            _ai.Enemy.Combat?.StartAttack();
            _attackTimer = AttackCooldown;
        }
    }

    public void FixedUpdate() { }
}
