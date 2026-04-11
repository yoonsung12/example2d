using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private float      _damage;
    private Vector2    _direction;
    private LayerMask  _targetLayer;
    private Rigidbody2D _rb;

    private void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Init(float damage, float speed, float lifetime, Vector2 direction, LayerMask targetLayer)
    {
        _damage      = damage;
        _direction   = direction.normalized;
        _targetLayer = targetLayer;
        _rb.linearVelocity = _direction * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _targetLayer) == 0) return;

        if (other.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(_damage, _direction * 3f);

        Destroy(gameObject);
    }
}
