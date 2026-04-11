using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Metroidvania/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General")]
    public string weaponName    = "Weapon";
    public float  damage        = 20f;
    public float  attackCooldown = 0.4f;
    public float  knockbackForce = 5f;

    [Header("Ranged")]
    public bool      isRanged;
    public Projectile projectilePrefab;
    public float     projectileSpeed    = 15f;
    public float     projectileLifetime = 3f;
}
