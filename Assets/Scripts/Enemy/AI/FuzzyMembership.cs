using UnityEngine;

/// <summary>
/// 퍼지 멤버십 함수 모음. 모든 함수는 [0, 1] 범위를 반환합니다.
/// </summary>
public static class FuzzyMembership
{
    /// <summary>사다리꼴: a→b 상승, b~c 고원, c→d 하강</summary>
    public static float Trapezoid(float x, float a, float b, float c, float d)
    {
        if (x <= a || x >= d) return 0f;
        if (x >= b && x <= c) return 1f;
        if (x < b)            return (x - a) / (b - a);
        return (d - x) / (d - c);
    }

    /// <summary>삼각형: a에서 0→1, b에서 1, c에서 1→0</summary>
    public static float Triangle(float x, float a, float b, float c)
    {
        if (x <= a || x >= c) return 0f;
        if (x <= b)           return (x - a) / (b - a);
        return (c - x) / (c - b);
    }

    /// <summary>어깨형 Low: x ≤ a → 1, x ≥ b → 0, 선형 하강</summary>
    public static float ShoulderLow(float x, float a, float b)
    {
        if (x <= a) return 1f;
        if (x >= b) return 0f;
        return (b - x) / (b - a);
    }

    /// <summary>어깨형 High: x ≤ a → 0, x ≥ b → 1, 선형 상승</summary>
    public static float ShoulderHigh(float x, float a, float b)
    {
        if (x <= a) return 0f;
        if (x >= b) return 1f;
        return (x - a) / (b - a);
    }
}
