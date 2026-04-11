/// <summary>
/// 비헤이비어 트리 노드의 실행 상태.
/// </summary>
public enum NodeState
{
    Running,
    Success,
    Failure
}

/// <summary>
/// BT 노드 베이스 클래스. 모든 노드는 NFBTEnemyAI 컨텍스트를 참조합니다.
/// </summary>
public abstract class BTNode
{
    protected readonly NFBTEnemyAI Ctx;

    protected BTNode(NFBTEnemyAI ctx)
    {
        Ctx = ctx;
    }

    /// <summary>이 노드가 활성화될 때 1회 호출</summary>
    public virtual void OnEnter() { }

    /// <summary>이 노드가 비활성화될 때 1회 호출</summary>
    public virtual void OnExit() { }

    /// <summary>매 프레임 평가. Running/Success/Failure 반환.</summary>
    public abstract NodeState Evaluate();
}
