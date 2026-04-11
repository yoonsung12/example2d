using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class CharacterBase : MonoBehaviour, IDamageable
{
    [SerializeField] protected CharacterStats _stats;

    public PlatformerMovement Movement  { get; private set; }
    public CharacterAnimation Anim      { get; private set; }
    public CharacterCombat    Combat    { get; private set; }

    public float MaxHealth     => _stats != null ? _stats.maxHealth : 100f;
    public float CurrentHealth { get; protected set; }
    public bool  IsDead        { get; protected set; }
    public CharacterStats Stats => _stats;

    protected virtual void Awake()
    {
        Movement = GetComponent<PlatformerMovement>();
        Anim     = GetComponent<CharacterAnimation>();
        Combat   = GetComponent<CharacterCombat>();
        CurrentHealth = MaxHealth;
    }

    public virtual void TakeDamage(float damage, Vector2 knockback = default)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (knockback != Vector2.zero)
            Movement?.ApplyKnockback(knockback);

        Anim?.PlayHurt();
        OnDamaged(damage);

        if (CurrentHealth <= 0f)
            Die();
    }

    protected virtual void OnDamaged(float damage) { }

    protected virtual void Die()
    {
        IsDead = true;
        Anim?.PlayDeath();
        OnDeath();
    }

    protected virtual void OnDeath() { }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }
}
