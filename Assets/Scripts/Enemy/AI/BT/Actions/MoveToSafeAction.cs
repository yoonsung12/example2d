using UnityEngine;

/// <summary>
/// Branch B — 플레이어로부터 멀어져 안전 거리 확보. 확보되면 Success 반환.
/// </summary>
public class MoveToSafeAction : BTNode
{
    private readonly float _safeDistance;

    public MoveToSafeAction(NFBTEnemyAI ctx, float safeDistance = 9f) : base(ctx)
    {
        _safeDistance = safeDistance;
    }

    public override NodeState Evaluate()
    {
        if (Ctx.PlayerTransform == null) return NodeState.Failure;

        float dist = Vector2.Distance(Ctx.transform.position, Ctx.PlayerTransform.position);
        if (dist >= _safeDistance) return NodeState.Success;

        // 플레이어 반대 방향으로 이동
        float dir = Ctx.transform.position.x > Ctx.PlayerTransform.position.x ? 1f : -1f;
        Ctx.Enemy.Movement?.Move(dir);
        return NodeState.Running;
    }

    public override void OnExit()
    {
        Ctx.Enemy.Movement?.Move(0f);
    }
}
