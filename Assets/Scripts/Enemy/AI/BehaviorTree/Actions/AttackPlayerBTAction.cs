using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Branch A/C — 플레이어 근접 공격. 공격 범위 벗어나면 Failure 반환.
/// ExtraCooldown: Branch C에서 0.5f 추가 쿨다운을 줄 때 사용.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Attack Player",
    story: "[Agent] attacks the player, optionally with [ExtraCooldown] extra cooldown",
    category: "Enemy AI/Actions",
    id: "b2c3d4e5-1111-2222-3333-aabbccddeeff")]
public partial class AttackPlayerBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float>      ExtraCooldown;

    private NFBTEnemyAI _ai;
    private float       _extraCooldownTimer;

    protected override Status OnStart()
    {
        _ai                 = Agent.Value?.GetComponent<NFBTEnemyAI>(); // AI 컴포넌트 캐싱
        _extraCooldownTimer = 0f;                                        // 쿨다운 타이머 초기화
        _ai?.Enemy.Movement?.Move(0f);                                   // 공격 시작 즉시 이동 정지
        return _ai != null ? Status.Running : Status.Failure;           // AI 없으면 실패
    }

    protected override Status OnUpdate()
    {
        Transform player = _ai.PlayerTransform;
        EnemyBase enemy  = _ai.Enemy;

        if (player == null) return Status.Failure;

        float dist = Vector2.Distance(enemy.transform.position, player.position);
        if (dist > _ai.AttackRange) return Status.Failure;

        // 추가 쿨다운 처리 (CharacterCombat 내부 쿨다운과 별개)
        _extraCooldownTimer -= Time.deltaTime;
        if (_extraCooldownTimer > 0f) return Status.Running;

        if (enemy.Combat != null && enemy.Combat.CanAttack)
        {
            enemy.Combat.StartAttack();
            _extraCooldownTimer = ExtraCooldown.Value;
        }

        return Status.Running;
    }
}
