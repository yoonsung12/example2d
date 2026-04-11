using UnityEngine;

/// <summary>
/// Branch C — 플레이어 진행 방향 앞쪽(매복 위치)으로 이동. 도달 시 Success 반환.
/// </summary>
public class MoveToAmbushAction : BTNode
{
    private const float AmbushLeadDistance = 4f;  // 플레이어 앞쪽 거리
    private const float ArrivalThreshold   = 0.6f;

    public MoveToAmbushAction(NFBTEnemyAI ctx) : base(ctx) { }

    public override NodeState Evaluate()
    {
        if (Ctx.PlayerTransform == null) return NodeState.Failure;

        // 플레이어 이동 방향 추정 (Rigidbody velocity 기반)
        float playerDirX = 0f;
        var rb = Ctx.PlayerTransform.GetComponent<Rigidbody2D>();
        if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            playerDirX = Mathf.Sign(rb.linearVelocity.x);

        Vector2 ambushPos = (Vector2)Ctx.PlayerTransform.position
                          + new Vector2(playerDirX * AmbushLeadDistance, 0f);

        float dist = Vector2.Distance(Ctx.transform.position, ambushPos);
        if (dist < ArrivalThreshold) return NodeState.Success;

        float dir = ambushPos.x > Ctx.transform.position.x ? 1f : -1f;
        Ctx.Enemy.Movement?.Move(dir);
        return NodeState.Running;
    }

    public override void OnExit()
    {
        Ctx.Enemy.Movement?.Move(0f);
    }
}
