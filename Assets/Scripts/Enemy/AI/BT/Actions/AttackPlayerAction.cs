using UnityEngine;

/// <summary>
/// Branch A — 플레이어 근접 공격. 공격 범위 벗어나면 Failure 반환.
/// </summary>
public class AttackPlayerAction : BTNode
{
    private readonly float _extraCooldown;

    public AttackPlayerAction(NFBTEnemyAI ctx, float extraCooldown = 0f) : base(ctx)
    {
        _extraCooldown = extraCooldown;
    }

    private float _cooldownTimer;

    public override void OnEnter()
    {
        _cooldownTimer = 0f;
    }

    public override NodeState Evaluate()
    {
        if (Ctx.PlayerTransform == null) return NodeState.Failure;

        float dist = Vector2.Distance(Ctx.transform.position, Ctx.PlayerTransform.position);
        if (dist > Ctx.AttackRange) return NodeState.Failure;

        // 추가 쿨다운 처리 (CharacterCombat 내부 쿨다운과 별개)
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer > 0f) return NodeState.Running;

        if (Ctx.Enemy.Combat != null && Ctx.Enemy.Combat.CanAttack)
        {
            Ctx.Enemy.Combat.StartAttack();
            _cooldownTimer = _extraCooldown;
        }

        return NodeState.Running;
    }
}
