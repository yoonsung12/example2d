using UnityEngine;

/// <summary>
/// Branch A — 플레이어 추격. 공격 범위 내 도달 시 Success 반환.
/// </summary>
public class MoveToPlayerAction : BTNode
{
    public MoveToPlayerAction(NFBTEnemyAI ctx) : base(ctx) { }

    public override NodeState Evaluate()
    {
        if (Ctx.PlayerTransform == null) return NodeState.Failure;

        float dist = Vector2.Distance(Ctx.transform.position, Ctx.PlayerTransform.position);

        if (dist <= Ctx.AttackRange) return NodeState.Success;

        float dir = Ctx.PlayerTransform.position.x > Ctx.transform.position.x ? 1f : -1f;
        Ctx.Enemy.Movement?.Move(dir);
        return NodeState.Running;
    }

    public override void OnExit()
    {
        Ctx.Enemy.Movement?.Move(0f);
    }
}
