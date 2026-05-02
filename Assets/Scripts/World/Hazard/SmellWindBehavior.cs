using System.Collections;
using UnityEngine;

/// <summary>
/// 은행 냄새(GinkgoNut 버스트 후)가 바람을 연속으로 맞으면 소멸하는 컴포넌트.
/// GinkgoNut.Burst()에서 Activate()를 호출해야 바람 반응이 시작된다.
/// 바람이 끊기면 누적 시간이 리셋된다.
/// </summary>
public class SmellWindBehavior : MonoBehaviour, IWindAffectable
{
    [Header("Wind Despawn")]
    [SerializeField] private float _windDespawnDuration = 2f; // 연속으로 바람을 맞아야 하는 시간 (초)

    // FanTool의 _windInterval(0.05s)보다 충분히 크게 설정 — 인터벌 사이 프레임에서 끊김 오탐 방지
    private const float WindTimeoutThreshold = 0.15f;

    private bool      _isActive;             // Activate() 호출 후에만 바람에 반응
    private float     _windAccumTime;        // 현재 세션의 바람 누적 시간
    private float     _timeSinceLastWind;    // 마지막 OnWind 이후 경과 시간
    private Coroutine _naturalDestroyRoutine; // 자연 소멸 코루틴 — 바람 소멸 시 취소용

    /// <summary>
    /// GinkgoNut.Burst()에서 호출.
    /// 자연 소멸 타이머를 시작하고 바람 반응을 활성화한다.
    /// </summary>
    public void Activate(float naturalDuration)
    {
        _isActive            = true;
        _timeSinceLastWind   = float.MaxValue; // 처음엔 바람 없음 상태로 초기화
        _naturalDestroyRoutine = StartCoroutine(NaturalDestroyRoutine(naturalDuration));
    }

    private void Update()
    {
        if (!_isActive) return;

        _timeSinceLastWind += Time.deltaTime; // 마지막 바람 이후 경과 시간 누적

        bool isBeingBlown = _timeSinceLastWind <= WindTimeoutThreshold; // 아직 바람 영향권 내

        if (isBeingBlown)
        {
            _windAccumTime += Time.deltaTime; // 바람 누적 시간 증가

            if (_windAccumTime >= _windDespawnDuration)
            {
                // 자연 소멸 취소 후 즉시 제거
                if (_naturalDestroyRoutine != null)
                    StopCoroutine(_naturalDestroyRoutine);
                Destroy(gameObject);
            }
        }
        else
        {
            _windAccumTime = 0f; // 바람 끊기면 누적 시간 리셋
        }
    }

    /// <summary>FanTool이 바람 방향과 세기를 전달 — 타임아웃 타이머를 리셋해 연속 노출로 인식</summary>
    public void OnWind(Vector2 windDirection, float force)
    {
        if (!_isActive) return;
        _timeSinceLastWind = 0f; // 바람 받으면 타이머 초기화
    }

    private IEnumerator NaturalDestroyRoutine(float duration)
    {
        yield return new WaitForSeconds(duration); // naturalDuration 초 대기
        Destroy(gameObject);                       // 바람 소멸 없이 시간 만료 시 제거
    }
}
