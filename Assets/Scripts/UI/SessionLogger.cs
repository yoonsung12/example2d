using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 플레이 세션마다 CSV 로그를 persistentDataPath/Logs/ 에 누적 저장합니다.
/// NFBTEnemyAI가 매초 Log()를 호출하고, 게임 종료 시 세션 요약을 기록합니다.
/// </summary>
public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance { get; private set; }

    // ── 내부 상태 ────────────────────────────────────────────────────────────
    private StreamWriter _writer;
    private float        _logInterval = 1f;
    private float        _logTimer;

    private int   _countChase;
    private int   _countEvade;
    private int   _countAmbush;
    private float _playStyleSum;
    private int   _playStyleSamples;

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        OpenFile();
    }

    private void OnApplicationQuit()
    {
        WriteSessionSummary();
        _writer?.Close();
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// NFBTEnemyAI.Update()에서 매 logInterval초마다 호출합니다.
    /// </summary>
    public void TryLog(float deltaTime,
                       float distance,
                       float playerHP,
                       string activeBranch,
                       float playStyleScore,
                       float hpLow,
                       float hpHigh,
                       float distNear,
                       float distFar)
    {
        _logTimer -= deltaTime;
        if (_logTimer > 0f) return;
        _logTimer = _logInterval;

        Log(distance, playerHP, activeBranch, playStyleScore, hpLow, hpHigh, distNear, distFar);
    }

    // ── 내부 처리 ─────────────────────────────────────────────────────────────

    private void Log(float distance,
                     float playerHP,
                     string activeBranch,
                     float playStyleScore,
                     float hpLow,
                     float hpHigh,
                     float distNear,
                     float distFar)
    {
        if (_writer == null) return;

        // 통계 집계
        switch (activeBranch)
        {
            case "Chase/Attack":  _countChase++;  break;
            case "Evade/Recover": _countEvade++;  break;
            case "Ambush":        _countAmbush++; break;
        }
        _playStyleSum += playStyleScore;
        _playStyleSamples++;

        // CSV 행 기록
        _writer.WriteLine(
            $"{Time.time:F2}," +
            $"{distance:F2}," +
            $"{playerHP:F1}," +
            $"{activeBranch}," +
            $"{playStyleScore:F4}," +
            $"{hpLow:F1}," +
            $"{hpHigh:F1}," +
            $"{distNear:F2}," +
            $"{distFar:F2}");
    }

    private void OpenFile()
    {
        string dir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(dir);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string path      = Path.Combine(dir, $"session_{timestamp}.csv");

        _writer = new StreamWriter(path, append: false, encoding: Encoding.UTF8);
        _writer.WriteLine("Time,Distance,PlayerHP,ActiveBranch,PlayStyleScore," +
                          "HPLow,HPHigh,DistNear,DistFar");
        _writer.AutoFlush = true;

        Debug.Log($"[SessionLogger] 로그 저장 경로: {path}");
    }

    private void WriteSessionSummary()
    {
        if (_writer == null) return;

        float avgStyle = _playStyleSamples > 0
            ? _playStyleSum / _playStyleSamples
            : 0f;

        _writer.WriteLine(
            $"# 세션 요약 | " +
            $"Chase:{_countChase} Evade:{_countEvade} Ambush:{_countAmbush} | " +
            $"AvgPlayStyle:{avgStyle:F4} | " +
            $"총 샘플:{_playStyleSamples}");
    }
}
