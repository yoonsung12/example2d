using UnityEngine;

/// <summary>선풍기 바람에 반응하는 오브젝트(꽃가루·구슬·열기구 등)가 구현하는 인터페이스</summary>
public interface IWindAffectable
{
    /// <summary>바람 방향과 세기를 받아 반응</summary>
    void OnWind(Vector2 windDirection, float force);
}
