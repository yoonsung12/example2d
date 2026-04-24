using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Branch A — 플레이어 추격. 공격 범위 내 도달 시 Success 반환.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Move To Player",
    story: "[Agent] moves toward the player until within attack range",
    category: "Enemy AI/Actions",
    id: "a1b2c3d4-1111-2222-3333-aabbccddeeff")]
public partial class MoveToPlayerBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

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

        float dist = Vector2.Distance(enemy.transform.position, player.position);
        if (dist <= _ai.AttackRange) return Status.Success;

        float dir = player.position.x > enemy.transform.position.x ? 1f : -1f;

        // 진행 방향 전방 하단에 땅이 없으면 이동 정지 (낭떠러지 방지)
        Vector2 edgeOrigin = (Vector2)enemy.transform.position
                           + new Vector2(dir * _ai.EdgeCheckDist, 0f); // 전방 체크 기점
        bool hasGround = Physics2D.Raycast(edgeOrigin, Vector2.down, 1f, _ai.GroundLayer); // 아래로 레이캐스트
        if (!hasGround)
        {
            enemy.Movement?.Move(0f); // 낭떠러지 감지 시 정지
            return Status.Running;    // 플레이어를 기다리며 Running 유지
        }

        enemy.Movement?.Move(dir); // 낭떠러지 없으면 정상 이동
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _ai?.Enemy.Movement?.Move(0f);
    }
}
