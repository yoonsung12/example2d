using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면 우하단에 플레이 스타일 산점도를 실시간으로 표시합니다.
/// X축: AttackFrequency (공격 빈도) / Y축: HitRate (명중률)
/// 점 색상: 빨강=Chase/Attack, 파랑=Evade/Recover, 노랑=Counter
/// 마름모 마커: FCM 클러스터 중심 (X=attackFreq, Y=hitRate 투영)
/// </summary>
public class AIScatterPlotUI : MonoBehaviour
{
    private const int   PlotW          = 200; // 산점도 텍스처 너비 (픽셀)
    private const int   PlotH          = 160; // 산점도 텍스처 높이 (픽셀)
    private const int   MaxPoints      = 150; // 최대 데이터 포인트 수
    private const float SampleInterval = 0.5f; // 샘플링 간격 (초)

    // 산점도에 표시할 데이터 포인트 구조체
    private struct DataPoint
    {
        public float  attackFreq; // attack_frequency 값 [0,1]
        public float  hitRate;    // hit_rate 값 [0,1]
        public string branch;     // 해당 시점의 분기명
    }

    private readonly List<DataPoint> _points = new(MaxPoints + 1); // 데이터 포인트 저장소

    private NFBTEnemyAI _target;      // 감시 중인 적 AI
    private Texture2D   _plotTex;     // 산점도 텍스처
    private float       _sampleTimer; // 다음 샘플링까지 남은 시간

    private GUIStyle _boxStyle;    // 패널 배경 스타일
    private GUIStyle _labelStyle;  // 일반 텍스트 스타일
    private GUIStyle _headerStyle; // 헤더 텍스트 스타일
    private GUIStyle _axisStyle;   // 축 레이블 스타일

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        _plotTex            = new Texture2D(PlotW, PlotH, TextureFormat.RGBA32, false); // 산점도 텍스처 생성
        _plotTex.filterMode = FilterMode.Point; // 픽셀 선명 렌더링
        ClearTexture();                          // 텍스처 초기화
    }

    private void Start()
    {
        _target = FindFirstObjectByType<NFBTEnemyAI>(); // 씬에서 첫 번째 적 AI 탐색
    }

    private void Update()
    {
        // 타겟 없거나 사망 시 재탐색
        if (_target == null || (_target.Enemy != null && _target.Enemy.IsDead))
            _target = FindFirstObjectByType<NFBTEnemyAI>();

        if (_target == null) return;

        _sampleTimer -= Time.deltaTime; // 샘플링 타이머 감소
        if (_sampleTimer > 0f) return;
        _sampleTimer = SampleInterval;  // 타이머 리셋

        AddPoint(); // 현재 피처 값 샘플링
        Redraw();   // 텍스처 다시 그리기
    }

    private void OnGUI()
    {
        InitStyles();

        const float panelW = PlotW + 24f;  // 패널 너비 = 텍스처 + 여백
        const float panelH = PlotH + 90f;  // 패널 높이 = 텍스처 + 레이블 영역
        float px = Screen.width  - panelW - 10f; // 우측 정렬
        float py = Screen.height - panelH - 10f; // 하단 정렬

        GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _boxStyle); // 패널 배경

        float cx = px + 12f; // 콘텐츠 X 시작점
        float cy = py + 8f;  // 콘텐츠 Y 시작점

        // 타이틀
        GUI.Label(new Rect(cx, cy, panelW, 20f), "■ Play Style 산점도", _headerStyle);
        cy += 4f;

        // Y축 레이블 (HitRate)
        GUI.Label(new Rect(cx, cy + 14f, 36f, 14f),    "HR", _axisStyle); // 상단 = 높은 명중률
        GUI.Label(new Rect(cx, cy + PlotH - 2f, 20f, 14f), "0", _axisStyle); // 하단 = 0

        // 산점도 텍스처
        float plotX = cx + 18f; // Y축 레이블 우측에 배치
        cy += 20f;
        GUI.DrawTexture(new Rect(plotX, cy, PlotW, PlotH), _plotTex); // 산점도 텍스처 렌더링

        // X축 레이블 (AttackFreq)
        float axisY = cy + PlotH + 2f;
        GUI.Label(new Rect(plotX, axisY, 20f, 14f),                "0",  _axisStyle); // 좌측 = 0
        GUI.Label(new Rect(plotX + PlotW - 24f, axisY, 28f, 14f), "AF", _axisStyle); // 우측 = 높은 공격빈도

        cy += PlotH + 18f;

        DrawLegend(cx, cy); // 범례 표시
        cy += 20f;

        // 현재 분기 표시
        if (_target != null)
        {
            string branch = _target.DbgBranch;              // 현재 분기명
            Color  bc     = BranchColor(branch);             // 분기 색상
            GUI.Label(new Rect(cx, cy, panelW, 20f),
                $"Active: <color=#{ColorHex(bc)}><b>{branch}</b></color>",
                _labelStyle);
        }
    }

    // ── 데이터 수집 ───────────────────────────────────────────────────────────

    private void AddPoint()
    {
        if (_points.Count >= MaxPoints) _points.RemoveAt(0); // 오래된 포인트 제거
        _points.Add(new DataPoint
        {
            attackFreq = _target.DbgAttackFreq, // 현재 공격 빈도
            hitRate    = _target.DbgHitRate,    // 현재 명중률
            branch     = _target.DbgBranch,     // 현재 분기명
        });
    }

    // ── 텍스처 렌더링 ─────────────────────────────────────────────────────────

    private void Redraw()
    {
        var   pixels = new Color[PlotW * PlotH]; // 픽셀 배열 초기화
        Color bg     = new Color(0.08f, 0.08f, 0.12f, 1f); // 배경색 (어두운 남색)
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg; // 배경 채우기

        // FCM 클러스터 중심 마커 그리기 (3개)
        if (_target?.DbgCenters != null && _target.DbgCenters.Length >= 3)
        {
            Color markerColor = new Color(0.2f, 1f, 0.7f, 1f); // 청록색 마커
            for (int i = 0; i < 3; i++)
            {
                float[] c  = _target.DbgCenters[i];    // i번째 클러스터 중심
                int     px = ToPixX(c[0]);              // attackFreq → X 픽셀
                int     py = ToPixY(c[1]);              // hitRate → Y 픽셀
                DrawDiamond(pixels, px, py, markerColor, 4); // 마름모 마커
            }
        }

        // 데이터 포인트 그리기 (오래된 점일수록 어둡게)
        for (int i = 0; i < _points.Count; i++)
        {
            var   p      = _points[i];
            float fade   = 0.25f + 0.75f * ((float)(i + 1) / _points.Count); // 페이드 계수
            Color baseC  = BranchColor(p.branch);                              // 분기 색상
            Color c      = new Color(baseC.r * fade, baseC.g * fade, baseC.b * fade, 1f); // 페이드 적용
            DrawDot(pixels, ToPixX(p.attackFreq), ToPixY(p.hitRate), c, 2);   // 점 그리기
        }

        // 가장 최근 포인트: 흰색 크게
        if (_points.Count > 0)
        {
            var last = _points[_points.Count - 1]; // 마지막 포인트
            DrawDot(pixels, ToPixX(last.attackFreq), ToPixY(last.hitRate), Color.white, 4);
        }

        _plotTex.SetPixels(pixels); // 픽셀 배열 → 텍스처 반영
        _plotTex.Apply();           // GPU에 업로드
    }

    private void ClearTexture()
    {
        var   pixels = new Color[PlotW * PlotH];
        Color bg     = new Color(0.08f, 0.08f, 0.12f, 1f); // 배경색
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
        _plotTex.SetPixels(pixels);
        _plotTex.Apply();
    }

    // ── 픽셀 드로우 헬퍼 ─────────────────────────────────────────────────────

    private void DrawDot(Color[] pixels, int cx, int cy, Color c, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            int x = cx + dx, y = cy + dy;
            if (x < 0 || x >= PlotW || y < 0 || y >= PlotH) continue; // 범위 밖 무시
            pixels[y * PlotW + x] = c; // 픽셀 색상 설정
        }
    }

    private void DrawDiamond(Color[] pixels, int cx, int cy, Color c, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius) continue; // 마름모 형태 필터링
            int x = cx + dx, y = cy + dy;
            if (x < 0 || x >= PlotW || y < 0 || y >= PlotH) continue; // 범위 밖 무시
            pixels[y * PlotW + x] = c; // 픽셀 색상 설정
        }
    }

    // [0,1] 값 → X 픽셀 좌표 변환
    private static int ToPixX(float normVal) =>
        Mathf.Clamp(Mathf.RoundToInt(normVal * (PlotW - 1)), 0, PlotW - 1);

    // [0,1] 값 → Y 픽셀 좌표 변환
    private static int ToPixY(float normVal) =>
        Mathf.Clamp(Mathf.RoundToInt(normVal * (PlotH - 1)), 0, PlotH - 1);

    // ── UI 헬퍼 ──────────────────────────────────────────────────────────────

    private void DrawLegend(float x, float y)
    {
        DrawColorBox(x,        y, new Color(1f, 0.4f, 0.4f));               // 빨강 범례 박스
        GUI.Label(new Rect(x + 14f, y, 60f, 16f),  "Chase",  _labelStyle);  // Chase 레이블
        DrawColorBox(x + 65f,  y, new Color(0.4f, 0.7f, 1f));               // 파랑 범례 박스
        GUI.Label(new Rect(x + 79f, y, 60f, 16f),  "Evade",  _labelStyle);  // Evade 레이블
        DrawColorBox(x + 130f, y, new Color(1f, 0.85f, 0.2f));              // 노랑 범례 박스
        GUI.Label(new Rect(x + 144f, y, 60f, 16f), "Counter", _labelStyle); // Counter 레이블
    }

    private void DrawColorBox(float x, float y, Color c)
    {
        GUI.DrawTexture(new Rect(x, y + 3f, 10f, 10f), MakeTex(c)); // 10×10 색상 박스
    }

    // 분기명 → 색상 매핑
    private static Color BranchColor(string branch) => branch switch
    {
        "Chase/Attack"  => new Color(1f, 0.4f, 0.4f),   // 빨강: 추격
        "Evade/Recover" => new Color(0.4f, 0.7f, 1f),   // 파랑: 회피
        "Counter"       => new Color(1f, 0.85f, 0.2f),  // 노랑: 카운터
        _               => Color.gray,                    // 기본: 회색
    };

    private static string ColorHex(Color c)
    {
        return $"{Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255):X2}" +
               $"{Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255):X2}" +
               $"{Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255):X2}"; // Color → 16진수 RGB
    }

    private void InitStyles()
    {
        if (_boxStyle != null) return; // 이미 초기화됐으면 무시

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = MakeTex(new Color(0f, 0f, 0f, 0.75f)) }, // 반투명 검정 배경
            padding = new RectOffset(10, 10, 8, 8),
        };
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal   = { textColor = Color.white }, // 흰색 텍스트
            richText = true,
        };
        _headerStyle = new GUIStyle(_labelStyle)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.4f, 0.9f, 1f) }, // 하늘색 헤더
        };
        _axisStyle = new GUIStyle(_labelStyle)
        {
            fontSize = 10,
            normal   = { textColor = new Color(0.6f, 0.6f, 0.6f) }, // 회색 축 레이블
        };
    }

    private static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1); // 1×1 픽셀 텍스처 생성
        t.SetPixel(0, 0, c);          // 픽셀 색상 설정
        t.Apply();                     // GPU에 업로드
        return t;
    }
}
