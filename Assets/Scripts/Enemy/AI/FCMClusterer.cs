using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fuzzy C-Means 클러스터링.
/// 실시간 데이터로 Player_HP / Distance_to_Player 의 퍼지 임계값을 자동 갱신합니다.
/// </summary>
public class FCMClusterer
{
    private const int   MaxIterations = 30;
    private const float FuzzinessM    = 2f;   // 퍼지화 계수 m (일반적으로 2)
    private const float Epsilon       = 1e-4f;

    // ── Player HP 임계값 (Low / Medium / High) ──────────────────────────────
    public float HPLow    { get; private set; } = 30f;
    public float HPMedium { get; private set; } = 55f;
    public float HPHigh   { get; private set; } = 80f;

    // ── Distance 임계값 (Near / Far) ────────────────────────────────────────
    public float DistNear { get; private set; } = 5f;
    public float DistFar  { get; private set; } = 12f;

    public void UpdateHPClusters(List<float> samples)
    {
        if (samples == null || samples.Count < 3) return;
        float[] centers = RunFCM(samples, 3);
        System.Array.Sort(centers);
        HPLow    = centers[0];
        HPMedium = centers[1];
        HPHigh   = centers[2];
    }

    public void UpdateDistanceClusters(List<float> samples)
    {
        if (samples == null || samples.Count < 2) return;
        float[] centers = RunFCM(samples, 2);
        System.Array.Sort(centers);
        DistNear = centers[0];
        DistFar  = centers[1];
    }

    // ── FCM 알고리즘 ────────────────────────────────────────────────────────

    private float[] RunFCM(List<float> data, int k)
    {
        int n = data.Count;

        // 멤버십 행렬 초기화 (랜덤, 행합 = 1)
        float[,] u = new float[n, k];
        for (int i = 0; i < n; i++)
        {
            float rowSum = 0f;
            float[] row  = new float[k];
            for (int j = 0; j < k; j++) row[j] = Random.value + 0.1f;
            for (int j = 0; j < k; j++) rowSum += row[j];
            for (int j = 0; j < k; j++) u[i, j] = row[j] / rowSum;
        }

        float[] centers = new float[k];

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            // 센터 갱신
            for (int j = 0; j < k; j++)
            {
                float num = 0f, den = 0f;
                for (int i = 0; i < n; i++)
                {
                    float um = Mathf.Pow(u[i, j], FuzzinessM);
                    num += um * data[i];
                    den += um;
                }
                centers[j] = den > 1e-8f ? num / den : centers[j];
            }

            // 멤버십 갱신
            float maxDelta = 0f;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < k; j++)
                {
                    float sum = 0f;
                    for (int l = 0; l < k; l++)
                    {
                        float dj = Mathf.Abs(data[i] - centers[j]) + 1e-8f;
                        float dl = Mathf.Abs(data[i] - centers[l]) + 1e-8f;
                        sum += Mathf.Pow(dj / dl, 2f / (FuzzinessM - 1f));
                    }
                    float newU = 1f / (sum + 1e-8f);
                    maxDelta   = Mathf.Max(maxDelta, Mathf.Abs(newU - u[i, j]));
                    u[i, j]    = newU;
                }
            }

            if (maxDelta < Epsilon) break;
        }

        return centers;
    }
}
