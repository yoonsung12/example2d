using System.Collections;
using UnityEngine;

/// <summary>
/// 산성비 위험 요소.
/// 비 ON → 빗방울 생성 → 비 OFF → 대기를 무한 반복.
/// 빗방울(FallingParticle)이 우산에 닿으면 빗소리 재생, 플레이어에 닿으면 여름 게이지 추가.
/// </summary>
public class AcidRainHazard : MonoBehaviour
{
    [SerializeField] private GameObject _rainParticlePrefab; // 빗방울 프리팹 (FallingParticle, Summer 설정)
    [SerializeField] private float _rainDuration  = 5f;      // 비가 내리는 시간 (초)
    [SerializeField] private float _pauseDuration = 3f;      // 비가 멈추는 시간 (초)
    [SerializeField] private float _spawnInterval = 0.08f;   // 빗방울 생성 간격 (초) — 낮을수록 폭우
    [SerializeField] private float _spawnWidth    = 12f;     // 생성 범위 가로 폭
    [SerializeField] private float _spawnHeight   = 10f;     // 스포너 Y 기준 생성 높이 오프셋

    public bool IsRaining { get; private set; }  // 현재 비 상태 — UI 등 외부 참조용

    private void Start()
    {
        StartCoroutine(RainCycle());  // 비 주기 코루틴 시작
    }

    private IEnumerator RainCycle()
    {
        while (true)  // 씬이 살아있는 동안 무한 반복
        {
            IsRaining = true;                              // 비 시작 상태로 전환
            yield return StartCoroutine(SpawnRain());     // rainDuration 동안 빗방울 생성
            IsRaining = false;                             // 비 멈춤 상태로 전환
            yield return new WaitForSeconds(_pauseDuration);  // pauseDuration 동안 대기
        }
    }

    private IEnumerator SpawnRain()
    {
        float elapsed = 0f;  // rainDuration 내 경과 시간 추적

        while (elapsed < _rainDuration)  // rainDuration이 될 때까지 빗방울 생성
        {
            SpawnDrop();                                        // 빗방울 하나 생성
            yield return new WaitForSeconds(_spawnInterval);   // 다음 빗방울까지 대기
            elapsed += _spawnInterval;                         // 경과 시간 누적
        }
    }

    private void SpawnDrop()
    {
        if (_rainParticlePrefab == null) return;  // 프리팹 미설정 시 무시

        // 가로는 스포너 X 기준 랜덤, 세로는 스포너 Y + 높이 오프셋
        float x        = transform.position.x + Random.Range(-_spawnWidth * 0.5f, _spawnWidth * 0.5f);
        var   spawnPos = new Vector3(x, transform.position.y + _spawnHeight, 0f);

        Instantiate(_rainParticlePrefab, spawnPos, Quaternion.identity);  // 빗방울 생성
    }
}
