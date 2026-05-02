using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 봄/여름/가을/겨울 게이지를 하나의 공유 바로 관리한다.
/// 위험 요소에 맞을 때마다 AddHit()으로 한 칸씩 쌓이고,
/// 총 칸이 _segmentCount에 도달하면 가장 많이 쌓인 계절의 디버프를 발동한다.
/// 동점 시 해당 계절 중 하나를 랜덤으로 선택한다.
/// 디버프 발동 중에는 누적이 중단되고 _decayRate 속도로 칸이 줄어든다.
/// </summary>
public class SeasonalGauge : MonoBehaviour
{
    public static SeasonalGauge Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int   _segmentCount = 10; // 최대 칸 수 (UI의 _segmentCount와 일치시켜야 함)
    [SerializeField] private float _decayRate    = 1f; // 디버프 중 초당 제거 칸 수

    // 맞은 순서대로 계절을 기록 — 인덱스 0이 가장 먼저 맞은 칸
    private readonly List<SeasonType> _hits = new();

    private float _decayAccum; // 소수점 감소 누적값 (1 이상이 되면 칸 하나 제거)

    public bool IsDebuffActive { get; private set; }
    public int  SegmentCount   => _segmentCount; // UI에서 참조용

    /// <summary>디버프 발동 시 — 발동된 계절 전달</summary>
    public event Action<SeasonType>       OnDebuffTriggered;
    /// <summary>디버프 해제 시</summary>
    public event Action                   OnDebuffEnded;
    /// <summary>게이지 변경 시 — 현재 히트 목록 전달 (UI 갱신용)</summary>
    public event Action<List<SeasonType>> OnHitsChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!IsDebuffActive) return;

        // 디버프 중: _decayRate 속도로 칸 하나씩 제거
        _decayAccum += _decayRate * Time.deltaTime;
        while (_decayAccum >= 1f && _hits.Count > 0)
        {
            _hits.RemoveAt(_hits.Count - 1); // 가장 최근 칸부터 제거
            _decayAccum -= 1f;
            OnHitsChanged?.Invoke(_hits);
        }

        if (_hits.Count == 0)
        {
            _decayAccum    = 0f;
            IsDebuffActive = false;
            OnDebuffEnded?.Invoke(); // 모든 칸이 사라지면 디버프 해제
        }
    }

    /// <summary>위험 요소가 플레이어에게 닿을 때 호출 — 해당 계절 칸 1개 추가</summary>
    public void AddHit(SeasonType season)
    {
        if (IsDebuffActive) return;          // 디버프 중에는 누적 불가
        if (_hits.Count >= _segmentCount) return; // 이미 꽉 찬 경우 무시

        _hits.Add(season);                   // 맞은 계절 순서대로 기록
        OnHitsChanged?.Invoke(_hits);        // UI에 변경 알림

        if (_hits.Count >= _segmentCount)
            TriggerDebuff();                 // 꽉 차면 디버프 발동
    }

    /// <summary>현재 특정 계절의 칸 수 반환</summary>
    public int GetHitCount(SeasonType season)
    {
        int count = 0;
        foreach (var h in _hits) if (h == season) count++;
        return count;
    }

    /// <summary>가장 많이 쌓인 계절로 디버프 발동. 동점 시 랜덤 선택.</summary>
    private void TriggerDebuff()
    {
        // 계절별 칸 수 집계
        int[] counts = new int[4];
        foreach (var h in _hits) counts[(int)h]++;

        // 최댓값 탐색
        int max = 0;
        for (int i = 0; i < counts.Length; i++)
            if (counts[i] > max) max = counts[i];

        // 동점 계절 수집 후 랜덤 선택
        var candidates = new List<SeasonType>();
        for (int i = 0; i < counts.Length; i++)
            if (counts[i] == max) candidates.Add((SeasonType)i);

        SeasonType dominant = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        IsDebuffActive = true;
        OnDebuffTriggered?.Invoke(dominant);
    }
}
