/// <summary>도구 종류 식별자</summary>
public enum ToolType
{
    Fan,       // 선풍기 (봄 맵 획득)
    Umbrella,  // 우산   (여름 맵 획득)
    Lighter,   // 라이터 (가을 맵 획득)
}

/// <summary>도구 조합 결과 종류</summary>
public enum ComboType
{
    AerialLift, // 선풍기 + 우산 → 공중 부양
    HeatWind,   // 선풍기 + 라이터 → 원거리 열풍
    Glide,      // 우산   + 라이터 → 활공
}
