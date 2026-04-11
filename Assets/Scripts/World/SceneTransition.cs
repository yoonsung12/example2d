using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 맵 전환 트리거.
/// 플레이어가 진입하면 페이드 아웃 → 씬 로드 → 페이드 인(자동) 순으로 진행.
/// _targetSpawnId 는 대상 씬의 SpawnPoint._spawnId 와 일치해야 합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string _targetScene;
    [SerializeField] private string _targetSpawnId;

    private bool _isTransitioning;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTransitioning)        return;
        if (!other.CompareTag("Player")) return;

        StartCoroutine(DoTransition(other));
    }

    private IEnumerator DoTransition(Collider2D playerCol)
    {
        _isTransitioning = true;

        // 1. 검정으로 페이드 아웃
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeOut();

        // 2. 현재 상태 저장
        var cb = playerCol.GetComponent<CharacterBase>();
        float hp = cb != null ? cb.CurrentHealth : 0f;
        SaveManager.Instance?.Save(playerCol.transform.position, hp);
        SaveManager.Instance?.SetCurrentScene(_targetScene);

        // 3. 대상 스폰 포인트 ID 전달
        GameManager.PendingSpawnId = _targetSpawnId;

        // 4. 씬 로드 (완료 시 FadeController가 자동으로 페이드 인)
        SceneManager.LoadScene(_targetScene);
    }
}
