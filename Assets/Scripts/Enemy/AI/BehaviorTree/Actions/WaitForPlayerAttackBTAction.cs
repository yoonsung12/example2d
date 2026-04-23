using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Counter 분기 — 플레이어의 공격 사이클을 감지하여 빈틈을 포착합니다.
/// 플레이어가 공격을 시작했다가 끝내는 순간 Success를 반환합니다.
/// (공격 시작 → 공격 종료 = 반격 타이밍)
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Wait For Player Attack",
    story: "[Agent] waits for player to finish an attack",
    category: "Enemy AI/Actions",
    id: "c3d4e5f6-3333-4444-5555-ccddeeff0011")]
public partial class WaitForPlayerAttackBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent; // 이 액션을 실행할 적 오브젝트

    // 플레이어 공격 감지 내부 상태
    private enum DetectState
    {
        WaitingForStart, // 플레이어가 공격을 시작하길 기다리는 중
        WaitingForEnd,   // 공격이 끝나길 기다리는 중 (빈틈 감지 대기)
    }

    private NFBTEnemyAI      _ai;           // 적 AI 컴포넌트 참조
    private CharacterCombat  _playerCombat; // 플레이어 전투 컴포넌트 참조
    private DetectState      _state;        // 현재 감지 상태

    protected override Status OnStart()
    {
        _ai = Agent.Value?.GetComponent<NFBTEnemyAI>(); // 적 AI 캐싱
        if (_ai == null) return Status.Failure;          // AI 없으면 즉시 실패

        // 플레이어 전투 컴포넌트 탐색
        var player = _ai.PlayerTransform?.GetComponent<CharacterBase>();
        _playerCombat = player?.GetComponent<CharacterCombat>(); // 플레이어 CharacterCombat 캐싱

        if (_playerCombat == null) return Status.Failure; // 플레이어 전투 컴포넌트 없으면 실패

        _state = DetectState.WaitingForStart; // 초기 상태: 공격 시작 대기
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_playerCombat == null) return Status.Failure; // 플레이어 컴포넌트 유효성 검사

        bool playerAttacking = _playerCombat.IsAttacking; // 플레이어 현재 공격 중 여부

        switch (_state)
        {
            case DetectState.WaitingForStart:
                if (playerAttacking)
                    _state = DetectState.WaitingForEnd; // 공격 시작 감지 → 끝나길 기다림
                break;

            case DetectState.WaitingForEnd:
                if (!playerAttacking)
                    return Status.Success; // 공격이 끝남 = 빈틈 포착 → 반격 트리거
                break;
        }

        return Status.Running; // 아직 빈틈 미감지 → 대기 계속
    }

    protected override void OnEnd()
    {
        _state = DetectState.WaitingForStart; // 상태 초기화
    }
}
