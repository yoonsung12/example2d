using UnityEngine;

public class EnemyBase : CharacterBase
{
    private float _invincibilityTimer;

    public event System.Action<EnemyBase> OnEnemyDied;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    public override void TakeDamage(float damage, Vector2 knockback = default)
    {
        if (_invincibilityTimer > 0f || IsDead) return;
        base.TakeDamage(damage, knockback);
        _invincibilityTimer = Stats != null ? Stats.invincibilityDuration : 0.3f;
    }

    protected override void OnDeath()
    {
        OnEnemyDied?.Invoke(this);
        Destroy(gameObject, 1f);
    }
}
