using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 우산 도구 (S키).
/// 비를 차단하는 콜라이더를 활성화하고 사용 중 캐릭터 주변 조명을 약화시킨다.
/// </summary>
public class UmbrellaTool : ToolBase
{
    [Header("Rain Block")]
    [SerializeField] private Collider2D _rainBlocker; // 비 충돌 차단용 콜라이더

    [Header("Darkness")]
    [SerializeField] private Light2D _playerLight;          // 플레이어 부착 2D 조명
    [SerializeField] private float   _openLightIntensity  = 0.3f; // 우산 사용 중 조명 강도
    [SerializeField] private float   _closeLightIntensity = 1.0f; // 기본 조명 강도

    protected override void Awake()
    {
        base.Awake();
        if (_rainBlocker != null) _rainBlocker.enabled = false; // 초기 비활성
    }

    /// <summary>우산 펼치기 — 비 차단 콜라이더 활성 + 조명 감소</summary>
    protected override void OnUse(Vector2 direction)
    {
        if (_rainBlocker != null) _rainBlocker.enabled   = true;
        if (_playerLight != null) _playerLight.intensity = _openLightIntensity;
    }

    /// <summary>우산 접기 — 차단 비활성 + 조명 복원</summary>
    protected override void OnStopUse()
    {
        if (_rainBlocker != null) _rainBlocker.enabled   = false;
        if (_playerLight != null) _playerLight.intensity = _closeLightIntensity;
    }
}
