using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, Vector2 knockback = default);
    bool IsDead { get; }
}
