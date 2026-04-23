using System;
using UnityEngine;

/// <summary>
/// 봄/여름/가을/겨울 게이지를 독립적으로 관리한다.
/// 각 게이지가 100에 도달하면 가장 많이 쌓인 계절의 디버프를 발동한다.
/// 디버프 발동 중에는 게이지 누적이 중단되고 서서히 0으로 감소한다.
/// </summary>
public class SeasonalGauge : MonoBehaviour
{
    public static SeasonalGauge Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _maxGauge      = 100f; // 디버프 발동 임계값
    [SerializeField] private float _decayRate     = 8f;   // 디버프 중 초당 감소량

    // 계절별 현재 게이지 값 (Spring=0, Summer=1, Autumn=2, Winter=3)
    private readonly float[] _gauges = new float[4];

    public bool IsDebuffActive { get; private set; }

    /// <summary>디버프 발동 시 — 발동된 계절 전달</summary>
    public event Action<SeasonType> OnDebuffTriggered;
    /// <summary>디버프 해제 시</summary>
    public event Action            OnDebuffEnded;
    /// <summary>게이지 변경 시 — 계절, 현재값, 최대값 전달</summary>
    public event Action<SeasonType, float, float> OnGaugeChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!IsDebuffActive) return;

        // 디버프 중: 모든 게이지 감소
        bool allZero = true;
        for (int i = 0; i < _gauges.Length; i++)
        {
            if (_gauges[i] > 0f)
            {
                _gauges[i] = Mathf.Max(0f, _gauges[i] - _decayRate * Time.deltaTime);
                OnGaugeChanged?.Invoke((SeasonType)i, _gauges[i], _maxGauge);
                if (_gauges[i] > 0f) allZero = false;
            }
        }

        if (allZero)
        {
            IsDebuffActive = false;
            OnDebuffEnded?.Invoke();
        }
    }

    /// <summary>외부(Hazard 등)에서 특정 계절 게이지를 누적시킨다</summary>
    public void AddGauge(SeasonType season, float amount)
    {
        if (IsDebuffActive) return; // 디버프 중 누적 중단

        int idx = (int)season;
        _gauges[idx] = Mathf.Min(_maxGauge, _gauges[idx] + amount);
        OnGaugeChanged?.Invoke(season, _gauges[idx], _maxGauge);

        float total = 0f;
        foreach (var g in _gauges) total += g;

        if (total >= _maxGauge)
            TriggerDebuff();
    }

    /// <summary>현재 게이지 값 반환</summary>
    public float GetGauge(SeasonType season) => _gauges[(int)season];

    /// <summary>가장 많이 쌓인 계절로 디버프 발동</summary>
    private void TriggerDebuff()
    {
        SeasonType dominant = SeasonType.Spring;
        float max = -1f;
        for (int i = 0; i < _gauges.Length; i++)
        {
            if (_gauges[i] > max) { max = _gauges[i]; dominant = (SeasonType)i; }
        }

        IsDebuffActive = true;
        OnDebuffTriggered?.Invoke(dominant);
    }
}
