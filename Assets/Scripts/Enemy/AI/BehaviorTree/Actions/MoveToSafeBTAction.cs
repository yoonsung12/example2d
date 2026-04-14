using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Branch B — 플레이어로부터 멀어져 안전 거리 확보. 확보되면 Success 반환.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Move To Safe",
    story: "[Agent] retreats until [SafeDistance] from player",
    category: "Enemy AI/Actions",
    id: "c3d4e5f6-1111-2222-3333-aabbccddeeff")]
public partial class MoveToSafeBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float>      SafeDistance;

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
        if (dist >= SafeDistance.Value) return Status.Success;

        // 플레이어 반대 방향으로 이동
        float dir = enemy.transform.position.x > player.position.x ? 1f : -1f;
        enemy.Movement?.Move(dir);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        _ai?.Enemy.Movement?.Move(0f);
    }
}
