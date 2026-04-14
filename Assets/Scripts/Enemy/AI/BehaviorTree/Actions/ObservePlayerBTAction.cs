using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// Branch B — 안전 거리에서 플레이어를 관찰하며 대기. 시간 경과 후 Success 반환.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Observe Player",
    story: "[Agent] observes the player for [Duration] seconds",
    category: "Enemy AI/Actions",
    id: "d4e5f6a7-1111-2222-3333-aabbccddeeff")]
public partial class ObservePlayerBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float>      Duration;

    private NFBTEnemyAI _ai;
    private float       _timer;

    protected override Status OnStart()
    {
        _ai    = Agent.Value?.GetComponent<NFBTEnemyAI>();
        _timer = Duration.Value;
        return _ai != null ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        _ai.Enemy.Movement?.Move(0f);
        _timer -= Time.deltaTime;
        return _timer > 0f ? Status.Running : Status.Success;
    }
}
