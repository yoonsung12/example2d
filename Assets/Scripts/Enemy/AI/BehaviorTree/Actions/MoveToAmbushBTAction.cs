using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Branch C — 플레이어 진행 방향 앞쪽(매복 위치)으로 이동. 도달 시 Success 반환.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Move To Ambush",
    story: "[Agent] moves ahead of the player to ambush position",
    category: "Enemy AI/Actions",
    id: "e5f6a7b8-1111-2222-3333-aabbccddeeff")]
public partial class MoveToAmbushBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    private const float AmbushLeadDistance = 4f;
    private const float ArrivalThreshold   = 0.6f;

    private NFBTEnemyAI _ai;

    protected override Status OnStart()
    {
        _ai = Agent.Value?.GetComponent<NFBTEnemyAI>();
        return _ai != null ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        Transform player = _ai.PlayerTransform;
        EnemyBase enemy  = _ai.Enemy;

        if (player == null) return Status.Failure;

        // 플레이어 이동 방향 추정 (Rigidbody2D velocity 기반)
        float playerDirX = 0f;
        var   rb         = player.GetComponent<Rigidbody2D>();
        if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            playerDirX = Mathf.Sign(rb.linearVelocity.x);

        Vector2 ambushPos = (Vector2)player.position
                          + new Vector2(playerDirX * AmbushLeadDistance, 0f);

        float dist = Vector2.Distance(enemy.transform.position, ambushPos);
        if (dist < ArrivalThreshold) return Status.Success;

        float dir = ambushPos.x > enemy.transform.position.x ? 1f : -1f;
        enemy.Movement?.Move(dir);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _ai?.Enemy.Movement?.Move(0f);
    }
}
