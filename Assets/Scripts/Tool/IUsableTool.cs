using UnityEngine;

/// <summary>플레이어가 사용할 수 있는 도구의 공통 인터페이스</summary>
public interface IUsableTool
{
    ToolType ToolType { get; }

    /// <summary>도구 사용 중 여부</summary>
    bool IsActive { get; }

    /// <summary>도구 사용 시작. direction = 바람/빛 발사 방향</summary>
    void Use(Vector2 direction);

    /// <summary>도구 사용 종료</summary>
    void StopUse();
}
