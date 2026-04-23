using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 플레이 세션마다 CSV 로그를 persistentDataPath/Logs/ 에 누적 저장합니다.
/// NFBTEnemyAI가 매초 TryLog()를 호출하고, 게임 종료 시 세션 요약을 기록합니다.
/// </summary>
public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance { get; private set; } // 씬 전반에서 접근 가능한 싱글턴

    private StreamWriter _writer;              // CSV 파일 스트림
    private float        _logInterval = 1f;    // 로그 기록 간격 (초)
    private float        _logTimer;            // 다음 로그까지 남은 시간

    // 분기별 선택 횟수 통계
    private int _countChase;   // Chase/Attack 선택 횟수
    private int _countEvade;   // Evade/Recover 선택 횟수
    private int _countCounter; // Counter 선택 횟수

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; } // 중복 싱글턴 제거
        Instance = this;                                                              // 싱글턴 등록
        DontDestroyOnLoad(gameObject);                                               // 씬 전환 시 유지
        OpenFile();                                                                   // CSV 파일 열기
    }

    private void OnApplicationQuit()
    {
        WriteSessionSummary(); // 종료 시 세션 요약 기록
        _writer?.Close();      // 파일 닫기
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// NFBTEnemyAI.Update()에서 매 logInterval초마다 호출합니다.
    /// </summary>
    public void TryLog(float deltaTime,
                       string activeBranch,
                       float attackFreq,
                       float hitRate,
                       float damagePerSec,
                       int   clusterIndex)
    {
        _logTimer -= deltaTime;           // 타이머 감소
        if (_logTimer > 0f) return;       // 아직 간격이 안 됐으면 무시
        _logTimer = _logInterval;         // 타이머 리셋

        Log(activeBranch, attackFreq, hitRate, damagePerSec, clusterIndex); // 실제 기록
    }

    // ── 내부 처리 ─────────────────────────────────────────────────────────────

    private void Log(string activeBranch,
                     float  attackFreq,
                     float  hitRate,
                     float  damagePerSec,
                     int    clusterIndex)
    {
        if (_writer == null) return; // 파일이 열려있지 않으면 무시

        // 분기별 통계 집계
        switch (activeBranch)
        {
            case "Chase/Attack":  _countChase++;   break; // 추격 분기 횟수 증가
            case "Evade/Recover": _countEvade++;   break; // 회피 분기 횟수 증가
            case "Counter":       _countCounter++; break; // 카운터 분기 횟수 증가
        }

        // CSV 행 기록: 시간, 분기, 3개 피처, 클러스터 인덱스
        _writer.WriteLine(
            $"{Time.time:F2}," +
            $"{activeBranch}," +
            $"{attackFreq:F4}," +
            $"{hitRate:F4}," +
            $"{damagePerSec:F4}," +
            $"{clusterIndex}");
    }

    private void OpenFile()
    {
        string dir = Path.Combine(Application.persistentDataPath, "Logs"); // 로그 디렉토리 경로
        Directory.CreateDirectory(dir);                                      // 디렉토리 없으면 생성

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");   // 타임스탬프
        string path      = Path.Combine(dir, $"session_{timestamp}.csv");  // 파일 경로

        _writer = new StreamWriter(path, append: false, encoding: Encoding.UTF8); // UTF-8 파일 생성
        _writer.WriteLine("Time,ActiveBranch,AttackFreq,HitRate,DamagePerSec,ClusterIndex"); // CSV 헤더
        _writer.AutoFlush = true; // 즉시 기록 (버퍼 미사용)

        Debug.Log($"[SessionLogger] 로그 저장 경로: {path}");
    }

    private void WriteSessionSummary()
    {
        if (_writer == null) return; // 파일 없으면 무시

        int total = _countChase + _countEvade + _countCounter; // 전체 샘플 수

        // 세션 요약 행 기록
        _writer.WriteLine(
            $"# 세션 요약 | " +
            $"Chase:{_countChase} Evade:{_countEvade} Counter:{_countCounter} | " +
            $"총 샘플:{total}");
    }
}
