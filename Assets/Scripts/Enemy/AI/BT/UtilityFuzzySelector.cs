using System.Collections.Generic;

/// <summary>
/// Utility Fuzzy Selector: 각 분기의 U_final = 0.7*S_base + 0.3*S_fuzzy 를 계산하여
/// 가장 높은 분기를 선택, 실행합니다.
/// </summary>
public class UtilityFuzzySelector : BTNode
{
    public class Branch
    {
        public string Name   = "Branch";
        public BTNode Node;
        public float  SBase;   // 기본 유틸리티 (거리/HP 삼각 판단)
        public float  SFuzzy;  // 퍼지 규칙 유틸리티

        public float UFinal => 0.7f * SBase + 0.3f * SFuzzy;
    }

    private readonly List<Branch> _branches;
    private Branch _activeBranch;

    public string ActiveBranchName => _activeBranch?.Name ?? "None";

    public UtilityFuzzySelector(NFBTEnemyAI ctx, List<Branch> branches) : base(ctx)
    {
        _branches = branches;
    }

    /// <summary>매 프레임 각 분기의 S_base / S_fuzzy 값 갱신.</summary>
    public void UpdateUtilities(float[] sBaseValues, float[] sFuzzyValues)
    {
        for (int i = 0; i < _branches.Count; i++)
        {
            if (i < sBaseValues.Length)  _branches[i].SBase  = sBaseValues[i];
            if (i < sFuzzyValues.Length) _branches[i].SFuzzy = sFuzzyValues[i];
        }
    }

    public override NodeState Evaluate()
    {
        // 가장 높은 U_final 분기 선택
        Branch best = null;
        float  bestU = float.MinValue;

        foreach (var branch in _branches)
        {
            if (branch.UFinal > bestU)
            {
                bestU = branch.UFinal;
                best  = branch;
            }
        }

        if (best == null) return NodeState.Failure;

        // 분기 전환 시 OnExit / OnEnter 호출
        if (best != _activeBranch)
        {
            _activeBranch?.Node.OnExit();
            _activeBranch = best;
            _activeBranch.Node.OnEnter();
        }

        return _activeBranch.Node.Evaluate();
    }
}
