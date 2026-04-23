using System.Collections.Generic;
using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    [SerializeField] private Collider2D _hitbox;
    [SerializeField] private LayerMask  _targetLayer;
    /// <summary>히트박스 시작 전 근접 사각지대를 커버하는 반경 (플레이어 중심 기준)</summary>
    [SerializeField] private float      _closeRangeRadius = 0.6f;

    /// <summary>공격 시작 시 발생 — CombatStatsTracker가 구독</summary>
    public event System.Action OnAttackStarted;

    /// <summary>적에게 피해를 입힐 때 발생 — CombatStatsTracker가 구독</summary>
    public event System.Action OnHitDealt;

    private CharacterBase _character;
    private float         _cooldownTimer;
    private bool          _isAttacking;
    private float         _attackDuration;
    private float         _attackTimer;

    // 한 스윙에서 이미 피해를 준 타겟 추적 (중복 피해 방지)
    private readonly HashSet<IDamageable> _hitTargets = new();

    private void Awake()
    {
        _character = GetComponent<CharacterBase>();
        if (_hitbox != null) _hitbox.enabled = false;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        if (_isAttacking)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f) DeactivateHitbox();
        }
    }

    public bool CanAttack   => _cooldownTimer <= 0f && !_isAttacking; // 공격 가능 여부
    public bool IsAttacking => _isAttacking;                          // 현재 공격 중 여부 (BT 액션에서 참조)

    public void StartAttack()
    {
        if (!CanAttack) return;
        _isAttacking    = true;
        _attackDuration = _character.Stats != null ? _character.Stats.attackCooldown * 0.5f : 0.2f; // 공격 지속 시간
        _attackTimer    = _attackDuration;
        _cooldownTimer  = _character.Stats != null ? _character.Stats.attackCooldown : 0.4f;         // 공격 쿨다운
        _character.Anim?.PlayAttack();
        OnAttackStarted?.Invoke(); // 공격 시작 이벤트 발생
        ActivateHitbox();
    }

    // Called by Animation Event via CharacterAnimation.OnAttackHitStart()
    public void ActivateHitbox()
    {
        if (_hitbox == null) return;
        _hitTargets.Clear();
        _hitbox.enabled = true;

        // 1) 근접 사각지대 보완: 플레이어 중심 기준 원형 스캔
        var closeHits = Physics2D.OverlapCircleAll(transform.position, _closeRangeRadius, _targetLayer);
        foreach (var col in closeHits)
            ApplyDamage(col);

        // 2) 히트박스와 이미 겹쳐있는 콜라이더 즉시 처리 (트리거 포함)
        var results = new List<Collider2D>();
        _hitbox.Overlap(new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = _targetLayer }, results);
        foreach (var col in results)
            ApplyDamage(col);
    }

    // Called by Animation Event via CharacterAnimation.OnAttackHitEnd()
    public void DeactivateHitbox()
    {
        if (_hitbox == null) return;
        _hitbox.enabled = false;
        _isAttacking    = false;
        _hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitbox == null || !_hitbox.enabled) return;
        if (((1 << other.gameObject.layer) & _targetLayer) == 0) return;
        ApplyDamage(other);
    }

    private void ApplyDamage(Collider2D other)
    {
        var target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;
        if (!_hitTargets.Add(target)) return;

        float damage    = _character.Stats != null ? _character.Stats.attackDamage   : 20f; // 공격 데미지
        float kbForce   = _character.Stats != null ? _character.Stats.knockbackForce : 5f;  // 넉백 강도
        Vector2 knockback = (other.transform.position - transform.position).normalized * kbForce; // 넉백 방향
        target.TakeDamage(damage, knockback);
        OnHitDealt?.Invoke(); // 명중 이벤트 발생
    }
}
