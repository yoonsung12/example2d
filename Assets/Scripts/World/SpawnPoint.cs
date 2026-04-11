using UnityEngine;

/// <summary>
/// 씬 진입 시 플레이어 배치 위치.
/// SceneTransition._targetSpawnId 와 _spawnId 가 일치하면 플레이어를 이 위치로 이동시킵니다.
/// 씬마다 최소 1개 이상 배치하고, 기본 진입 지점의 _spawnId 를 "default" 로 설정하세요.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string _spawnId = "default";

    private void Start()
    {
        string pending = GameManager.PendingSpawnId;

        // 빈 스폰 ID → "default" 폴백
        if (string.IsNullOrEmpty(pending)) pending = "default";

        if (_spawnId != pending) return;

        GameManager.PendingSpawnId = null;

        // 플레이어 이동
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning($"[SpawnPoint] PlayerController를 찾을 수 없습니다. 씬에 Player가 있는지 확인하세요.");
            return;
        }

        // Rigidbody2D가 있으면 rb.position으로 이동 (transform.position은 물리 시스템이 덮어씀)
        var rb = pc.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position       = transform.position;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            pc.transform.position = transform.position;
        }

        Debug.Log($"[SpawnPoint] 플레이어를 '{_spawnId}' 위치({transform.position})로 이동했습니다.");
        GameManager.Instance?.FindPlayer();
    }

    // 에디터에서 위치 확인용 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
    }
}
