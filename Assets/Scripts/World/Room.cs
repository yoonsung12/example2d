using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룸 영역 트리거.
/// 플레이어 진입 시 카메라 바운드 갱신 + 방별 전투 통계 수집 시작.
/// 룸 콜라이더 안에 있는 EnemyBase를 자동 탐색해 사망 이벤트를 구독합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Room : MonoBehaviour
{
    [SerializeField] private string     _roomId;
    [SerializeField] private Collider2D _cameraBounds;

    public string RoomId       => _roomId;
    public Collider2D CameraBounds => _cameraBounds;

    private readonly List<EnemyBase> _enemies = new();

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 1. 방문 저장
        SaveManager.Instance?.AddVisitedRoom(_roomId);

        // 2. 카메라 바운드 갱신
        if (_cameraBounds != null)
        {
            var camFollow = Camera.main?.GetComponent<CameraFollow>();
            camFollow?.SetRoomBounds(_cameraBounds.bounds);
        }

        // 3. 룸 안의 적 탐색
        DiscoverEnemies();

    }

    private void OnTriggerExit2D(Collider2D other) { } // 퇴장 이벤트 (현재 미사용)

    // ── 적 탐색 ────────────────────────────────────────────────────────────

    private void DiscoverEnemies()
    {
        // 이전 구독 해제
        foreach (var e in _enemies)
            if (e != null) e.OnEnemyDied -= OnEnemyDied;
        _enemies.Clear();

        // 룸 콜라이더와 겹치는 Enemy 레이어(9) 오브젝트 탐색
        var filter  = new ContactFilter2D();
        filter.SetLayerMask(1 << 9); // Enemy layer
        filter.useTriggers = false;

        var cols = new List<Collider2D>();
        GetComponent<Collider2D>().Overlap(filter, cols);

        foreach (var col in cols)
        {
            var enemy = col.GetComponent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;

            _enemies.Add(enemy);
            enemy.SetHomeRoom(this);
            enemy.OnEnemyDied += OnEnemyDied;
        }
    }

    // ── 적 사망 콜백 ──────────────────────────────────────────────────────

    private void OnEnemyDied(EnemyBase enemy)
    {
        enemy.OnEnemyDied -= OnEnemyDied; // 사망한 적의 이벤트 구독 해제
    }
}
