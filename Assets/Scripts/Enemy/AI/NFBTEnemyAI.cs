using Unity.Behavior;
using UnityEngine;

/// <summary>
/// NFBT (Neuro-Fuzzy Behavior Tree) 적 AI 컨트롤러.
/// CombatStatsTracker → FCM → RBFN 파이프라인으로 플레이어 전투 스타일을 분류하고
/// 클러스터 인덱스 기반으로 ActiveBranch를 결정합니다.
/// 클러스터 0=방어형→Chase/Attack / 1=균형형→Evade/Recover / 2=공격형→Counter
/// </summary>
[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(BehaviorGraphAgent))]
public class NFBTEnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float _detectionRange = 10f; // 적이 플레이어를 감지하는 반경
    [SerializeField] private float _attackRange    = 1.5f; // 적의 공격 가능 반경

    [Header("FCM")]
    [SerializeField] private float _sampleInterval    = 5f;  // 피처 벡터 샘플링 간격 (초)
    [SerializeField] private float _fcmUpdateInterval = 30f; // FCM 센터 갱신 간격 (초)

    // ── 공개 프로퍼티 (BT 노드에서 참조) ─────────────────────────────────────
    public EnemyBase Enemy           { get; private set; } // 적 기본 컴포넌트
    public Transform PlayerTransform { get; private set; } // 플레이어 트랜스폼
    public float     AttackRange     => _attackRange;       // 공격 범위 (읽기 전용)
    public float     DetectionRange  => _detectionRange;    // 감지 범위 (읽기 전용)

    /// <summary>현재 선택된 BT 분기명 (BT 노드에서 참조)</summary>
    public string ActiveBranch { get; private set; } = "None";

    // ── NFBT 계층 ────────────────────────────────────────────────────────────
    private RBFNetwork   _rbfn; // 클러스터 인덱스 분류기
    private FCMClusterer _fcm;  // 실시간 피처 클러스터링

    // ── 타이머 ────────────────────────────────────────────────────────────────
    private float _sampleTimer; // 다음 샘플링까지 남은 시간
    private float _fcmTimer;    // 다음 FCM 갱신까지 남은 시간

    // ── 분기 이름 매핑 (클러스터 인덱스 → 분기명) ────────────────────────────
    // 인덱스 0: 방어형 플레이어 → 적이 압박 (Chase/Attack)
    // 인덱스 1: 균형형 플레이어 → 적이 회피 (Evade/Recover)
    // 인덱스 2: 공격형 플레이어 → 적이 카운터 (Counter)
    private static readonly string[] BranchNames =
    {
        "Chase/Attack",   // 클러스터 0
        "Evade/Recover",  // 클러스터 1
        "Counter",        // 클러스터 2
    };

    // ── 디버그 프로퍼티 (AIDebugDisplay / AIScatterPlotUI에서 참조) ───────────
    public float     DbgAttackFreq   { get; private set; }       // 현재 attack_frequency
    public float     DbgHitRate      { get; private set; }       // 현재 hit_rate
    public float     DbgDamagePerSec { get; private set; }       // 현재 damage_per_sec
    public int       DbgClusterIndex { get; private set; }       // RBFN 출력 클러스터 인덱스
    public string    DbgBranch       { get; private set; } = "None"; // 현재 분기명
    public float     DbgDist         { get; private set; }       // 플레이어까지 거리
    public float     DbgFCMLastTime  { get; private set; }       // 마지막 FCM 갱신 시각
    public float[][] DbgCenters      { get; private set; }       // FCM 클러스터 중심 (시각화용)

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        Enemy      = GetComponent<EnemyBase>(); // 적 컴포넌트 캐싱
        _rbfn      = new RBFNetwork();          // RBFN 생성
        _fcm       = new FCMClusterer();        // FCM 생성
        DbgCenters = _fcm.GetCenters();         // 초기 기본 센터 저장
    }

    private void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>(); // 씬에서 플레이어 컨트롤러 탐색
        if (pc != null) PlayerTransform = pc.transform;     // 플레이어 트랜스폼 캐싱

        _sampleTimer = _sampleInterval;    // 첫 샘플링 타이머 초기화
        _fcmTimer    = _fcmUpdateInterval; // 첫 FCM 갱신 타이머 초기화
    }

    private void Update()
    {
        if (Enemy.IsDead || PlayerTransform == null) return; // 사망 또는 플레이어 없으면 무시

        float dist = Vector2.Distance(transform.position, PlayerTransform.position); // 플레이어까지 거리

        // 감지 범위 밖이면 정지
        if (dist > _detectionRange)
        {
            Enemy.Movement?.Move(0f); // 이동 정지
            ActiveBranch = "None";    // 분기 초기화
            return;
        }

        var tracker = CombatStatsTracker.Instance; // 전투 통계 트래커 참조
        if (tracker == null) return;                // 트래커 없으면 무시

        float[] features = tracker.GetFeatureVector(); // 현재 3D 피처 벡터 취득

        int clusterIndex = _rbfn.Compute(features);    // RBFN으로 클러스터 분류
        ActiveBranch = BranchNames[clusterIndex];       // 클러스터 인덱스 → 분기명 변환

        // 디버그 값 갱신
        DbgAttackFreq   = features[0];    // attack_frequency
        DbgHitRate      = features[1];    // hit_rate
        DbgDamagePerSec = features[2];    // damage_per_sec
        DbgClusterIndex = clusterIndex;   // 클러스터 인덱스
        DbgBranch       = ActiveBranch;   // 현재 분기명
        DbgDist         = dist;           // 플레이어 거리

        // 세션 로그 기록
        SessionLogger.Instance?.TryLog(
            Time.deltaTime,
            ActiveBranch,
            features[0],
            features[1],
            features[2],
            clusterIndex);
    }

    private void FixedUpdate()
    {
        if (Enemy.IsDead) return; // 사망 시 무시

        var tracker = CombatStatsTracker.Instance;
        if (tracker == null) return;

        // 피처 벡터 샘플링 (FCM 입력용)
        _sampleTimer -= Time.fixedDeltaTime;
        if (_sampleTimer <= 0f)
        {
            _fcm.AddSample(tracker.GetFeatureVector()); // 현재 피처 벡터를 FCM 샘플로 추가
            _sampleTimer = _sampleInterval;              // 타이머 리셋
        }

        // FCM 갱신 및 RBFN 센터 업데이트
        _fcmTimer -= Time.fixedDeltaTime;
        if (_fcmTimer <= 0f)
        {
            float[][] newCenters = _fcm.GetCenters(); // FCM 실행 → 클러스터 중심 취득
            _rbfn.SetCenters(newCenters);              // RBFN 센터 갱신
            DbgCenters     = newCenters;               // 디버그용 센터 저장
            DbgFCMLastTime = Time.time;                // 마지막 갱신 시각 기록
            _fcm.ClearSamples();                       // 샘플 초기화
            _fcmTimer = _fcmUpdateInterval;            // 타이머 리셋

            Debug.Log(
                $"[FCM 갱신] t={Time.time:F1}s | " +
                $"C0(방어)={Vec3Str(newCenters[0])} " +
                $"C1(균형)={Vec3Str(newCenters[1])} " +
                $"C2(공격)={Vec3Str(newCenters[2])}");
        }
    }

    // float[3] 배열을 "[ x, y, z ]" 형식 문자열로 변환 (디버그 출력용)
    private static string Vec3Str(float[] v) =>
        $"[{v[0]:F2},{v[1]:F2},{v[2]:F2}]";

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;                                    // 감지 범위: 노란색
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;                                       // 공격 범위: 빨간색
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
