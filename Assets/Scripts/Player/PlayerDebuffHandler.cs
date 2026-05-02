using UnityEngine;

/// <summary>
/// SeasonalGauge의 디버프 이벤트를 받아 PlatformerMovement의 플래그를 적용/해제한다.
/// Player GameObject에 추가해서 사용한다.
/// </summary>
[RequireComponent(typeof(PlatformerMovement))]
public class PlayerDebuffHandler : MonoBehaviour
{
    [Header("Summer Slow")]
    [SerializeField] private float _slowMultiplier = 0.4f; // 여름 슬로우 이동속도 배율 (인스펙터에서 조절)

    private PlatformerMovement _movement; // 디버프 플래그를 적용할 이동 컴포넌트

    private void Awake()
    {
        _movement = GetComponent<PlatformerMovement>(); // PlatformerMovement 캐시
    }

    private void OnEnable()
    {
        if (SeasonalGauge.Instance == null) return;
        SeasonalGauge.Instance.OnDebuffTriggered += ApplyDebuff;  // 디버프 발동 이벤트 구독
        SeasonalGauge.Instance.OnDebuffEnded     += ClearDebuff;  // 디버프 해제 이벤트 구독
    }

    private void OnDisable()
    {
        if (SeasonalGauge.Instance == null) return;
        SeasonalGauge.Instance.OnDebuffTriggered -= ApplyDebuff;  // 구독 해제
        SeasonalGauge.Instance.OnDebuffEnded     -= ClearDebuff;  // 구독 해제
    }

    /// <summary>발동된 계절에 맞는 디버프를 플레이어에게 적용한다</summary>
    private void ApplyDebuff(SeasonType season)
    {
        switch (season)
        {
            case SeasonType.Spring:
                _movement.IsBound = true;           // 봄 속박: 이동/점프/대시 차단, 공격·도구는 허용
                break;

            case SeasonType.Summer:
                _movement.SpeedMultiplier = _slowMultiplier; // 여름 슬로우: 이동속도 감소
                break;

            case SeasonType.Autumn:
                _movement.IsConfused = true;        // 가을 혼란: 좌우 방향 반전
                break;

            case SeasonType.Winter:
                _movement.IsFrozen = true;          // 겨울 빙결: 모든 행동 차단
                break;
        }
    }

    /// <summary>디버프 해제 시 모든 플래그를 초기 상태로 되돌린다</summary>
    private void ClearDebuff()
    {
        _movement.IsBound         = false; // 속박 해제
        _movement.IsConfused      = false; // 혼란 해제
        _movement.IsFrozen        = false; // 빙결 해제
        _movement.SpeedMultiplier = 1f;    // 이동속도 정상화
    }
}
