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
        // 씬의 첫 번째 적 자동 탐색
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
        if (!_show || _target == null) return;
        InitStyles();

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
