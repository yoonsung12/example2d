using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// NFBTEnemyAI가 계산한 ActiveBranch가 지정된 이름과 일치하면 true 반환.
/// Behavior Graph 내 Selector 분기 선택에 사용됩니다.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Is Active Branch",
    story: "[Agent] active branch is [BranchName]",
    category: "Enemy AI/Conditions",
    id: "f6a7b8c9-1111-2222-3333-aabbccddeeff")]
public partial class IsActiveBranchBTCondition : Unity.Behavior.Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<string>     BranchName;

    public override bool IsTrue()
    {
        var ai = Agent.Value?.GetComponent<NFBTEnemyAI>();
        return ai != null && ai.ActiveBranch == BranchName.Value;
    }
}
