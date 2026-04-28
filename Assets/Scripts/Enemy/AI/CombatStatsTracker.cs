using UnityEngine;

/// <summary>
/// 각 적이 독립적으로 소유하는 전투 통계 트래커.
/// 이 적과의 상호작용만 측정하여 RBFN 입력 벡터를 제공합니다.
/// 피처: [attack_frequency, hit_rate, damage_per_sec]
/// - attack_frequency: 이 적의 감지 범위 내에서 플레이어가 공격한 빈도
/// - hit_rate: 이 적이 실제로 맞은 횟수 / 이 적 근처 공격 횟수
/// - damage_per_sec: 이 적이 플레이어에게 가한 초당 피해량
/// </summary>
public class CombatStatsTracker : MonoBehaviour
{
    [Header("정규화 최대값 (게임에 맞게 튜닝)")]
    [SerializeField] private float _maxAttackFreq   = 2.5f; // 초당 최대 공격 횟수 기준
    [SerializeField] private float _maxDamagePerSec = 40f;  // 초당 최대 피해량 기준

    private NFBTEnemyAI _ownerAI;    // 이 트래커를 소유하는 적 AI
    private float       _sessionStart; // 초기화 시각
    private int         _attackCount;  // 이 적 감지 범위 내 플레이어 공격 횟수
    private int         _hitCount;     // 이 적이 플레이어에게 맞은 횟수
    private float       _totalDamage;  // 이 적이 플레이어에게 가한 누적 피해량

    // ── 정규화된 피처 [0,1] ──────────────────────────────────────────────────

    /// <summary>감지 범위 내 플레이어 초당 공격 횟수 (정규화 [0,1])</summary>
    public float AttackFrequency =>
        Mathf.Clamp01(_attackCount / (SessionTime * _maxAttackFreq)); // 범위 내 공격 빈도

    /// <summary>이 적의 명중률 [0,1] (이 적이 맞은 횟수 / 범위 내 공격 횟수)</summary>
    public float HitRate =>
        _attackCount == 0 ? 0f : Mathf.Clamp01((float)_hitCount / _attackCount); // 이 적 명중률

    /// <summary>이 적이 플레이어에게 가한 초당 피해 (정규화 [0,1])</summary>
    public float DamagePerSec =>
        Mathf.Clamp01(_totalDamage / (SessionTime * _maxDamagePerSec)); // 이 적의 초당 피해

    /// <summary>RBFN 입력용 3D 피처 벡터 반환</summary>
    public float[] GetFeatureVector() =>
        new[] { AttackFrequency, HitRate, DamagePerSec }; // 3개 피처를 배열로 반환

    private float SessionTime => Mathf.Max(1f, Time.time - _sessionStart); // 0 나누기 방지

    // ── 초기화 (NFBTEnemyAI.Start에서 호출) ─────────────────────────────────

    /// <summary>소유 AI를 등록하고 이벤트 구독을 시작합니다.</summary>
    public void Initialize(NFBTEnemyAI ownerAI)
    {
        _ownerAI      = ownerAI;    // 소유 AI 등록
        _sessionStart = Time.time;  // 세션 시작 시각 기록
        SubscribeToEvents();         // 이벤트 구독 시작
    }

    private void OnDestroy() => UnsubscribeFromEvents(); // 파괴 시 이벤트 구독 해제

    // ── 이벤트 구독 / 해제 ───────────────────────────────────────────────────

    private void SubscribeToEvents()
    {
        var player = Player.Instance;
        if (player == null) return;

        if (player.Combat != null)
            player.Combat.OnAttackStarted += OnPlayerAttackStarted; // 플레이어 공격 이벤트 구독

        _ownerAI.Enemy.OnDamageTaken += OnThisEnemyHit; // 이 적 피격 이벤트 구독

        player.OnDamageTaken += OnPlayerDamageTaken; // 플레이어 피격 이벤트 구독 (이 적 기여분 추정)
    }

    private void UnsubscribeFromEvents()
    {
        var player = Player.Instance;
        if (player?.Combat != null)
            player.Combat.OnAttackStarted -= OnPlayerAttackStarted; // 플레이어 공격 구독 해제

        if (_ownerAI?.Enemy != null)
            _ownerAI.Enemy.OnDamageTaken -= OnThisEnemyHit; // 이 적 피격 구독 해제

        if (player != null)
            player.OnDamageTaken -= OnPlayerDamageTaken; // 플레이어 피격 구독 해제
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────────

    private void OnPlayerAttackStarted()
    {
        // 플레이어가 이 적의 감지 범위 내에 있을 때만 공격 횟수 카운트
        if (_ownerAI == null || Player.Instance == null) return;
        float dist = Vector2.Distance(_ownerAI.transform.position, Player.Instance.transform.position);
        if (dist <= _ownerAI.DetectionRange)
            _attackCount++; // 이 적 감지 범위 내 공격만 카운트
    }

    private void OnThisEnemyHit(float damage) => _hitCount++; // 이 적이 피격당했을 때 카운트

    private void OnPlayerDamageTaken(float damage)
    {
        // 이 적이 공격 범위 근처에 있을 때 플레이어 피해를 이 적의 기여로 간주
        if (_ownerAI == null || Player.Instance == null) return;
        float dist = Vector2.Distance(_ownerAI.transform.position, Player.Instance.transform.position);
        if (dist <= _ownerAI.AttackRange * 1.5f)
            _totalDamage += damage; // 이 적 기여 피해로 누산
    }
}
