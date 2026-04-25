using System.Collections;
using UnityEngine;

/// <summary>
/// 은행 산발 생성기.
/// 랜덤 간격으로 GinkgoNut 프리팹을 화면 상단에서 하나씩 떨어뜨린다.
/// </summary>
public class GinkgoSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _ginkgoNutPrefab; // 은행 프리팹 (GinkgoNut 컴포넌트 포함)
    [SerializeField] private float _minInterval = 1.5f;   // 은행 생성 최소 간격 (초)
    [SerializeField] private float _maxInterval = 4f;     // 은행 생성 최대 간격 (초)
    [SerializeField] private float _spawnWidth  = 10f;    // 생성 범위 가로 폭
    [SerializeField] private float _spawnHeight = 8f;     // 스포너 Y 기준 생성 높이 오프셋

    private void Start()
    {
        StartCoroutine(SpawnLoop());  // 씬 시작 시 생성 루프 시작
    }

    private IEnumerator SpawnLoop()
    {
        while (true)  // 씬이 살아있는 동안 무한 반복
        {
            // 랜덤 간격 대기 — 은행이 불규칙하게 하나씩 떨어지는 효과
            float interval = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(interval);

            SpawnGinkgoNut();  // 은행 하나 생성
        }
    }

    private void SpawnGinkgoNut()
    {
        if (_ginkgoNutPrefab == null) return;  // 프리팹 미설정 시 무시

        // 가로는 스포너 X 기준 랜덤, 세로는 스포너 Y + 높이 오프셋
        float x        = transform.position.x + Random.Range(-_spawnWidth * 0.5f, _spawnWidth * 0.5f);
        var   spawnPos = new Vector3(x, transform.position.y + _spawnHeight, 0f);

        Instantiate(_ginkgoNutPrefab, spawnPos, Quaternion.identity);  // 은행 생성
    }
}
