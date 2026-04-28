using UnityEngine;

/// <summary>
/// Radial Basis Function Network.
/// 오프라인 FCM 분석으로 확정된 3D 센터를 기준으로
/// 입력 벡터가 어느 클러스터에 가장 가까운지 인덱스로 반환합니다.
/// 인덱스 0=소극형(Chase/Attack) / 1=중간형(Evade/Recover) / 2=공격형(Counter)
/// 센터: 공격성(AttackFreq + HitRate) 기준 오름차순 정렬
/// </summary>
public class RBFNetwork
{
    private readonly float[][] _centers; // FCM 오프라인 분석으로 확정된 클러스터 센터
    private readonly float     _sigma;   // 가우시안 폭 파라미터

    public RBFNetwork(float sigma = 0.3f)
    {
        _sigma = sigma; // 가우시안 폭 설정
        // 오프라인 FCM 분석 결과 (2026-04-27, FPC=0.9002, 샘플 814개)
        // 정렬 기준: attackFreq + hitRate 오름차순
        _centers = new float[][]
        {
            new[] { 0.03819f, 0.0048f,  0.01966f }, // 인덱스 0: 소극형  → Chase/Attack
            new[] { 0.04337f, 0.3120f,  0.01063f }, // 인덱스 1: 중간형  → Evade/Recover
            new[] { 0.06425f, 0.9391f,  0.02138f }, // 인덱스 2: 공격형  → Counter
        };
    }

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
