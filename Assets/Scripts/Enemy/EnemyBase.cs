using UnityEngine;

public class EnemyBase : CharacterBase
{
    private float _invincibilityTimer;

    public event System.Action<EnemyBase> OnEnemyDied;

    /// <summary>이 적이 속한 룸. Room.DiscoverEnemies()에서 자동 할당됩니다.</summary>
    public Room HomeRoom { get; private set; }

    public void SetHomeRoom(Room room) => HomeRoom = room;

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
