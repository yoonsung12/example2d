using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면 우하단에 플레이 스타일 산점도를 실시간으로 표시합니다.
/// X축: 플레이어와의 거리 / Y축: 플레이어 HP
/// 점 색상: 빨강=Chase, 파랑=Evade, 노랑=Ambush / 흰점=현재 위치
/// 회색 선: FCM 클러스터 임계값
/// </summary>
public class AIScatterPlotUI : MonoBehaviour
{
    private const int   PlotW      = 200;
    private const int   PlotH      = 160;
    private const int   MaxPoints  = 150;
    private const float SampleInterval = 0.5f;

    private struct DataPoint
    {
        public float  dist;
        public float  hp;
        public string branch;
    }

    private readonly List<DataPoint> _points = new(MaxPoints + 1);

    private NFBTEnemyAI _target;
    private Texture2D   _plotTex;
    private float       _sampleTimer;

    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _axisStyle;

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        _plotTex            = new Texture2D(PlotW, PlotH, TextureFormat.RGBA32, false);
        _plotTex.filterMode = FilterMode.Point;
        ClearTexture();
    }

    private void Start()
    {
        _target = FindFirstObjectByType<NFBTEnemyAI>();
    }

    private void Update()
    {
        if (_target == null || (_target.Enemy != null && _target.Enemy.IsDead))
            _target = FindFirstObjectByType<NFBTEnemyAI>();

        if (_target == null) return;

        _sampleTimer -= Time.deltaTime;
        if (_sampleTimer > 0f) return;
        _sampleTimer = SampleInterval;

        AddPoint();
        Redraw();
    }

    private void OnGUI()
    {
        InitStyles();

        const float panelW = PlotW + 24f;
        const float panelH = PlotH + 90f;
        float px = Screen.width  - panelW - 10f;
        float py = Screen.height - panelH - 10f;

        GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _boxStyle);

        float cx = px + 12f;
        float cy = py + 8f;

        // 타이틀
        GUI.Label(new Rect(cx, cy, panelW, 20f), "■ Play Style 산점도", _headerStyle);
        cy += 4f;

        // 축 레이블 (Y축)
        GUI.Label(new Rect(cx, cy + 14f, 30f, 14f), "HP", _axisStyle);
        GUI.Label(new Rect(cx, cy + PlotH - 2f, 30f, 14f), "0", _axisStyle);

        // 산점도 텍스처 (Y축 레이블 우측에 배치)
        float plotX = cx + 18f;
        cy += 20f;
        GUI.DrawTexture(new Rect(plotX, cy, PlotW, PlotH), _plotTex);

        // 축 레이블 (X축)
        float axisY = cy + PlotH + 2f;
        GUI.Label(new Rect(plotX, axisY, 20f, 14f), "0", _axisStyle);
        GUI.Label(new Rect(plotX + PlotW - 24f, axisY, 40f, 14f), "Dist", _axisStyle);

        cy += PlotH + 18f;

        // 범례
        DrawLegend(cx, cy);
        cy += 20f;

        // 현재 분기
        if (_target != null)
        {
            string branch = _target.DbgBranch;
            Color  bc     = BranchColor(branch);
            GUI.Label(new Rect(cx, cy, panelW, 20f),
                $"Active: <color=#{ColorHex(bc)}><b>{branch}</b></color>",
                _labelStyle);
        }
    }

    // ── 데이터 수집 ───────────────────────────────────────────────────────────

    private void AddPoint()
    {
        if (_points.Count >= MaxPoints) _points.RemoveAt(0);
        _points.Add(new DataPoint
        {
            dist   = _target.DbgDist,
            hp     = GetPlayerHP(),
            branch = _target.DbgBranch
        });
    }

    private float GetPlayerHP()
    {
        var cb = _target.PlayerTransform?.GetComponent<CharacterBase>();
        return cb != null ? cb.CurrentHealth : 100f;
    }

    // ── 텍스처 렌더링 ─────────────────────────────────────────────────────────

    private void Redraw()
    {
        var   pixels  = new Color[PlotW * PlotH];
        Color bg      = new Color(0.08f, 0.08f, 0.12f, 1f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

        float maxDist = _target != null ? _target.DetectionRange : 12f;

        // FCM 임계선
        if (_target != null)
        {
            Color lineColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            DrawHLine(pixels, _target.DbgHPLow    / 100f, lineColor);
            DrawHLine(pixels, _target.DbgHPMedium / 100f, lineColor);
            DrawHLine(pixels, _target.DbgHPHigh   / 100f, lineColor);
            DrawVLine(pixels, _target.DbgDistNear / maxDist, lineColor);
            DrawVLine(pixels, _target.DbgDistFar  / maxDist, lineColor);

            // FCM 클러스터 센터 마커 (HP × Dist 교차점)
            Color markerColor = new Color(0.2f, 1f, 0.7f, 1f); // 청록색
            float[] hpCenters   = { _target.DbgHPLow, _target.DbgHPMedium, _target.DbgHPHigh };
            float[] distCenters = { _target.DbgDistNear, _target.DbgDistFar };
            foreach (float hp in hpCenters)
            foreach (float dist in distCenters)
                DrawDiamond(pixels, ToPixX(dist, maxDist), ToPixY(hp), markerColor, 4);
        }

        // 데이터 포인트 (오래된 점은 어둡게)
        for (int i = 0; i < _points.Count; i++)
        {
            var   p    = _points[i];
            float fade = 0.25f + 0.75f * ((float)(i + 1) / _points.Count);
            Color base_color = BranchColor(p.branch);
            Color c    = new Color(base_color.r * fade, base_color.g * fade, base_color.b * fade, 1f);
            DrawDot(pixels, ToPixX(p.dist, maxDist), ToPixY(p.hp), c, 2);
        }

        // 현재 점 (흰색, 크게)
        if (_points.Count > 0)
        {
            var last = _points[_points.Count - 1];
            DrawDot(pixels, ToPixX(last.dist, maxDist), ToPixY(last.hp), Color.white, 4);
        }

        _plotTex.SetPixels(pixels);
        _plotTex.Apply();
    }

    private void ClearTexture()
    {
        var   pixels = new Color[PlotW * PlotH];
        Color bg     = new Color(0.08f, 0.08f, 0.12f, 1f);
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
            if (x < 0 || x >= PlotW || y < 0 || y >= PlotH) continue;
            pixels[y * PlotW + x] = c;
        }
    }

    private void DrawDiamond(Color[] pixels, int cx, int cy, Color c, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius) continue;
            int x = cx + dx, y = cy + dy;
            if (x < 0 || x >= PlotW || y < 0 || y >= PlotH) continue;
            pixels[y * PlotW + x] = c;
        }
    }

    private void DrawHLine(Color[] pixels, float normY, Color c)
    {
        int py = Mathf.Clamp(Mathf.RoundToInt(normY * (PlotH - 1)), 0, PlotH - 1);
        for (int x = 0; x < PlotW; x++) pixels[py * PlotW + x] = c;
    }

    private void DrawVLine(Color[] pixels, float normX, Color c)
    {
        int px = Mathf.Clamp(Mathf.RoundToInt(normX * (PlotW - 1)), 0, PlotW - 1);
        for (int y = 0; y < PlotH; y++) pixels[y * PlotW + px] = c;
    }

    private int ToPixX(float dist, float maxDist) =>
        Mathf.Clamp(Mathf.RoundToInt(dist / maxDist * (PlotW - 1)), 0, PlotW - 1);

    private int ToPixY(float hp) =>
        Mathf.Clamp(Mathf.RoundToInt(hp / 100f * (PlotH - 1)), 0, PlotH - 1);

    // ── UI 헬퍼 ──────────────────────────────────────────────────────────────

    private void DrawLegend(float x, float y)
    {
        DrawColorBox(x,         y, new Color(1f, 0.4f, 0.4f));
        GUI.Label(new Rect(x + 14f, y, 55f, 16f), "Chase",  _labelStyle);
        DrawColorBox(x + 65f,   y, new Color(0.4f, 0.7f, 1f));
        GUI.Label(new Rect(x + 79f, y, 55f, 16f), "Evade",  _labelStyle);
        DrawColorBox(x + 130f,  y, new Color(1f, 0.8f, 0.2f));
        GUI.Label(new Rect(x + 144f, y, 60f, 16f), "Ambush", _labelStyle);
    }

    private void DrawColorBox(float x, float y, Color c)
    {
        GUI.DrawTexture(new Rect(x, y + 3f, 10f, 10f), MakeTex(c));
    }

    private static Color BranchColor(string branch) => branch switch
    {
        "Chase/Attack"  => new Color(1f, 0.4f, 0.4f),
        "Evade/Recover" => new Color(0.4f, 0.7f, 1f),
        "Ambush"        => new Color(1f, 0.8f, 0.2f),
        _               => Color.gray,
    };

    private static string ColorHex(Color c)
    {
        return $"{Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255):X2}" +
               $"{Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255):X2}" +
               $"{Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255):X2}";
    }

    private void InitStyles()
    {
        if (_boxStyle != null) return;

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = MakeTex(new Color(0f, 0f, 0f, 0.75f)) },
            padding = new RectOffset(10, 10, 8, 8),
        };
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal   = { textColor = Color.white },
            richText = true,
        };
        _headerStyle = new GUIStyle(_labelStyle)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.4f, 0.9f, 1f) },
        };
        _axisStyle = new GUIStyle(_labelStyle)
        {
            fontSize = 10,
            normal   = { textColor = new Color(0.6f, 0.6f, 0.6f) },
        };
    }

    private static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
