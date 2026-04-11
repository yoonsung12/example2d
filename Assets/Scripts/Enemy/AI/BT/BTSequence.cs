using System.Collections.Generic;

/// <summary>
/// Sequence 복합 노드 (AND). 자식을 순서대로 실행하며,
/// 하나라도 Failure 시 즉시 Failure 반환. 모두 Success 시 Success 반환.
/// </summary>
public class BTSequence : BTNode
{
    private readonly List<BTNode> _children;

    public BTSequence(NFBTEnemyAI ctx, List<BTNode> children) : base(ctx)
    {
        _children = children;
    }

    public override NodeState Evaluate()
    {
        foreach (var child in _children)
        {
            NodeState state = child.Evaluate();
            if (state == NodeState.Running) return NodeState.Running;
            if (state == NodeState.Failure) return NodeState.Failure;
            // Success → 다음 자식으로 진행
        }
        return NodeState.Success;
    }

    public override void OnEnter()
    {
        foreach (var child in _children)
            child.OnEnter();
    }

    public override void OnExit()
    {
        foreach (var child in _children)
            child.OnExit();
    }
}
