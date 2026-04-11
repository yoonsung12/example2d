using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    [SerializeField] private Collider2D _hitbox;
    [SerializeField] private LayerMask  _targetLayer;

    private CharacterBase _character;
    private float         _cooldownTimer;
    private bool          _isAttacking;
    private float         _attackDuration;
    private float         _attackTimer;

    private void Awake()
    {
        _character = GetComponent<CharacterBase>();
        if (_hitbox != null) _hitbox.enabled = false;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        // AnimatorController 없을 때 타이머로 히트박스 자동 종료
        if (_isAttacking)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f) DeactivateHitbox();
        }
    }

    public bool CanAttack => _cooldownTimer <= 0f && !_isAttacking;

    public void StartAttack()
    {
        if (!CanAttack) return;
        _isAttacking   = true;
        _attackDuration = _character.Stats != null ? _character.Stats.attackCooldown * 0.5f : 0.2f;
        _attackTimer   = _attackDuration;
        _cooldownTimer = _character.Stats != null ? _character.Stats.attackCooldown : 0.4f;
        _character.Anim?.PlayAttack();
        ActivateHitbox();
    }

    // Called by Animation Event via CharacterAnimation.OnAttackHitStart()
    public void ActivateHitbox()
    {
        if (_hitbox == null) return;
        _hitbox.enabled = true;
    }

    // Called by Animation Event via CharacterAnimation.OnAttackHitEnd()
    public void DeactivateHitbox()
    {
        if (_hitbox == null) return;
        _hitbox.enabled = false;
        _isAttacking    = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitbox == null || !_hitbox.enabled) return;
        if (((1 << other.gameObject.layer) & _targetLayer) == 0) return;
        if (!other.TryGetComponent<IDamageable>(out var target)) return;

        float damage    = _character.Stats != null ? _character.Stats.attackDamage   : 20f;
        float kbForce   = _character.Stats != null ? _character.Stats.knockbackForce : 5f;
        Vector2 knockback = (other.transform.position - transform.position).normalized * kbForce;
        target.TakeDamage(damage, knockback);
    }
}
