using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Counter 분기 — 플레이어의 공격 빈틈에 빠르게 돌진해 반격합니다.
/// WaitForPlayerAttackBTAction이 Success를 반환한 직후 실행됩니다.
/// 공격 성공 시 Success, 타임아웃 시 Failure를 반환합니다.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Counter Attack",
    story: "[Agent] rushes and counter-attacks the player within [Timeout] seconds",
    category: "Enemy AI/Actions",
    id: "d4e5f6a7-4444-5555-6666-ddeeff001122")]
public partial class CounterAttackBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;     // 이 액션을 실행할 적 오브젝트
    [SerializeReference] public BlackboardVariable<float>      RushSpeed; // 돌진 속도 배율 (기본 이동속도 × 배율)
    [SerializeReference] public BlackboardVariable<float>      Timeout;   // 최대 돌진 허용 시간 (초)

    private NFBTEnemyAI _ai;          // 적 AI 컴포넌트 참조
    private float       _timer;       // 타임아웃 추적 타이머
    private bool        _attacked;    // 이번 사이클에서 공격을 실행했는지 여부

    protected override Status OnStart()
    {
        _ai = Agent.Value?.GetComponent<NFBTEnemyAI>(); // 적 AI 캐싱
        if (_ai == null) return Status.Failure;          // AI 없으면 즉시 실패

        _timer   = Timeout.Value; // 타임아웃 타이머 초기화
        _attacked = false;         // 공격 플래그 초기화
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Transform player = _ai.PlayerTransform; // 플레이어 위치
        EnemyBase enemy  = _ai.Enemy;           // 적 기본 컴포넌트

        if (player == null) return Status.Failure; // 플레이어 없으면 실패

        _timer -= Time.deltaTime; // 타임아웃 타이머 감소
        if (_timer <= 0f)
        {
            // 시간 초과 → 빈틈 놓침
            enemy.Movement?.Move(0f);
            return Status.Failure;
        }

        float dist = Vector2.Distance(enemy.transform.position, player.position); // 플레이어까지 거리

        if (dist > _ai.AttackRange)
        {
            // 공격 범위 밖 → RushSpeed 배율로 돌진
            float dir      = player.position.x > enemy.transform.position.x ? 1f : -1f; // 플레이어 방향
            float baseSpeed = enemy.Movement != null ? 1f : 1f; // 기본 이동 (Move()가 내부적으로 속도 적용)
            enemy.Movement?.Move(dir * RushSpeed.Value); // 돌진 이동 (배율 적용)
        }
        else if (!_attacked)
        {
            // 공격 범위 내 진입 → 반격 실행 (1회)
            enemy.Movement?.Move(0f);          // 이동 정지
            enemy.Combat?.StartAttack();        // 공격 실행
            _attacked = true;                   // 공격 완료 플래그 설정
        }
        else
        {
            // 공격 완료 → 이 프레임에서 CanAttack이 다시 true가 될 때까지 대기
            if (enemy.Combat != null && enemy.Combat.CanAttack)
                return Status.Success; // 공격 쿨다운 끝 = 반격 사이클 완료
        }

        return Status.Running; // 돌진 또는 공격 쿨다운 중
    }

    protected override void OnEnd()
    {
        _ai?.Enemy.Movement?.Move(0f); // 액션 종료 시 이동 정지
    }
}
