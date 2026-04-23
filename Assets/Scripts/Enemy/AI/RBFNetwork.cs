using UnityEngine;

/// <summary>
/// Radial Basis Function Network.
/// FCMClusterer에서 주입된 3D 센터를 기준으로
/// 입력 벡터가 어느 클러스터에 가장 가까운지 인덱스로 반환합니다.
/// 인덱스 0=방어형(Chase/Attack) / 1=균형형(Evade/Recover) / 2=공격형(Counter)
/// </summary>
public class RBFNetwork
{
    private float[][] _centers;      // RBF 센터 (FCMClusterer에서 주입)
    private readonly float _sigma;   // 가우시안 폭 파라미터

    public RBFNetwork(float sigma = 0.3f)
    {
        _sigma = sigma; // 가우시안 폭 설정
        // FCM 갱신 전 사용할 기본 센터 (FCMClusterer 기본값과 동일)
        _centers = new float[][]
        {
            new[] { 0.2f, 0.3f, 0.2f }, // 인덱스 0: 방어형
            new[] { 0.5f, 0.5f, 0.5f }, // 인덱스 1: 균형형
            new[] { 0.8f, 0.8f, 0.5f }, // 인덱스 2: 공격형
        };
    }

    /// <summary>FCM 클러스터 중심 주입 (FCM 갱신 시 호출)</summary>
    public void SetCenters(float[][] centers) => _centers = centers;

    /// <summary>입력 벡터와 가장 가까운 클러스터 인덱스 반환 (0/1/2)</summary>
    public int Compute(float[] inputs)
    {
        int   bestIndex = 0;              // 가장 높은 활성화값의 클러스터 인덱스
        float bestPhi   = float.MinValue; // 현재까지 최대 활성화값

        for (int i = 0; i < _centers.Length; i++)
        {
            float phi = GaussianRBF(inputs, _centers[i]); // 가우시안 활성화값 계산
            if (phi <= bestPhi) continue;                   // 더 높은 값이 아니면 무시
            bestPhi   = phi;                                // 최대 활성화값 갱신
            bestIndex = i;                                  // 해당 클러스터 인덱스 저장
        }

        return bestIndex; // 가장 가까운 클러스터 인덱스 반환
    }

    // 가우시안 방사형 기저함수: 입력과 센터 간 거리를 [0,1] 활성화값으로 변환
    private float GaussianRBF(float[] x, float[] c)
    {
        float distSq = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            float d  = x[i] - c[i]; // 각 차원의 차이
            distSq  += d * d;        // 제곱합 누산
        }
        return Mathf.Exp(-distSq / (2f * _sigma * _sigma)); // 가우시안 함수 적용
    }
}
