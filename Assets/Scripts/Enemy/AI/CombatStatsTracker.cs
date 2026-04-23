using UnityEngine;

/// <summary>
/// 플레이어의 실시간 전투 통계를 수집합니다.
/// FCMClusterer 샘플 공급 및 RBFNetwork 입력 벡터를 제공합니다.
/// 피처: [attack_frequency, hit_rate, damage_taken_per_sec] (모두 정규화 [0,1])
/// </summary>
public class CombatStatsTracker : MonoBehaviour
{
    public static CombatStatsTracker Instance { get; private set; } // 씬 전반에서 접근 가능한 싱글턴

    [Header("정규화 최대값 (게임에 맞게 튜닝)")]
    [SerializeField] private float _maxAttackFreq   = 5f;  // 초당 최대 공격 횟수 기준
    [SerializeField] private float _maxDamagePerSec = 50f; // 초당 최대 피해량 기준

    private float _sessionStart; // 세션 시작 시각 (Time.time 기준)
    private int   _attackCount;  // 세션 누적 공격 횟수
    private int   _hitCount;     // 세션 누적 명중 횟수
    private float _totalDamage;  // 세션 누적 받은 피해량

    // ── 정규화된 피처 [0,1] ─────────────────────────────────────────────────

    /// <summary>초당 공격 횟수 (정규화 [0,1])</summary>
    public float AttackFrequency =>
        Mathf.Clamp01(_attackCount / (SessionTime * _maxAttackFreq)); // 누적 공격 수 / (경과시간 × 최대값)

    /// <summary>공격 명중률 [0,1]</summary>
    public float HitRate =>
        _attackCount == 0 ? 0f : Mathf.Clamp01((float)_hitCount / _attackCount); // 명중 수 / 총 공격 수

    /// <summary>초당 받은 피해 (정규화 [0,1])</summary>
    public float DamagePerSec =>
        Mathf.Clamp01(_totalDamage / (SessionTime * _maxDamagePerSec)); // 총 피해 / (경과시간 × 최대값)

    /// <summary>FCM / RBFN 입력용 3D 피처 벡터 반환</summary>
    public float[] GetFeatureVector() =>
        new[] { AttackFrequency, HitRate, DamagePerSec }; // 3개 피처를 배열로 반환

    private float SessionTime => Mathf.Max(1f, Time.time - _sessionStart); // 0 나누기 방지용 최소 1초

    // ── Unity 생명주기 ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; } // 중복 싱글턴 제거
        Instance = this;                                         // 싱글턴 등록
        DontDestroyOnLoad(gameObject);                          // 씬 전환 시 유지
        _sessionStart = Time.time;                               // 세션 시작 시각 기록
    }

    private void Start()
    {
        SubscribeToPlayer(); // 플레이어 이벤트 구독
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayer(); // 이벤트 구독 해제 (메모리 누수 방지)
    }

    // ── 이벤트 구독 / 해제 ───────────────────────────────────────────────────

    private void SubscribeToPlayer()
    {
        var player = Player.Instance; // 플레이어 싱글턴 참조
        if (player == null) return;   // 플레이어 없으면 무시

        if (player.Combat != null)
        {
            player.Combat.OnAttackStarted += OnAttackStarted; // 공격 시작 이벤트 구독
            player.Combat.OnHitDealt      += OnHitDealt;      // 명중 이벤트 구독
        }
        player.OnDamageTaken += OnDamageTaken; // 피해 이벤트 구독
    }

    private void UnsubscribeFromPlayer()
    {
        var player = Player.Instance;
        if (player == null) return;

        if (player.Combat != null)
        {
            player.Combat.OnAttackStarted -= OnAttackStarted; // 공격 이벤트 구독 해제
            player.Combat.OnHitDealt      -= OnHitDealt;      // 명중 이벤트 구독 해제
        }
        player.OnDamageTaken -= OnDamageTaken; // 피해 이벤트 구독 해제
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────────

    private void OnAttackStarted()      => _attackCount++;     // 공격 횟수 1 증가
    private void OnHitDealt()           => _hitCount++;        // 명중 횟수 1 증가
    private void OnDamageTaken(float d) => _totalDamage += d;  // 받은 피해 누산
}
