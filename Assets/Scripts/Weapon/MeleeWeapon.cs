using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [SerializeField] private WeaponData  _data;
    [SerializeField] private Collider2D  _hitbox;
    [SerializeField] private LayerMask   _targetLayer;
    /// <summary>히트박스 시작 전 근접 사각지대를 커버하는 반경 (무기 중심 기준)</summary>
    [SerializeField] private float       _closeRangeRadius = 0.6f;

    private float _cooldownTimer;

    // 한 스윙에서 이미 피해를 준 타겟 추적 (중복 피해 방지)
    private readonly HashSet<IDamageable> _hitTargets = new();

    private void Awake()
    {
        if (_hitbox != null) _hitbox.enabled = false;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    public bool CanAttack => _cooldownTimer <= 0f;

    public void Attack()
    {
        if (!CanAttack) return;
        _cooldownTimer = _data.attackCooldown;
        StartCoroutine(HitboxRoutine());
    }

    private IEnumerator HitboxRoutine()
    {
        _hitTargets.Clear();
        _hitbox.enabled = true;

        // 1) 근접 사각지대 보완: 무기 중심 기준 원형 스캔
        var closeHits = Physics2D.OverlapCircleAll(transform.position, _closeRangeRadius, _targetLayer);
        foreach (var col in closeHits)
            ApplyDamage(col);

        // 2) 히트박스와 이미 겹쳐있는 콜라이더 즉시 처리 (트리거 포함)
        var results = new List<Collider2D>();
        _hitbox.Overlap(new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = _targetLayer }, results);
        foreach (var col in results)
            ApplyDamage(col);

        yield return new WaitForSeconds(0.12f);
        _hitbox.enabled = false;
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
        if (!other.TryGetComponent<IDamageable>(out var target)) return;
        if (!_hitTargets.Add(target)) return;

        Vector2 knockback = (other.transform.position - transform.position).normalized * _data.knockbackForce;
        target.TakeDamage(_data.damage, knockback);
    }
}
