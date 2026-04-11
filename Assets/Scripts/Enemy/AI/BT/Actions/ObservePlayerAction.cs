using UnityEngine;

/// <summary>
/// Branch B — 안전 거리에서 플레이어를 관찰하며 대기. 시간 경과 후 Success 반환.
/// </summary>
public class ObservePlayerAction : BTNode
{
    private readonly float _duration;
    private float _timer;

    public ObservePlayerAction(NFBTEnemyAI ctx, float duration = 2f) : base(ctx)
    {
        _duration = duration;
    }

    public override void OnEnter()
    {
        _timer = _duration;
    }

    public override NodeState Evaluate()
    {
        Ctx.Enemy.Movement?.Move(0f);
        _timer -= Time.deltaTime;
        return _timer > 0f ? NodeState.Running : NodeState.Success;
    }
}
