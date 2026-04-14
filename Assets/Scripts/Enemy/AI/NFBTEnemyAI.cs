using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// NFBT (Neuro-Fuzzy Behavior Tree) 적 AI 컨트롤러.
/// Layer1(RBFN) → Layer2(Fuzzy) 로 ActiveBranch를 결정하고,
/// Layer3 실행은 BehaviorGraphAgent에 위임합니다.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(BehaviorGraphAgent))]
public class NFBTEnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float     _detectionRange = 10f;
    [SerializeField] private float     _attackRange    = 1.5f;

    [Header("FCM")]
    [SerializeField] private float _fcmUpdateInterval = 30f;

    // ── 공개 프로퍼티 (BT 노드에서 참조) ────────────────────────────────────
    public EnemyBase Enemy           { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public float     AttackRange     => _attackRange;
    public float     DetectionRange  => _detectionRange;

    /// <summary>현재 선택된 BT 분기명. BT 노드에서 참조합니다.</summary>
    public string ActiveBranch { get; private set; } = "None";

    // ── NFBT 계층 ────────────────────────────────────────────────────────────
    private RBFNetwork      _rbfn;
    private FCMClusterer    _fcm;
    private FuzzyRuleEngine _fuzzy;

    // ── FCM 샘플 수집 ────────────────────────────────────────────────────────
    private readonly List<float> _hpSamples   = new();
    private readonly List<float> _distSamples = new();
    private float _fcmTimer;

    // ── PlayStyle_Score ───────────────────────────────────────────────────────
    private float _playStyleScore = 0.5f;

    // 교전 기록 중복 방지 플래그
    private bool _engagementRecorded;

    // ── 디버그 프로퍼티 (AIDebugDisplay / AIScatterPlotUI에서 참조) ───────────
    public float  DbgPlayStyle  { get; private set; } = 0.5f;
    public float  DbgSBaseA     { get; private set; }
    public float  DbgSBaseB     { get; private set; }
    public float  DbgSBaseC     { get; private set; }
    public float  DbgSFuzzyA    { get; private set; }
    public float  DbgSFuzzyB    { get; private set; }
    public float  DbgSFuzzyC    { get; private set; }
    public float  DbgUFinalA    { get; private set; }
    public float  DbgUFinalB    { get; private set; }
    public float  DbgUFinalC    { get; private set; }
    public string DbgBranch     { get; private set; } = "None";
    public float  DbgDist       { get; private set; }

    // FCM 임계값 (산점도 기준선 / FCM 패널용)
    public float DbgHPLow       => _fcm.HPLow;
    public float DbgHPMedium    => _fcm.HPMedium;
    public float DbgHPHigh      => _fcm.HPHigh;
    public float DbgDistNear    => _fcm.DistNear;
    public float DbgDistFar     => _fcm.DistFar;
    public float DbgFCMLastTime { get; private set; }

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        Enemy  = GetComponent<EnemyBase>();
        _rbfn  = new RBFNetwork();
        _fcm   = new FCMClusterer();
        _fuzzy = new FuzzyRuleEngine(_fcm);
    }

    private void Start()
    {
        // 플레이어 참조
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) PlayerTransform = pc.transform;

        // Layer1: 세션 시작 시 1회 PlayStyle_Score 계산
        if (PlayerBehaviorTracker.Instance != null)
            _playStyleScore = _rbfn.Compute(PlayerBehaviorTracker.Instance.GetInputVector());

        _fcmTimer = _fcmUpdateInterval;
    }

    private void Update()
    {
        if (Enemy.IsDead || PlayerTransform == null) return;

        float dist     = Vector2.Distance(transform.position, PlayerTransform.position);
        float playerHP = GetPlayerHP();

        // 탐지 범위 밖 → 정지
        if (dist > _detectionRange)
        {
            Enemy.Movement?.Move(0f);
            ActiveBranch = "None";
            return;
        }

        // FCM 샘플 수집 (최대 500개)
        if (_hpSamples.Count  < 500) _hpSamples.Add(playerHP);
        if (_distSamples.Count < 500) _distSamples.Add(dist);

        // Layer2: 퍼지 규칙 평가
        FuzzyRuleEngine.FuzzyResult fr = _fuzzy.Evaluate(_playStyleScore, playerHP, dist);

        // S_base: 기본 유틸리티 (삼각 상황 판단)
        float sBaseChase  = Mathf.Clamp01(1f - dist / _detectionRange);
        float sBaseEvade  = Mathf.Clamp01(1f - Enemy.CurrentHealth / Enemy.MaxHealth);
        float sBaseAmbush = Mathf.Clamp01(dist / _detectionRange * 0.8f);

        // S_fuzzy: 퍼지 규칙 결과 (Branch A, B, C 순)
        float sFuzzyA = Mathf.Max(fr.UtilityChase, fr.UtilityRush);
        float sFuzzyB = fr.UtilityEvade;
        float sFuzzyC = fr.UtilityChase;

        // U_final 계산 및 분기 선택 (Layer3 진입점)
        float uA = 0.7f * sBaseChase  + 0.3f * sFuzzyA;
        float uB = 0.7f * sBaseEvade  + 0.3f * sFuzzyB;
        float uC = 0.7f * sBaseAmbush + 0.3f * sFuzzyC;

        if      (uA >= uB && uA >= uC) ActiveBranch = "Chase/Attack";
        else if (uB >= uA && uB >= uC) ActiveBranch = "Evade/Recover";
        else                           ActiveBranch = "Ambush";

        // 디버그 값 갱신
        DbgPlayStyle = _playStyleScore;
        DbgSBaseA    = sBaseChase;  DbgSFuzzyA = sFuzzyA;
        DbgSBaseB    = sBaseEvade;  DbgSFuzzyB = sFuzzyB;
        DbgSBaseC    = sBaseAmbush; DbgSFuzzyC = sFuzzyC;
        DbgUFinalA   = uA;
        DbgUFinalB   = uB;
        DbgUFinalC   = uC;
        DbgBranch    = ActiveBranch;
        DbgDist      = dist;

        // 세션 로그
        SessionLogger.Instance?.TryLog(
            Time.deltaTime,
            dist,
            playerHP,
            ActiveBranch,
            _playStyleScore,
            _fcm.HPLow,
            _fcm.HPHigh,
            _fcm.DistNear,
            _fcm.DistFar);

        // 교전 기록 (Chase/Attack 분기 최초 선택 시 1회)
        if (!_engagementRecorded && ActiveBranch == "Chase/Attack")
        {
            _engagementRecorded = true;
            var room = Enemy.HomeRoom;
            if (room?.CurrentRecord != null)
                PlayerBehaviorTracker.Instance?.RecordRoomEngagement(room.CurrentRecord);
        }
        // Layer3 실행은 BehaviorGraphAgent가 자체 업데이트에서 처리합니다.
    }

    private void FixedUpdate()
    {
        if (Enemy.IsDead) return;

        // FCM 주기적 임계값 갱신
        _fcmTimer -= Time.fixedDeltaTime;
        if (_fcmTimer <= 0f)
        {
            // 갱신 전 값 스냅샷
            float prevHPLow    = _fcm.HPLow;
            float prevHPMed    = _fcm.HPMedium;
            float prevHPHigh   = _fcm.HPHigh;
            float prevDistNear = _fcm.DistNear;
            float prevDistFar  = _fcm.DistFar;

            _fcm.UpdateHPClusters(_hpSamples);
            _fcm.UpdateDistanceClusters(_distSamples);
            _hpSamples.Clear();
            _distSamples.Clear();
            _fcmTimer    = _fcmUpdateInterval;
            DbgFCMLastTime = Time.time;

            Debug.Log(
                $"[FCM 갱신] t={Time.time:F1}s\n" +
                $"  HP    Low  {prevHPLow:F1} → {_fcm.HPLow:F1}\n" +
                $"  HP    Med  {prevHPMed:F1} → {_fcm.HPMedium:F1}\n" +
                $"  HP    High {prevHPHigh:F1} → {_fcm.HPHigh:F1}\n" +
                $"  Dist  Near {prevDistNear:F2} → {_fcm.DistNear:F2}\n" +
                $"  Dist  Far  {prevDistFar:F2} → {_fcm.DistFar:F2}");
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private float GetPlayerHP()
    {
        var cb = PlayerTransform?.GetComponent<CharacterBase>();
        return cb != null ? cb.CurrentHealth : 100f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
