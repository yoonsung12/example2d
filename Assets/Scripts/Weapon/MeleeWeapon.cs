using System.Collections;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [SerializeField] private WeaponData  _data;
    [SerializeField] private Collider2D  _hitbox;
    [SerializeField] private LayerMask   _targetLayer;

    private float _cooldownTimer;

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
        _hitbox.enabled = true;
        yield return new WaitForSeconds(0.12f);
        _hitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitbox == null || !_hitbox.enabled) return;
        if (((1 << other.gameObject.layer) & _targetLayer) == 0) return;
        if (!other.TryGetComponent<IDamageable>(out var target)) return;

        Vector2 knockback = (other.transform.position - transform.position).normalized * _data.knockbackForce;
        target.TakeDamage(_data.damage, knockback);
    }
}
