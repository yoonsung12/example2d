using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 플레이어의 전투 행동 통계를 수집, 저장, 로드합니다.
/// RBFN 입력 벡터(4차원)를 제공합니다.
/// </summary>
public class PlayerBehaviorTracker : MonoBehaviour
{
    public static PlayerBehaviorTracker Instance { get; private set; }

    private const string FileName = "behavior.json";
    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    [Serializable]
    private class BehaviorData
    {
        public int totalEncounters;
        public int combatEngagements;
        public int enemiesCleared;
        public int encountersSkipped;
        public int retreatsAtLowHP;
        public int lowHPMoments;
    }

    private BehaviorData _data = new();

    // ── 정규화된 RBFN 입력값 [0,1] ─────────────────────────────────────────

    public float CombatEngagementRate =>
        _data.totalEncounters == 0 ? 0.5f :
        Mathf.Clamp01((float)_data.combatEngagements / _data.totalEncounters);

    public float EnemyClearRate =>
        _data.combatEngagements == 0 ? 0.5f :
        Mathf.Clamp01((float)_data.enemiesCleared / _data.combatEngagements);

    public float SkipRate =>
        _data.totalEncounters == 0 ? 0.5f :
        Mathf.Clamp01((float)_data.encountersSkipped / _data.totalEncounters);

    public float HPThresholdRetreat =>
        _data.lowHPMoments == 0 ? 0.5f :
        Mathf.Clamp01((float)_data.retreatsAtLowHP / _data.lowHPMoments);

    public float[] GetInputVector() =>
        new[] { CombatEngagementRate, EnemyClearRate, SkipRate, HPThresholdRetreat };

    // ── 이벤트 기록 API ─────────────────────────────────────────────────────

    public void RecordEncounter()        { _data.totalEncounters++;      Save(); }
    public void RecordEngagement()       { _data.combatEngagements++;    Save(); }
    public void RecordEnemyCleared()     { _data.enemiesCleared++;       Save(); }
    public void RecordSkip()             { _data.encountersSkipped++;    Save(); }
    public void RecordLowHP()            { _data.lowHPMoments++;         Save(); }
    public void RecordRetreatAtLowHP()   { _data.retreatsAtLowHP++;      Save(); }

    // ── Unity 생명주기 ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

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
