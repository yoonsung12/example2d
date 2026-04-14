using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// <summary>
/// ActiveBranch 가 BranchName 과 일치하면 Success, 아니면 Failure 반환.
/// Selector 분기 선택에 사용합니다.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Enemy Check Active Branch",
    story: "[Agent] checks if active branch is [BranchName]",
    category: "Enemy AI/Actions",
    id: "a9b8c7d6-1111-2222-3333-aabbccddeeff")]
public partial class CheckActiveBranchBTAction : Unity.Behavior.Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<string>     BranchName;

    protected override Status OnStart()
    {
        var ai = Agent.Value?.GetComponent<NFBTEnemyAI>();
        if (ai == null) return Status.Failure;
        return ai.ActiveBranch == BranchName.Value ? Status.Success : Status.Failure;
    }

    protected override Status OnUpdate() => Status.Success;
}
