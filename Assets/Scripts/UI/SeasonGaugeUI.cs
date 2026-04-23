using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 계절별 게이지를 4개의 Slider로 표시한다.
/// SeasonalGauge.OnGaugeChanged 이벤트를 구독해 갱신한다.
/// </summary>
public class SeasonGaugeUI : MonoBehaviour
{
    [Header("Gauge Sliders (Spring / Summer / Autumn / Winter)")]
    [SerializeField] private Slider _springSlider;
    [SerializeField] private Slider _summerSlider;
    [SerializeField] private Slider _autumnSlider;
    [SerializeField] private Slider _winterSlider;

    private void Start()
    {
        if (SeasonalGauge.Instance != null)
            SeasonalGauge.Instance.OnGaugeChanged += RefreshGauge;
    }

    private void OnDestroy()
    {
        if (SeasonalGauge.Instance != null)
            SeasonalGauge.Instance.OnGaugeChanged -= RefreshGauge;
    }

    /// <summary>변경된 계절의 슬라이더를 갱신한다</summary>
    private void RefreshGauge(SeasonType season, float current, float max)
    {
        Slider target = season switch
        {
            SeasonType.Spring => _springSlider,
            SeasonType.Summer => _summerSlider,
            SeasonType.Autumn => _autumnSlider,
            SeasonType.Winter => _winterSlider,
            _                 => null
        };

        if (target == null) return;
        target.minValue = 0f;
        target.maxValue = max;
        target.value    = current;
    }
}
