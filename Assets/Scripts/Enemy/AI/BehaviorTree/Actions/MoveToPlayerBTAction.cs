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
        enemy.Movement?.Move(dir);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _ai?.Enemy.Movement?.Move(0f);
    }
}
