using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NFBT (Neuro-Fuzzy Behavior Tree) 적 AI 컨트롤러.
/// Layer1(RBFN) → Layer2(Fuzzy) → Layer3(BT) 순으로 매 프레임 실행됩니다.
/// 기존 EnemyAI.cs 를 대체합니다.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
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

    // ── NFBT 계층 ────────────────────────────────────────────────────────────
    private RBFNetwork           _rbfn;
    private FCMClusterer         _fcm;
    private FuzzyRuleEngine      _fuzzy;
    private UtilityFuzzySelector _utilitySelector;

    // ── FCM 샘플 수집 ────────────────────────────────────────────────────────
    private readonly List<float> _hpSamples   = new();
    private readonly List<float> _distSamples = new();
    private float _fcmTimer;

    // ── PlayStyle_Score ───────────────────────────────────────────────────────
    private float _playStyleScore = 0.5f;

    // 교전 기록 중복 방지 플래그
    private bool _engagementRecorded;

    // ── 디버그 프로퍼티 (AIDebugDisplay에서 참조) ─────────────────────────────
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

        _utilitySelector = BuildBehaviorTree();
        _fcmTimer        = _fcmUpdateInterval;
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

        _utilitySelector.UpdateUtilities(
            sBaseValues:  new[] { sBaseChase, sBaseEvade, sBaseAmbush },
            sFuzzyValues: new[] { sFuzzyA,    sFuzzyB,    sFuzzyC    }
        );

        // 디버그 값 갱신
        DbgPlayStyle = _playStyleScore;
        DbgSBaseA    = sBaseChase;  DbgSFuzzyA = sFuzzyA;
        DbgSBaseB    = sBaseEvade;  DbgSFuzzyB = sFuzzyB;
        DbgSBaseC    = sBaseAmbush; DbgSFuzzyC = sFuzzyC;
        DbgUFinalA   = 0.7f * sBaseChase  + 0.3f * sFuzzyA;
        DbgUFinalB   = 0.7f * sBaseEvade  + 0.3f * sFuzzyB;
        DbgUFinalC   = 0.7f * sBaseAmbush + 0.3f * sFuzzyC;
        DbgBranch    = _utilitySelector.ActiveBranchName;
        DbgDist      = dist;

        // 교전 기록 (Chase/Attack 분기 최초 선택 시 1회)
        if (!_engagementRecorded && _utilitySelector.ActiveBranchName == "Chase/Attack")
        {
            _engagementRecorded = true;
            var room = Enemy.HomeRoom;
            if (room?.CurrentRecord != null)
                PlayerBehaviorTracker.Instance?.RecordRoomEngagement(room.CurrentRecord);
        }

        // Layer3: BT 실행
        _utilitySelector.Evaluate();
    }

    private void FixedUpdate()
    {
        if (Enemy.IsDead) return;

        // FCM 주기적 임계값 갱신
        _fcmTimer -= Time.fixedDeltaTime;
        if (_fcmTimer <= 0f)
        {
            _fcm.UpdateHPClusters(_hpSamples);
            _fcm.UpdateDistanceClusters(_distSamples);
            _hpSamples.Clear();
            _distSamples.Clear();
            _fcmTimer = _fcmUpdateInterval;
        }
    }

    // ── BT 구성 ─────────────────────────────────────────────────────────────

    private UtilityFuzzySelector BuildBehaviorTree()
    {
        // Branch A: 추격 → 전투
        var branchA = new BTSequence(this, new List<BTNode>
        {
            new MoveToPlayerAction(this),
            new AttackPlayerAction(this),
        });

        // Branch B: 회피 → 관찰
        var branchB = new BTSequence(this, new List<BTNode>
        {
            new MoveToSafeAction(this),
            new ObservePlayerAction(this, 2f),
        });

        // Branch C: 매복 이동 → 공격
        var branchC = new BTSequence(this, new List<BTNode>
        {
            new MoveToAmbushAction(this),
            new AttackPlayerAction(this, extraCooldown: 0.5f),
        });

        return new UtilityFuzzySelector(this, new List<UtilityFuzzySelector.Branch>
        {
            new() { Name = "Chase/Attack", Node = branchA, SBase = 0.5f, SFuzzy = 0.5f },
            new() { Name = "Evade/Recover", Node = branchB, SBase = 0.3f, SFuzzy = 0.3f },
            new() { Name = "Ambush",        Node = branchC, SBase = 0.4f, SFuzzy = 0.4f },
        });
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
