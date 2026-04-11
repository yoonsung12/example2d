using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 플레이어의 전투 행동 통계를 방(Room) 단위로 수집·저장·로드합니다.
/// RBFN 입력 벡터(4차원)를 방문 기록 기반으로 계산합니다.
/// </summary>
public class PlayerBehaviorTracker : MonoBehaviour
{
    public static PlayerBehaviorTracker Instance { get; private set; }

    private const string FileName = "behavior.json";
    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    // ── 방별 전투 기록 ────────────────────────────────────────────────────────

    [Serializable]
    public class RoomBattleRecord
    {
        public string roomId;
        public int    totalEnemies;   // 진입 시 적 수
        public int    killedEnemies;  // 처치한 적 수
        public bool   engaged;        // 교전 발생 여부
        public bool   fullyCleared;   // 전멸 완료 여부
    }

    [Serializable]
    private class BehaviorData
    {
        // 방별 기록 (재방문 시 새 레코드 추가)
        public List<RoomBattleRecord> roomRecords = new();

        // 구버전 호환 — HPThresholdRetreat 계산에만 사용
        public int retreatsAtLowHP;
        public int lowHPMoments;
    }

    private BehaviorData _data = new();

    // ── 정규화된 RBFN 입력값 [0,1] ─────────────────────────────────────────

    /// <summary>적이 있는 방 중 교전이 발생한 비율.</summary>
    public float CombatEngagementRate
    {
        get
        {
            int encounter = EncounterRoomCount;
            return encounter == 0 ? 0.5f :
                Mathf.Clamp01((float)EngagedRoomCount / encounter);
        }
    }

    /// <summary>교전한 방 중 적을 전멸시킨 비율.</summary>
    public float EnemyClearRate
    {
        get
        {
            int engaged = EngagedRoomCount;
            return engaged == 0 ? 0.5f :
                Mathf.Clamp01((float)ClearedRoomCount / engaged);
        }
    }

    /// <summary>적이 있는 방 중 그냥 통과한 비율.</summary>
    public float SkipRate
    {
        get
        {
            int encounter = EncounterRoomCount;
            return encounter == 0 ? 0.5f :
                Mathf.Clamp01((float)(encounter - EngagedRoomCount) / encounter);
        }
    }

    /// <summary>저체력 상황 중 후퇴한 비율.</summary>
    public float HPThresholdRetreat =>
        _data.lowHPMoments == 0 ? 0.5f :
        Mathf.Clamp01((float)_data.retreatsAtLowHP / _data.lowHPMoments);

    public float[] GetInputVector() =>
        new[] { CombatEngagementRate, EnemyClearRate, SkipRate, HPThresholdRetreat };

    // ── 통계 집계 프로퍼티 ───────────────────────────────────────────────────

    public int TotalRoomsVisited  => _data.roomRecords.Count;
    public int EncounterRoomCount => _data.roomRecords.FindAll(r => r.totalEnemies > 0).Count;
    public int EngagedRoomCount   => _data.roomRecords.FindAll(r => r.engaged).Count;
    public int ClearedRoomCount   => _data.roomRecords.FindAll(r => r.fullyCleared).Count;

    // ── 방 단위 이벤트 API ───────────────────────────────────────────────────

    /// <summary>방 진입 시 호출. 새 레코드를 생성하고 현재 방 ID를 반환합니다.</summary>
    public RoomBattleRecord RecordRoomEntered(string roomId, int enemyCount)
    {
        var rec = new RoomBattleRecord
        {
            roomId       = roomId,
            totalEnemies = enemyCount,
        };
        _data.roomRecords.Add(rec);
        Save();
        return rec;
    }

    /// <summary>방에서 교전이 시작될 때 호출.</summary>
    public void RecordRoomEngagement(RoomBattleRecord record)
    {
        if (record == null || record.engaged) return;
        record.engaged = true;
        Save();
    }

    /// <summary>방에서 적 하나가 처치될 때 호출.</summary>
    public void RecordEnemyKilledInRoom(RoomBattleRecord record)
    {
        if (record == null) return;
        record.killedEnemies++;
        if (record.killedEnemies >= record.totalEnemies && record.totalEnemies > 0)
        {
            record.fullyCleared = true;
        }
        Save();
    }

    // ── 기존 글로벌 API (HP 후퇴 전용) ──────────────────────────────────────

    public void RecordLowHP()            { _data.lowHPMoments++;    Save(); }
    public void RecordRetreatAtLowHP()   { _data.retreatsAtLowHP++; Save(); }

    // ── Unity 생명주기 ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── 저장/로드 ────────────────────────────────────────────────────────────

    private void Save()
    {
        File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
    }

    private void Load()
    {
        if (!File.Exists(FilePath)) return;
        var loaded = JsonUtility.FromJson<BehaviorData>(File.ReadAllText(FilePath));
        if (loaded != null) _data = loaded;
    }
}
