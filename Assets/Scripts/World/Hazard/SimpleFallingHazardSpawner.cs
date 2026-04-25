using System.Collections;
using UnityEngine;

/// <summary>
/// 꽃가루(봄)·눈(겨울) 연속 생성기.
/// FallingParticle 프리팹을 일정 간격으로 화면 상단에서 무한히 생성한다.
/// </summary>
public class SimpleFallingHazardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _particlePrefab;  // 생성할 입자 프리팹 (FallingParticle 컴포넌트 포함)
    [SerializeField] private float _spawnInterval = 0.3f; // 입자 생성 간격 (초)
    [SerializeField] private float _spawnWidth    = 10f;  // 생성 범위 가로 폭 — 스포너 X를 중심으로 좌우 절반씩
    [SerializeField] private float _spawnHeight   = 10f;  // 스포너 Y 기준 생성 높이 오프셋

    private void Start()
    {
        StartCoroutine(SpawnLoop());  // 씬 시작 시 무한 생성 루프 시작
    }

    private IEnumerator SpawnLoop()
    {
        while (true)  // 씬이 살아있는 동안 무한 반복
        {
            SpawnParticle();                                    // 입자 하나 생성
            yield return new WaitForSeconds(_spawnInterval);   // 다음 생성까지 대기
        }
    }

    private void SpawnParticle()
    {
        if (_particlePrefab == null) return;  // 프리팹 미설정 시 무시

        // 가로는 스포너 X 기준 랜덤, 세로는 스포너 Y + 높이 오프셋
        float x        = transform.position.x + Random.Range(-_spawnWidth * 0.5f, _spawnWidth * 0.5f);
        var   spawnPos = new Vector3(x, transform.position.y + _spawnHeight, 0f);

        Instantiate(_particlePrefab, spawnPos, Quaternion.identity);  // 입자 생성
    }
}
