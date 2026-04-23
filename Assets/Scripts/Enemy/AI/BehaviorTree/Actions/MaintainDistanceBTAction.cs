using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Counter 분기 — 플레이어와 지정된 거리 범위를 유지합니다.
/// MinDistance보다 가까우면 후퇴, MaxDistance보다 멀면 전진, 그 사이면 정지.
/// 항상 Running을 반환하며 Counter 분기가 활성인 동안 지속 실행됩니다.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Maintain Distance",
    story: "[Agent] maintains distance from player between [MinDistance] and [MaxDistance]",
    category: "Enemy AI/Actions",
    id: "b2c3d4e5-2222-3333-4444-bbccddeeff00")]
public partial class MaintainDistanceBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;       // 이 액션을 실행할 적 오브젝트
    [SerializeReference] public BlackboardVariable<float>      MinDistance; // 플레이어와 유지할 최소 거리
    [SerializeReference] public BlackboardVariable<float>      MaxDistance; // 플레이어와 유지할 최대 거리

    private NFBTEnemyAI _ai; // 적 AI 컴포넌트 참조

    protected override Status OnStart()
    {
        _ai = Agent.Value?.GetComponent<NFBTEnemyAI>(); // 적 AI 컴포넌트 캐싱
        return _ai != null ? Status.Running : Status.Failure; // AI 없으면 즉시 실패
    }

    protected override Status OnUpdate()
    {
        Transform player = _ai.PlayerTransform; // 플레이어 위치
        EnemyBase enemy  = _ai.Enemy;           // 적 기본 컴포넌트

        if (player == null) return Status.Failure; // 플레이어 없으면 실패

        float dist = Vector2.Distance(enemy.transform.position, player.position); // 플레이어까지 현재 거리

        if (dist < MinDistance.Value)
        {
            // 너무 가까우면 플레이어 반대 방향으로 후퇴
            float dir = enemy.transform.position.x > player.position.x ? 1f : -1f; // 플레이어 반대 방향
            enemy.Movement?.Move(dir);
        }
        else if (dist > MaxDistance.Value)
        {
            // 너무 멀면 플레이어 방향으로 전진
            float dir = player.position.x > enemy.transform.position.x ? 1f : -1f; // 플레이어 방향
            enemy.Movement?.Move(dir);
        }
        else
        {
            // 적정 거리 유지 중이면 정지
            enemy.Movement?.Move(0f);
        }

        return Status.Running; // 항상 Running (Counter 분기 내내 지속 실행)
    }

    protected override void OnEnd()
    {
        _ai?.Enemy.Movement?.Move(0f); // 액션 종료 시 이동 정지
    }
}
