using Unity.Behavior;
using UnityEngine;

/// <summary>
/// NFBT (Neuro-Fuzzy Behavior Tree) 적 AI 컨트롤러.
/// 각 적은 자신의 CombatStatsTracker → RBFN 파이프라인으로 독립적으로 분기를 결정합니다.
/// 클러스터 0=방어형→Chase/Attack / 1=균형형→Evade/Recover / 2=공격형→Counter
/// 감지 범위 밖 → Patrol (이동은 PatrolBTAction이 담당)
/// </summary>
[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(BehaviorGraphAgent))]
[RequireComponent(typeof(CombatStatsTracker))]
public class NFBTEnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float _detectionRange = 10f; // 적이 플레이어를 감지하는 반경
    [SerializeField] private float _attackRange    = 1.5f; // 적의 공격 가능 반경

    [Header("Patrol")]
    [SerializeField] private float     _patrolHalfWidth = 3f;  // 스폰 위치 기준 좌우 패트롤 범위 절반
    [SerializeField] private float     _edgeCheckDist   = 0.5f; // 낭떠러지 전방 감지 거리
    [SerializeField] private LayerMask _groundLayer;             // 지형 레이어 마스크

    private float _startX; // 스폰 시점 X 좌표 (패트롤 경계 기준점)

    // ── 공개 프로퍼티 (BT 노드에서 참조) ─────────────────────────────────────
    public EnemyBase Enemy           { get; private set; } // 적 기본 컴포넌트
    public Transform PlayerTransform { get; private set; } // 플레이어 트랜스폼
    public float     AttackRange     => _attackRange;       // 공격 범위 (읽기 전용)
    public float     DetectionRange  => _detectionRange;    // 감지 범위 (읽기 전용)

    public float     PatrolLeftX   => _startX - _patrolHalfWidth; // 순찰 좌측 경계 X (PatrolBTAction 참조)
    public float     PatrolRightX  => _startX + _patrolHalfWidth; // 순찰 우측 경계 X (PatrolBTAction 참조)
    public float     EdgeCheckDist => _edgeCheckDist;              // 낭떠러지 체크 거리 (PatrolBTAction 참조)
    public LayerMask GroundLayer   => _groundLayer;                // 지형 레이어 (PatrolBTAction 참조)

    /// <summary>현재 선택된 BT 분기명 (BT 노드에서 참조)</summary>
    public string ActiveBranch { get; private set; } = "Patrol";

    // ── NFBT 계층 ────────────────────────────────────────────────────────────
    private RBFNetwork          _rbfn;    // 클러스터 인덱스 분류기
    private CombatStatsTracker  _tracker; // 이 적 전용 전투 통계 트래커

    /// <summary>이 적의 전투 통계 트래커 (AIDebugDisplay 참조용)</summary>
    public CombatStatsTracker Tracker => _tracker;

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
    public float  DbgAttackFreq   { get; private set; }            // 현재 attack_frequency
    public float  DbgHitRate      { get; private set; }            // 현재 hit_rate
    public float  DbgDamagePerSec { get; private set; }            // 현재 damage_per_sec
    public int    DbgClusterIndex { get; private set; }            // RBFN 출력 클러스터 인덱스
    public string DbgBranch       { get; private set; } = "Patrol"; // 현재 분기명
    public float  DbgDist         { get; private set; }            // 플레이어까지 거리

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        Enemy    = GetComponent<EnemyBase>();        // 적 컴포넌트 캐싱
        _rbfn    = new RBFNetwork();                 // RBFN 생성
        _tracker = GetComponent<CombatStatsTracker>(); // 이 적 전용 트래커 캐싱
        _startX  = transform.position.x;             // 스폰 위치 X 저장
    }

    private void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>(); // 씬에서 플레이어 컨트롤러 탐색
        if (pc != null) PlayerTransform = pc.transform;     // 플레이어 트랜스폼 캐싱
        _tracker.Initialize(this);                          // 플레이어 참조 확보 후 트래커 초기화
    }

    private void Update()
    {
        if (Enemy.IsDead || PlayerTransform == null) return; // 사망 또는 플레이어 없으면 무시

        float dist = Vector2.Distance(transform.position, PlayerTransform.position); // 플레이어까지 거리

        // 감지 범위 밖이면 순찰 분기 유지 (이동은 PatrolBTAction이 담당)
        if (dist > _detectionRange)
        {
            ActiveBranch = "Patrol"; // Patrol 분기로 전환
            return;
        }

        // 이 적 전용 트래커에서 피처 벡터 획득 (다른 적과 독립적)
        float[] features = _tracker.GetFeatureVector();

        int clusterIndex = _rbfn.Compute(features);    // RBFN으로 클러스터 분류
        ActiveBranch = BranchNames[clusterIndex];       // 클러스터 인덱스 → 분기명 변환

        // 디버그 값 갱신
        DbgAttackFreq   = features[0];  // attack_frequency
        DbgHitRate      = features[1];  // hit_rate
        DbgDamagePerSec = features[2];  // damage_per_sec
        DbgClusterIndex = clusterIndex; // 클러스터 인덱스
        DbgBranch       = ActiveBranch; // 현재 분기명
        DbgDist         = dist;         // 플레이어 거리

        // 세션 로그 기록
        SessionLogger.Instance?.TryLog(
            Time.deltaTime,
            ActiveBranch,
            features[0],
            features[1],
            features[2],
            clusterIndex);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange); // 감지 범위 시각화

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange); // 공격 범위 시각화

        // 패트롤 범위를 청록색 선으로 표시
        float baseX = Application.isPlaying ? _startX : transform.position.x;
        float y     = transform.position.y;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(baseX - _patrolHalfWidth, y), new Vector3(baseX + _patrolHalfWidth, y)); // 패트롤 구간 수평선
        Gizmos.DrawWireSphere(new Vector3(baseX - _patrolHalfWidth, y), 0.15f); // 왼쪽 경계 표시
        Gizmos.DrawWireSphere(new Vector3(baseX + _patrolHalfWidth, y), 0.15f); // 오른쪽 경계 표시
    }
}
