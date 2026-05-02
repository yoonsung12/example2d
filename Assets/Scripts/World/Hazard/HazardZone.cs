using System.Collections;
using UnityEngine;

/// <summary>
/// 지정된 공간에서 하나의 위험 요소 타입을 내리는 통합 스포너.
/// 인스펙터에서 타입과 파라미터를 설정하면 해당 위험 요소만 생성된다.
/// </summary>
public class HazardZone : MonoBehaviour
{
    // 내릴 수 있는 위험 요소 종류
    public enum HazardType { Pollen, Rain, GinkgoNut, Snow }

    [Header("Zone")]
    [SerializeField] private HazardType _hazardType;         // 내릴 위험 요소 종류 선택
    [SerializeField] private float _zoneWidth         = 10f; // 생성 범위 가로 폭 (스포너 X 중심 기준)
    [SerializeField] private float _spawnHeightOffset = 10f; // 생성 위치 Y 오프셋 (스포너 Y 기준 위로)

    [Header("Prefabs")]
    [SerializeField] private GameObject _pollenPrefab;    // 꽃가루 프리팹
    [SerializeField] private GameObject _rainPrefab;      // 빗방울 프리팹
    [SerializeField] private GameObject _ginkgoNutPrefab; // 은행 프리팹
    [SerializeField] private GameObject _snowPrefab;      // 눈 프리팹

    [Header("Pollen / Snow Settings")]
    [SerializeField] private float _particleInterval = 0.3f; // 꽃가루·눈 생성 간격 (초)

    [Header("Rain Settings")]
    [SerializeField] private float _rainSpawnInterval = 0.08f; // 빗방울 생성 간격 (초) — 낮을수록 폭우
    [SerializeField] private float _rainOnDuration    = 5f;    // 비가 내리는 지속 시간 (초)
    [SerializeField] private float _rainOffDuration   = 3f;    // 비가 멈추는 대기 시간 (초)

    [Header("GinkgoNut Settings")]
    [SerializeField] private float _ginkgoMinInterval = 1.5f; // 은행 생성 최소 간격 (초)
    [SerializeField] private float _ginkgoMaxInterval = 4f;   // 은행 생성 최대 간격 (초)

    private Coroutine _spawnRoutine; // 실행 중인 스폰 코루틴 — 중단 시 참조용

    private void Start()
    {
        StartSpawning(); // 씬 시작 시 자동으로 스폰 시작
    }

    /// <summary>스폰을 시작한다. 외부에서 호출해 동적으로 켤 수 있다.</summary>
    public void StartSpawning()
    {
        StopSpawning(); // 중복 실행 방지 — 기존 코루틴 먼저 정리

        // 선택된 타입에 맞는 코루틴 시작
        _spawnRoutine = _hazardType switch
        {
            HazardType.Pollen    => StartCoroutine(ContinuousSpawnRoutine(_pollenPrefab, _particleInterval)),
            HazardType.Rain      => StartCoroutine(RainSpawnRoutine()),
            HazardType.GinkgoNut => StartCoroutine(GinkgoSpawnRoutine()),
            HazardType.Snow      => StartCoroutine(ContinuousSpawnRoutine(_snowPrefab, _particleInterval)),
            _                    => null
        };
    }

    /// <summary>스폰을 중단한다. 외부에서 호출해 동적으로 끌 수 있다.</summary>
    public void StopSpawning()
    {
        if (_spawnRoutine == null) return; // 실행 중인 코루틴이 없으면 무시
        StopCoroutine(_spawnRoutine);      // 코루틴 중단
        _spawnRoutine = null;              // 참조 초기화
    }

    /// <summary>꽃가루·눈 — 일정 간격으로 무한 생성</summary>
    private IEnumerator ContinuousSpawnRoutine(GameObject prefab, float interval)
    {
        if (prefab == null) yield break;                 // 프리팹 미설정 시 즉시 종료
        var wait = new WaitForSeconds(interval);         // WaitForSeconds 재사용으로 GC 최소화
        while (true)
        {
            Spawn(prefab);     // 입자 하나 생성
            yield return wait; // 다음 생성까지 대기
        }
    }

    /// <summary>비 — rainOnDuration 동안 빗방울 생성 → rainOffDuration 대기 → 무한 반복</summary>
    private IEnumerator RainSpawnRoutine()
    {
        if (_rainPrefab == null) yield break;                    // 프리팹 미설정 시 즉시 종료
        var spawnWait = new WaitForSeconds(_rainSpawnInterval);  // 빗방울 생성 대기 재사용
        var offWait   = new WaitForSeconds(_rainOffDuration);    // 멈춤 대기 재사용
        while (true)
        {
            float elapsed = 0f;                    // 비 내리는 경과 시간 초기화
            while (elapsed < _rainOnDuration)      // rainOnDuration이 될 때까지 빗방울 생성
            {
                Spawn(_rainPrefab);                // 빗방울 하나 생성
                yield return spawnWait;            // 다음 빗방울까지 대기
                elapsed += _rainSpawnInterval;     // 경과 시간 누적
            }
            yield return offWait;                  // 비 멈춤 대기
        }
    }

    /// <summary>은행 — 최소·최대 사이 랜덤 간격으로 하나씩 생성</summary>
    private IEnumerator GinkgoSpawnRoutine()
    {
        if (_ginkgoNutPrefab == null) yield break; // 프리팹 미설정 시 즉시 종료
        while (true)
        {
            // 최소·최대 간격 사이 랜덤 대기 — 불규칙한 낙하 연출
            yield return new WaitForSeconds(Random.Range(_ginkgoMinInterval, _ginkgoMaxInterval));
            Spawn(_ginkgoNutPrefab); // 은행 하나 생성
        }
    }

    /// <summary>존 안 랜덤 위치에 프리팹 하나 생성</summary>
    private void Spawn(GameObject prefab)
    {
        // 가로: 존 중심 X ± zoneWidth/2 랜덤 위치
        float x = transform.position.x + Random.Range(-_zoneWidth * 0.5f, _zoneWidth * 0.5f);
        // 세로: 존 Y + 높이 오프셋
        float y = transform.position.y + _spawnHeightOffset;
        Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity); // 오브젝트 생성
    }

    /// <summary>Scene 뷰에서 생성 범위를 노란 박스로 시각화 — 공간 지정 확인용</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // 노란색으로 구분
        // 존 중심에서 높이 오프셋 절반 위를 중심으로 와이어프레임 박스 표시
        Gizmos.DrawWireCube(
            transform.position + Vector3.up * (_spawnHeightOffset * 0.5f),
            new Vector3(_zoneWidth, _spawnHeightOffset, 0f)
        );
    }
}
