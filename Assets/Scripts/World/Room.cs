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

    // 현재 방문 중인 전투 레코드 (플레이어가 이 방에 있는 동안 유효)
    public PlayerBehaviorTracker.RoomBattleRecord CurrentRecord { get; private set; }

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

        // 4. 전투 기록 시작
        CurrentRecord = PlayerBehaviorTracker.Instance?
            .RecordRoomEntered(_roomId, _enemies.Count);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        // 퇴장 시 레코드 닫기 (저장은 이미 됨)
        CurrentRecord = null;
    }

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
        enemy.OnEnemyDied -= OnEnemyDied;

        if (CurrentRecord == null) return;
        PlayerBehaviorTracker.Instance?.RecordEnemyKilledInRoom(CurrentRecord);
    }
}
