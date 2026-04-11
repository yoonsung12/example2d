using UnityEngine;

/// <summary>
/// Radial Basis Function Network.
/// 4차원 플레이어 행동 벡터를 입력받아 PlayStyle_Score(0=회피형 / 1=공격형)를 출력합니다.
/// 센터는 플레이어 아키타입 3개로 사전 정의되어 있습니다.
/// </summary>
public class RBFNetwork
{
    // 사전 정의된 RBF 센터 (3가지 플레이어 아키타입)
    // 입력 순서: [CombatEngagementRate, EnemyClearRate, SkipRate, HPThresholdRetreat]
    private static readonly float[][] DefaultCenters =
    {
        new[] { 0.9f, 0.85f, 0.1f,  0.1f  },  // 공격형 아키타입
        new[] { 0.5f, 0.5f,  0.3f,  0.35f },  // 균형형 아키타입
        new[] { 0.1f, 0.2f,  0.85f, 0.8f  },  // 회피형 아키타입
    };

    // 각 센터의 출력 가중치 (공격형=1.0, 균형형=0.5, 회피형=0.0)
    private static readonly float[] DefaultWeights = { 1.0f, 0.5f, 0.0f };

    private readonly float[][] _centers;
    private readonly float[]   _weights;
    private readonly float     _sigma;

    public RBFNetwork(float sigma = 0.4f)
    {
        _centers = DefaultCenters;
        _weights = DefaultWeights;
        _sigma   = sigma;
    }

    /// <summary>
    /// PlayStyle_Score 계산. 결과: 0.0 = 회피형, 1.0 = 공격형.
    /// </summary>
    public float Compute(float[] inputs)
    {
        float sumPhi      = 0f;
        float weightedSum = 0f;

        for (int i = 0; i < _centers.Length; i++)
        {
            float phi  = GaussianRBF(inputs, _centers[i]);
            weightedSum += _weights[i] * phi;
            sumPhi      += phi;
        }

        // 정규화 후 클램프
        return sumPhi < 1e-6f ? 0.5f : Mathf.Clamp01(weightedSum / sumPhi);
    }

    private float GaussianRBF(float[] x, float[] c)
    {
        float distSq = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            float d = x[i] - c[i];
            distSq += d * d;
        }
        return Mathf.Exp(-distSq / (2f * _sigma * _sigma));
    }
}
