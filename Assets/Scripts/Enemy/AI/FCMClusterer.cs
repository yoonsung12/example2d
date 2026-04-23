using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D 피처 공간 [attack_frequency, hit_rate, damage_per_sec]에 대한
/// Fuzzy C-Means 클러스터링. 클러스터 중심을 RBFNetwork의 센터로 제공합니다.
/// 중심은 공격성(attackFreq + hitRate) 기준 오름차순 정렬되어
/// 인덱스 0=방어형 / 1=균형형 / 2=공격형으로 일관성을 유지합니다.
/// </summary>
public class FCMClusterer
{
    public const int K          = 3; // 클러스터 수 (방어형 / 균형형 / 공격형)
    public const int Dimensions = 3; // 피처 차원 수 (attack_freq, hit_rate, damage_per_sec)

    private const int   MaxIterations = 30;   // FCM 최대 반복 횟수
    private const float FuzzinessM    = 2f;   // 퍼지화 계수 m
    private const float Epsilon       = 1e-4f; // 수렴 판정 임계값

    private readonly List<float[]> _samples = new(); // 피처 벡터 샘플 저장소

    // 초기 클러스터 중심: FCM 갱신 전 사용하는 기본 아키타입 값
    // 정렬 순서: [방어형(0), 균형형(1), 공격형(2)]
    private float[][] _centers = new float[][]
    {
        new[] { 0.2f, 0.3f, 0.2f }, // 방어형: 공격 빈도·명중률·피해 모두 낮음
        new[] { 0.5f, 0.5f, 0.5f }, // 균형형: 중간값
        new[] { 0.8f, 0.8f, 0.5f }, // 공격형: 공격 빈도·명중률 높음
    };

    /// <summary>3D 피처 샘플 추가 (최대 500개 유지)</summary>
    public void AddSample(float[] sample)
    {
        if (sample.Length != Dimensions) return;        // 차원 불일치 무시
        if (_samples.Count >= 500) _samples.RemoveAt(0); // 오래된 샘플 제거
        _samples.Add(sample);                             // 새 샘플 추가
    }

    /// <summary>
    /// FCM 실행 후 정렬된 클러스터 중심 반환.
    /// 샘플 K개 미만 시 기본 센터 반환.
    /// </summary>
    public float[][] GetCenters()
    {
        if (_samples.Count >= K)                          // 최소 K개 샘플이 있어야 FCM 실행
            _centers = SortByAggression(RunFCM());        // FCM 실행 후 공격성 기준 정렬
        return _centers;
    }

    /// <summary>샘플 초기화 (FCM 갱신 후 호출)</summary>
    public void ClearSamples() => _samples.Clear();

    // ── FCM 알고리즘 ─────────────────────────────────────────────────────────

    private float[][] RunFCM()
    {
        int n = _samples.Count; // 샘플 수

        // 멤버십 행렬 초기화 (각 행의 합 = 1)
        float[,] u = new float[n, K];
        for (int i = 0; i < n; i++)
        {
            float rowSum = 0f;
            var   row    = new float[K];
            for (int j = 0; j < K; j++) row[j]   = Random.value + 0.1f; // 랜덤 초기화
            for (int j = 0; j < K; j++) rowSum   += row[j];              // 합산
            for (int j = 0; j < K; j++) u[i, j]  = row[j] / rowSum;     // 정규화 (합=1)
        }

        // 클러스터 센터 배열 초기화
        var centers = new float[K][];
        for (int j = 0; j < K; j++) centers[j] = new float[Dimensions];

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            // 센터 갱신: 멤버십 가중 평균
            for (int j = 0; j < K; j++)
            {
                float   den = 0f;                         // 가중치 합산값
                float[] num = new float[Dimensions];       // 가중 합산 벡터

                for (int i = 0; i < n; i++)
                {
                    float um = Mathf.Pow(u[i, j], FuzzinessM); // 퍼지 가중치 u^m
                    den += um;                                    // 분모 누산
                    for (int d = 0; d < Dimensions; d++)
                        num[d] += um * _samples[i][d];           // 분자 누산
                }

                if (den > 1e-8f)                                 // 0 나누기 방지
                    for (int d = 0; d < Dimensions; d++)
                        centers[j][d] = num[d] / den;            // 가중 평균 센터 갱신
            }

            // 멤버십 갱신: 거리 비율 역수
            float maxDelta = 0f;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < K; j++)
                {
                    float sum = 0f;
                    for (int l = 0; l < K; l++)
                    {
                        float dj = Dist(_samples[i], centers[j]) + 1e-8f; // 샘플~j 거리
                        float dl = Dist(_samples[i], centers[l]) + 1e-8f; // 샘플~l 거리
                        sum += Mathf.Pow(dj / dl, 2f / (FuzzinessM - 1f)); // 거리 비율의 멱함수
                    }
                    float newU  = 1f / (sum + 1e-8f);                        // 역수 = 새 멤버십
                    maxDelta    = Mathf.Max(maxDelta, Mathf.Abs(newU - u[i, j])); // 최대 변화량 추적
                    u[i, j]     = newU;                                       // 멤버십 갱신
                }
            }

            if (maxDelta < Epsilon) break; // 수렴 시 조기 종료
        }

        return centers;
    }

    /// <summary>
    /// 공격성 점수 (attackFreq + hitRate) 기준 오름차순 정렬.
    /// 항상 인덱스 0=방어형, 1=균형형, 2=공격형 순서를 보장합니다.
    /// </summary>
    private static float[][] SortByAggression(float[][] centers)
    {
        // attackFreq(차원0) + hitRate(차원1) = 공격성 점수로 정렬
        System.Array.Sort(centers, (a, b) => (a[0] + a[1]).CompareTo(b[0] + b[1]));
        return centers;
    }

    // 유클리드 거리 계산
    private static float Dist(float[] a, float[] b)
    {
        float s = 0f;
        for (int d = 0; d < Dimensions; d++)
        {
            float diff = a[d] - b[d]; // 각 차원 차이
            s += diff * diff;          // 제곱합
        }
        return Mathf.Sqrt(s); // 유클리드 거리
    }
}
