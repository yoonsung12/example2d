using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float     _detectionRange = 8f;
    [SerializeField] private float     _attackRange    = 1.5f;
    [SerializeField] private LayerMask _playerLayer;

    [Header("Patrol")]
    [SerializeField] private Transform[] _patrolPoints;

    public EnemyBase  Enemy          { get; private set; }
    public Transform  PlayerTransform { get; private set; }
    public Transform[] PatrolPoints  => _patrolPoints;
    public float      DetectionRange => _detectionRange;
    public float      AttackRange    => _attackRange;

    private StateMachine _sm;

    private void Awake()
    {
        Enemy = GetComponent<EnemyBase>();
        _sm   = new StateMachine();
    }

    private void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) PlayerTransform = pc.transform;

        _sm.ChangeState(new IdleState(this));
    }

    private void Update()
    {
        if (Enemy.IsDead) return;
        _sm.Update();
    }

    private void FixedUpdate()
    {
        if (Enemy.IsDead) return;
        _sm.FixedUpdate();
    }

    public void ChangeState(IState state) => _sm.ChangeState(state);

    public bool IsPlayerInRange(float range)
    {
        if (PlayerTransform == null) return false;
        return Vector2.Distance(transform.position, PlayerTransform.position) <= range;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
