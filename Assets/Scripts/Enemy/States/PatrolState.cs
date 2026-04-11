using UnityEngine;

public class PatrolState : IState
{
    private readonly EnemyAI _ai;
    private int   _index;
    private const float Threshold = 0.3f;

    public PatrolState(EnemyAI ai) => _ai = ai;

    public void Enter()  { }
    public void Exit()   { }

    public void Update()
    {
        if (_ai.IsPlayerInRange(_ai.DetectionRange))
        {
            _ai.ChangeState(new ChaseState(_ai));
            return;
        }

        if (_ai.PatrolPoints.Length == 0) return;

        float dist = Vector2.Distance(_ai.transform.position, _ai.PatrolPoints[_index].position);
        if (dist < Threshold)
            _index = (_index + 1) % _ai.PatrolPoints.Length;
    }

    public void FixedUpdate()
    {
        if (_ai.PatrolPoints.Length == 0) return;
        float dir = _ai.PatrolPoints[_index].position.x > _ai.transform.position.x ? 1f : -1f;
        _ai.Enemy.Movement?.Move(dir);
    }
}
