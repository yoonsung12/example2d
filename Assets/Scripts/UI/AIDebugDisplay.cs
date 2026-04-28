using UnityEngine;

/// <summary>
/// 화면 좌측 하단에 가장 가까운 적의 NFBT AI 변수를 실시간 표시합니다.
/// 씬에 빈 GameObject를 만들어 이 컴포넌트를 붙이면 됩니다.
/// </summary>
public class AIDebugDisplay : MonoBehaviour
{
    [SerializeField] private bool _show = true; // 디버그 UI 표시 여부

    private NFBTEnemyAI _target; // 현재 감시 중인 적 AI

    private GUIStyle _boxStyle;    // 패널 배경 스타일
    private GUIStyle _labelStyle;  // 일반 텍스트 스타일
    private GUIStyle _headerStyle; // 헤더 텍스트 스타일

    private void Start()
    {
        _target = FindFirstObjectByType<NFBTEnemyAI>(); // 씬에서 첫 번째 적 AI 탐색
    }

    private void Update()
    {
        // 타겟이 없거나 사망하면 살아있는 적 재탐색
        if (_target == null || _target.Enemy == null || _target.Enemy.IsDead)
            _target = FindFirstObjectByType<NFBTEnemyAI>();
    }

    private void InitStyles()
    {
        if (_boxStyle != null) return; // 이미 초기화됐으면 무시

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = MakeTexture(new Color(0f, 0f, 0f, 0.72f)) }, // 반투명 검정 배경
            padding = new RectOffset(10, 10, 8, 8),
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal   = { textColor = Color.white }, // 흰색 텍스트
            richText = true,                         // 리치 텍스트 허용
        };

        _headerStyle = new GUIStyle(_labelStyle)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.4f, 0.9f, 1f) }, // 하늘색 헤더
        };
    }

    private void OnGUI()
    {
        if (!_show) return;
        InitStyles();

        DrawCombatStatsPanel(); // 전투 통계 패널 (좌상단)

        if (_target == null) return;

        // ── NFBT 메인 패널 (좌하단) ────────────────────────────────────────
        float x = 10f, y = Screen.height - 120f;
        float w = 310f, h = 110f;

        GUI.Box(new Rect(x, y, w, h), GUIContent.none, _boxStyle); // 패널 배경

        float lx = x + 12f; // 텍스트 X 시작점
        float ly = y + 10f; // 텍스트 Y 시작점
        float lh = 20f;      // 텍스트 줄 높이

        GUI.Label(new Rect(lx, ly, w, lh), "■ NFBT Enemy AI", _headerStyle); ly += 24f;

        // 플레이어 거리
        GUI.Label(new Rect(lx, ly, w, lh),
            $"Distance   <color=#ffdd88>{_target.DbgDist:F1}</color> u", _labelStyle); ly += 22f;

        DrawLine(lx, ly, w - 24f); ly += 10f; // 구분선

        // 활성 클러스터만 표시
        string[] branchColors = { "#ff8888", "#88aaff", "#ffdd44" }; // 빨강/파랑/노랑
        Color[]  barColors    = { new Color(1f, 0.4f, 0.4f), new Color(0.4f, 0.6f, 1f), new Color(1f, 0.85f, 0.2f) };
        int ci = Mathf.Clamp(_target.DbgClusterIndex, 0, 2);          // 클러스터 인덱스 범위 제한
        GUI.Label(new Rect(lx, ly, w, lh),
            $"Cluster    <color=#aaaaaa>[{ci}]</color>  " +
            $"<color={branchColors[ci]}><b>{_target.DbgBranch}</b></color>",
            _labelStyle); ly += 22f;

        DrawBranchBar(lx, ref ly, _target.DbgBranch, true, barColors[ci]); // 선택된 클러스터 바만 표시
    }

    // ── 전투 통계 패널 (좌상단) ────────────────────────────────────────────

    private void DrawCombatStatsPanel()
    {
        var tracker = CombatStatsTracker.Instance; // 전투 통계 트래커 참조

        float x = 10f, y = 10f;
        float w = 300f, h = 130f;

        GUI.Box(new Rect(x, y, w, h), GUIContent.none, _boxStyle); // 패널 배경

        float lx = x + 12f;
        float ly = y + 10f;
        float lh = 20f;

        GUI.Label(new Rect(lx, ly, w, lh), "■ Combat Stats (Player)", _headerStyle); ly += 24f;

        if (tracker == null)
        {
            GUI.Label(new Rect(lx, ly, w, lh),
                "<color=#ff8888>CombatStatsTracker 없음</color>", _labelStyle); // 트래커 없음 경고
            return;
        }

        DrawLine(lx, ly, w - 24f); ly += 8f; // 구분선

        // 3개 피처 바 표시
        DrawFeatureRow(lx, ref ly, lh, "AttackFreq ", tracker.AttackFrequency, new Color(1f, 0.4f, 0.4f),  "초당 공격 횟수");
        DrawFeatureRow(lx, ref ly, lh, "HitRate    ", tracker.HitRate,         new Color(0.4f, 1f, 0.5f),  "명중률");
        DrawFeatureRow(lx, ref ly, lh, "DmgPerSec  ", tracker.DamagePerSec,    new Color(0.4f, 0.7f, 1f),  "초당 피해");
    }

    // ── 피처 행 표시 헬퍼 ────────────────────────────────────────────────────

    private void DrawFeatureRow(float x, ref float y, float lh,
        string label, float value, Color barColor, string desc)
    {
        // 레이블 + 값 + 설명 텍스트 출력
        GUI.Label(new Rect(x, y, 290f, lh),
            $"{label} <b><color=#{ColorToHex(barColor)}>{value:F3}</color></b>  <color=#888888>{desc}</color>",
            _labelStyle);
        y += 18f;
        DrawBar(x, y, 200f, 6f, value, barColor); // 값을 바 형태로 시각화
        y += 11f;
    }

    // ── 분기 바 표시 헬퍼 ────────────────────────────────────────────────────

    private void DrawBranchBar(float x, ref float y, string label, bool active, Color color)
    {
        string activeTag = active ? " <color=#ffff00>◀ 활성</color>" : ""; // 활성 분기 표시
        GUI.Label(new Rect(x, y, 290f, 18f),
            $"{label}{activeTag}", _labelStyle);
        y += 18f;
        DrawBar(x, y, 180f, 5f, active ? 1f : 0f, active ? Color.white : color); // 활성 분기는 흰색 바
        y += 10f;
    }

    // ── 공통 그리기 헬퍼 ─────────────────────────────────────────────────────

    private void DrawBar(float x, float y, float maxW, float h, float value, Color color)
    {
        GUI.DrawTexture(new Rect(x, y, maxW, h),
            MakeTexture(new Color(0.3f, 0.3f, 0.3f, 0.8f)));              // 배경 바 (회색)
        GUI.DrawTexture(new Rect(x, y, maxW * Mathf.Clamp01(value), h),
            MakeTexture(color));                                             // 채움 바 (컬러)
    }

    private void DrawLine(float x, float y, float w)
    {
        GUI.DrawTexture(new Rect(x, y, w, 1f),
            MakeTexture(new Color(0.5f, 0.5f, 0.5f, 0.5f))); // 반투명 회색 구분선
    }

    private static string ColorToHex(Color c)
    {
        return $"{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}"; // Color → 16진수 RGB 문자열
    }

    private static int ToByte(float v) => Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255); // float [0,1] → byte [0,255]

    private static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1);  // 1×1 픽셀 텍스처 생성
        tex.SetPixel(0, 0, color);       // 픽셀 색상 설정
        tex.Apply();                      // 텍스처 반영
        return tex;
    }
}
