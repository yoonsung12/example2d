using UnityEngine;

/// <summary>
/// 화면 좌측 하단에 가장 가까운 적의 NFBT AI 변수를 실시간 표시합니다.
/// 씬에 빈 GameObject를 만들어 이 컴포넌트를 붙이면 됩니다.
/// </summary>
public class AIDebugDisplay : MonoBehaviour
{
    [SerializeField] private bool _show = true;

    private NFBTEnemyAI _target;

    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;

    private void Start()
    {
        _target = FindFirstObjectByType<NFBTEnemyAI>();
    }

    private void Update()
    {
        // 타겟이 없거나 죽었으면 살아있는 적 재탐색
        if (_target == null || _target.Enemy == null || _target.Enemy.IsDead)
            _target = FindFirstObjectByType<NFBTEnemyAI>();
    }

    private void InitStyles()
    {
        if (_boxStyle != null) return;

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(new Color(0f, 0f, 0f, 0.72f)) },
            padding = new RectOffset(10, 10, 8, 8),
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            normal    = { textColor = Color.white },
            richText  = true,
        };

        _headerStyle = new GUIStyle(_labelStyle)
        {
            fontSize  = 14,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.4f, 0.9f, 1f) },
        };
    }

    private void OnGUI()
    {
        if (!_show) return;
        InitStyles();

        // 행동 트래커 패널 (항상 표시)
        DrawTrackerPanel();

        // FCM 클러스터 패널 (우상단)
        DrawFCMPanel();

        if (_target == null) return;

        float x = 10f, y = Screen.height - 260f;
        float w = 310f, h = 250f;

        GUI.Box(new Rect(x, y, w, h), GUIContent.none, _boxStyle);

        float lx = x + 12f;
        float ly = y + 10f;
        float lh = 20f;

        GUI.Label(new Rect(lx, ly, w, lh), "■ NFBT Enemy AI", _headerStyle); ly += 24f;

        // PlayStyle_Score 바
        float ps = _target.DbgPlayStyle;
        GUI.Label(new Rect(lx, ly, w, lh),
            $"PlayStyle  <color=#aaffaa>{ps:F3}</color>  (0=회피 / 1=공격)", _labelStyle);
        ly += 18f;
        DrawBar(lx, ly, w - 24f, 8f, ps, new Color(0.3f, 0.9f, 0.4f)); ly += 14f;

        GUI.Label(new Rect(lx, ly, w, lh),
            $"Distance   <color=#ffdd88>{_target.DbgDist:F1}</color> u", _labelStyle); ly += 20f;

        // 구분선
        DrawLine(lx, ly, w - 24f); ly += 10f;

        // 각 분기 U_final
        DrawBranchRow(lx, ref ly, lh, "A Chase/Atk",
            _target.DbgSBaseA, _target.DbgSFuzzyA, _target.DbgUFinalA,
            _target.DbgBranch == "Chase/Attack",  new Color(1f, 0.4f, 0.4f));

        DrawBranchRow(lx, ref ly, lh, "B Evade    ",
            _target.DbgSBaseB, _target.DbgSFuzzyB, _target.DbgUFinalB,
            _target.DbgBranch == "Evade/Recover", new Color(0.4f, 0.7f, 1f));

        DrawBranchRow(lx, ref ly, lh, "C Ambush   ",
            _target.DbgSBaseC, _target.DbgSFuzzyC, _target.DbgUFinalC,
            _target.DbgBranch == "Ambush",         new Color(1f, 0.8f, 0.2f));

        // 현재 분기
        DrawLine(lx, ly, w - 24f); ly += 10f;
        GUI.Label(new Rect(lx, ly, w, lh),
            $"Active: <color=#ffff66>{_target.DbgBranch}</color>", _labelStyle);
    }

    private void DrawFCMPanel()
    {
        if (_target == null) return;

        float w  = 280f, h = 110f;
        float px = Screen.width - w - 10f;
        float py = 10f;

        GUI.Box(new Rect(px, py, w, h), GUIContent.none, _boxStyle);

        float lx = px + 12f;
        float ly = py + 10f;
        float lh = 18f;

        GUI.Label(new Rect(lx, ly, w, lh), "■ FCM 클러스터 임계값", _headerStyle); ly += 22f;

        // HP 임계값
        GUI.Label(new Rect(lx, ly, w, lh),
            $"HP   Low <color=#ff8888>{_target.DbgHPLow:F1}</color>  " +
            $"Med <color=#ffdd88>{_target.DbgHPMedium:F1}</color>  " +
            $"High <color=#88ff88>{_target.DbgHPHigh:F1}</color>",
            _labelStyle); ly += lh;

        // 거리 임계값
        GUI.Label(new Rect(lx, ly, w, lh),
            $"Dist Near <color=#88ccff>{_target.DbgDistNear:F2}</color>  " +
            $"Far <color=#aaaaff>{_target.DbgDistFar:F2}</color>",
            _labelStyle); ly += lh;

        DrawLine(lx, ly, w - 24f); ly += 8f;

        // 마지막 갱신 시각
        float lastUpdate = _target.DbgFCMLastTime;
        string updateStr = lastUpdate <= 0f
            ? "갱신 대기 중"
            : $"{lastUpdate:F1}s (다음 갱신 약 {Mathf.Max(0f, lastUpdate + 30f - Time.time):F0}초 후)";
        GUI.Label(new Rect(lx, ly, w, lh),
            $"마지막 갱신: <color=#aaaaaa>{updateStr}</color>",
            _labelStyle);
    }

    private void DrawTrackerPanel()
    {
        var tr = PlayerBehaviorTracker.Instance;

        float x = 10f, y = 10f;
        float w = 300f, h = 148f;

        GUI.Box(new Rect(x, y, w, h), GUIContent.none, _boxStyle);

        float lx = x + 12f;
        float ly = y + 10f;
        float lh = 20f;

        GUI.Label(new Rect(lx, ly, w, lh), "■ Player Behavior Tracker", _headerStyle); ly += 24f;

        if (tr == null)
        {
            GUI.Label(new Rect(lx, ly, w, lh), "<color=#ff8888>PlayerBehaviorTracker 없음</color>", _labelStyle);
            return;
        }

        // 방 통계 요약
        GUI.Label(new Rect(lx, ly, w, lh),
            $"방문 <color=#ffdd88>{tr.TotalRoomsVisited}</color>  " +
            $"교전 <color=#ff8888>{tr.EngagedRoomCount}</color>  " +
            $"전멸 <color=#aaffaa>{tr.ClearedRoomCount}</color>  " +
            $"통과 <color=#88aaff>{tr.EncounterRoomCount - tr.EngagedRoomCount}</color>",
            _labelStyle); ly += 22f;

        DrawLine(lx, ly, w - 24f); ly += 8f;

        // 4개 RBFN 입력 텐서
        DrawTensorRow(lx, ref ly, lh, "CombatRate ",
            tr.CombatEngagementRate, new Color(1f, 0.4f, 0.4f), "교전비율");
        DrawTensorRow(lx, ref ly, lh, "ClearRate  ",
            tr.EnemyClearRate,       new Color(0.4f, 1f, 0.5f), "전멸비율");
        DrawTensorRow(lx, ref ly, lh, "SkipRate   ",
            tr.SkipRate,             new Color(0.4f, 0.7f, 1f), "통과비율");
        DrawTensorRow(lx, ref ly, lh, "HPRetreat  ",
            tr.HPThresholdRetreat,   new Color(1f, 0.8f, 0.2f), "저HP후퇴");
    }

    private void DrawTensorRow(float x, ref float y, float lh,
        string label, float value, Color barColor, string desc)
    {
        GUI.Label(new Rect(x, y, 290f, lh),
            $"{label} <b><color=#{ColorToHex(barColor)}>{value:F3}</color></b>  <color=#888888>{desc}</color>",
            _labelStyle);
        y += 18f;
        DrawBar(x, y, 200f, 6f, value, barColor);
        y += 11f;
    }

    private static string ColorToHex(Color c)
    {
        return $"{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}";
    }

    private static int ToByte(float v) => Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

    private void DrawBranchRow(float x, ref float y, float lh, string label,
        float sBase, float sFuzzy, float uFinal, bool active, Color barColor)
    {
        string activeTag = active ? " <color=#ffff00>◀</color>" : "";
        GUI.Label(new Rect(x, y, 290f, lh),
            $"{label}  Sb={sBase:F2} Sf={sFuzzy:F2}  <b>U={uFinal:F2}</b>{activeTag}",
            _labelStyle);
        y += 18f;
        DrawBar(x, y, 180f, 6f, uFinal, active ? Color.white : barColor);
        y += 12f;
    }

    private void DrawBar(float x, float y, float maxW, float h, float value, Color color)
    {
        // 배경
        GUI.DrawTexture(new Rect(x, y, maxW, h),
            MakeTexture(new Color(0.3f, 0.3f, 0.3f, 0.8f)));
        // 채움
        GUI.DrawTexture(new Rect(x, y, maxW * Mathf.Clamp01(value), h),
            MakeTexture(color));
    }

    private void DrawLine(float x, float y, float w)
    {
        GUI.DrawTexture(new Rect(x, y, w, 1f),
            MakeTexture(new Color(0.5f, 0.5f, 0.5f, 0.5f)));
    }

    private static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
