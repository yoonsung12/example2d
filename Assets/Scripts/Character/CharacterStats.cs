using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "Metroidvania/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Combat")]
    public float attackDamage = 20f;
    public float attackCooldown = 0.4f;
    public float knockbackForce = 5f;
    public float invincibilityDuration = 0.5f;
}
