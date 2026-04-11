using UnityEngine;

/// <summary>
/// 퍼지 규칙 R-01 ~ R-04를 평가하여 각 BT 분기의 S_fuzzy를 계산합니다.
/// FCMClusterer에서 자동 갱신된 임계값을 사용합니다.
/// </summary>
public class FuzzyRuleEngine
{
    private readonly FCMClusterer _fcm;

    public FuzzyRuleEngine(FCMClusterer fcm)
    {
        _fcm = fcm;
    }

    public struct FuzzyResult
    {
        public float UtilityChase;  // Branch A/C: 추격
        public float UtilityEvade;  // Branch B  : 회피
        public float UtilityRush;   // Branch A  : 돌격
    }

    /// <summary>
    /// 퍼지 규칙 평가 후 각 분기의 S_fuzzy 반환.
    /// </summary>
    /// <param name="playStyleScore">RBFN 출력 [0,1] (0=회피형, 1=공격형)</param>
    /// <param name="playerHP">플레이어 HP [0, 100]</param>
    /// <param name="distToPlayer">플레이어까지 거리 (유닛)</param>
    public FuzzyResult Evaluate(float playStyleScore, float playerHP, float distToPlayer)
    {
        // ── PlayStyle_Score 퍼지화 (임계값 확대 → 중립 플레이어도 반응) ────
        float psLow  = FuzzyMembership.ShoulderLow(playStyleScore,  0.25f, 0.55f);
        float psHigh = FuzzyMembership.ShoulderHigh(playStyleScore, 0.45f, 0.7f);

        // ── Player_HP 퍼지화 (FCM 임계값 사용, 0~100 정규화) ────────────────
        float hpLow  = FuzzyMembership.ShoulderLow(
            playerHP, _fcm.HPLow - 10f, _fcm.HPLow + 10f);
        float hpHigh = FuzzyMembership.ShoulderHigh(
            playerHP, _fcm.HPHigh - 10f, _fcm.HPHigh + 10f);

        // ── Distance_to_Player 퍼지화 (FCM 임계값 사용) ─────────────────────
        float distFar = FuzzyMembership.ShoulderHigh(
            distToPlayer, _fcm.DistNear, _fcm.DistFar);

        // ── 규칙 평가 (MIN 연산 = AND) ───────────────────────────────────────
        // R-01: PlayStyle High AND Player_HP High → Evade (회피)
        float r01 = Mathf.Min(psHigh, hpHigh);
        // R-02: PlayStyle High AND Player_HP Low  → Rush  (돌격)
        float r02 = Mathf.Min(psHigh, hpLow);
        // R-03: PlayStyle Low  AND Distance Far   → Chase (추격)
        float r03 = Mathf.Min(psLow,  distFar);
        // R-04: PlayStyle Low  AND Player_HP Low  → Chase (추격)
        float r04 = Mathf.Min(psLow,  hpLow);

        // ── 집계 (MAX 연산 = OR) ─────────────────────────────────────────────
        return new FuzzyResult
        {
            UtilityEvade = r01,
            UtilityRush  = r02,
            UtilityChase = Mathf.Max(r03, r04),
        };
    }
}
