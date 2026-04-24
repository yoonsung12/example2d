using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Patrol 분기 — 플레이어 감지 전까지 좌우 순찰.
/// 경계·낭떠러지·벽 감지 시 방향 전환. ActiveBranch == "Patrol" 동안 Running 유지.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Patrol",
    story: "[Agent] patrols between left and right bounds",
    category: "Enemy AI/Actions",
    id: "f7a8b9c0-5555-6666-7777-eeff00112233")]
public partial class PatrolBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent; // 순찰할 적 오브젝트

    private NFBTEnemyAI _ai;         // 적 AI 컴포넌트 참조
    private float       _currentDir; // 현재 이동 방향 (+1: 우측, -1: 좌측)

    protected override Status OnStart()
    {
        _ai         = Agent.Value?.GetComponent<NFBTEnemyAI>(); // AI 컴포넌트 캐싱
        _currentDir = 1f;                                        // 초기 방향: 우측
        return _ai != null ? Status.Running : Status.Failure;   // AI 없으면 즉시 실패
    }

    protected override Status OnUpdate()
    {
        // ActiveBranch가 "Patrol"이 아니면 Failure 반환 → Selector가 처음부터 재평가
        if (_ai.ActiveBranch != "Patrol") return Status.Failure;

        EnemyBase enemy = _ai.Enemy; // 적 기본 컴포넌트 참조

        if (ShouldReverse(enemy)) _currentDir = -_currentDir; // 방향 전환 필요 시 반전

        enemy.Movement?.Move(_currentDir); // 현재 방향으로 이동 명령

        return Status.Running; // Patrol 분기 활성인 동안 Running 유지
    }

    protected override void OnEnd()
    {
        _ai?.Enemy.Movement?.Move(0f); // 분기 종료 시 이동 정지
    }

    // 경계·낭떠러지·벽 중 하나라도 감지되면 true 반환
    private bool ShouldReverse(EnemyBase enemy)
    {
        float x = enemy.transform.position.x; // 현재 x 좌표

        // 좌측 경계 도달: 왼쪽 이동 중이고 경계를 벗어남
        if (_ai.LeftBound  != null && _currentDir < 0f && x <= _ai.LeftBound.position.x)  return true;
        // 우측 경계 도달: 오른쪽 이동 중이고 경계를 벗어남
        if (_ai.RightBound != null && _currentDir > 0f && x >= _ai.RightBound.position.x) return true;

        // 낭떠러지 감지: 전방 하단에 땅이 없으면 true
        Vector2 edgeOrigin = (Vector2)enemy.transform.position
                           + new Vector2(_currentDir * _ai.EdgeCheckDist, 0f); // 전방 체크 기점 계산
        if (!Physics2D.Raycast(edgeOrigin, Vector2.down, 1f, _ai.GroundLayer)) return true;

        // 벽 감지: 전방 수평 방향에 지형이 있으면 true
        if (Physics2D.Raycast(
                enemy.transform.position,           // 레이 시작점
                Vector2.right * _currentDir,        // 현재 이동 방향
                0.5f,                               // 감지 거리
                _ai.GroundLayer)) return true;      // 지형 레이어만 감지

        return false; // 방향 전환 불필요
    }
}
