using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SeasonalGauge의 히트 목록을 단일 게이지 바(칸 방식)로 표시한다.
/// 각 칸은 해당 칸을 채운 계절의 색으로 표시되며, 빈 칸은 회색이다.
/// _segmentCount는 SeasonalGauge의 값과 일치시켜야 한다.
/// </summary>
public class SeasonGaugeBarUI : MonoBehaviour
{
    [Header("Segment Settings")]
    [SerializeField] private int   _segmentCount  = 10;  // 칸 수 (SeasonalGauge._segmentCount와 동일하게 설정)
    [SerializeField] private float _segmentWidth  = 22f; // 칸 너비 (픽셀)
    [SerializeField] private float _segmentHeight = 16f; // 칸 높이 (픽셀)
    [SerializeField] private float _segmentGap    = 3f;  // 칸 간격 (픽셀)

    [Header("Row")]
    [SerializeField] private RectTransform _row; // 단일 게이지 행 부모

    // 계절별 채워진 칸 색상
    private static readonly Color SpringColor = new Color(1.00f, 0.50f, 0.70f); // 분홍 (봄)
    private static readonly Color SummerColor = new Color(0.30f, 0.60f, 1.00f); // 파랑 (여름)
    private static readonly Color AutumnColor = new Color(1.00f, 0.55f, 0.10f); // 주황 (가을)
    private static readonly Color WinterColor = new Color(0.70f, 0.90f, 1.00f); // 하늘 (겨울)
    private static readonly Color EmptyColor  = new Color(0.20f, 0.20f, 0.20f, 0.60f); // 빈 칸

    private Image[] _segments; // 게이지 칸 이미지 배열

    private void Awake()
    {
        _segments = BuildSegments(); // 칸 이미지 동적 생성
    }

    private void Start()
    {
        if (SeasonalGauge.Instance != null)
            SeasonalGauge.Instance.OnHitsChanged += Refresh; // 게이지 변경 이벤트 구독
    }

    private void OnDestroy()
    {
        if (SeasonalGauge.Instance != null)
            SeasonalGauge.Instance.OnHitsChanged -= Refresh; // 이벤트 구독 해제
    }

    /// <summary>_row 안에 _segmentCount개의 칸 Image를 생성하고 반환한다</summary>
    private Image[] BuildSegments()
    {
        if (_row == null) return new Image[0];

        var images = new Image[_segmentCount];
        float step = _segmentWidth + _segmentGap; // 칸 하나가 차지하는 X 간격

        for (int i = 0; i < _segmentCount; i++)
        {
            var go  = new GameObject("Seg_" + i, typeof(RectTransform));
            go.transform.SetParent(_row, false);

            var img   = go.AddComponent<Image>();
            img.color = EmptyColor; // 초기 상태는 빈 칸
            images[i] = img;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0f, 0.5f); // 좌중앙 앵커
            rect.anchorMax        = new Vector2(0f, 0.5f);
            rect.pivot            = new Vector2(0f, 0.5f);
            rect.sizeDelta        = new Vector2(_segmentWidth, _segmentHeight); // 칸 크기
            rect.anchoredPosition = new Vector2(i * step, 0f);                 // 좌→우 배치
        }

        return images;
    }

    /// <summary>히트 목록이 바뀔 때마다 칸 색을 갱신한다</summary>
    private void Refresh(List<SeasonType> hits)
    {
        for (int i = 0; i < _segments.Length; i++)
        {
            if (i < hits.Count)
                _segments[i].color = SeasonToColor(hits[i]); // 채워진 칸: 계절 색
            else
                _segments[i].color = EmptyColor;              // 빈 칸: 회색
        }
    }

    /// <summary>계절 → 색상 변환</summary>
    private static Color SeasonToColor(SeasonType season) => season switch
    {
        SeasonType.Spring => SpringColor,
        SeasonType.Summer => SummerColor,
        SeasonType.Autumn => AutumnColor,
        SeasonType.Winter => WinterColor,
        _                 => Color.white,
    };
}
