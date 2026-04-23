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

    /// <summary>HP 변경 시 발생 (현재HP, 최대HP)</summary>
    public event System.Action<float, float> OnHealthChanged;

    /// <summary>피해를 받을 때 발생 (피해량) — CombatStatsTracker가 구독</summary>
    public event System.Action<float> OnDamageTaken;

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

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage); // HP를 0 이하로 내려가지 않게 갱신
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);     // HP 변경 이벤트 발생
        OnDamageTaken?.Invoke(damage);                         // 피해량 이벤트 발생 (CombatStatsTracker 수신)
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
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
