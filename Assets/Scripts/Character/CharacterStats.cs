using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "Metroidvania/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 5f; // 칸 수 = 최대 HP (한 대 = 1 데미지 = 1칸)

    [Header("Combat")]
    public float attackDamage = 1f; // 한 번 공격 시 1칸 감소
    public float attackCooldown = 0.4f;
    public float knockbackForce = 5f;
    public float invincibilityDuration = 0.5f;
}
