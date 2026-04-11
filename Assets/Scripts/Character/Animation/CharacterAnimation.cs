using UnityEngine;

/// <summary>
/// Drives Animator parameters from PlatformerMovement state.
/// Actual animation clips are connected later — works without clips.
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterAnimation : MonoBehaviour
{
    // Cached parameter hashes (avoids string lookup every frame)
    private static readonly int SpeedHash        = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash   = Animator.StringToHash("IsGrounded");
    private static readonly int VelocityYHash    = Animator.StringToHash("VelocityY");
    private static readonly int IsWallSlideHash  = Animator.StringToHash("IsWallSliding");
    private static readonly int IsDashingHash    = Animator.StringToHash("IsDashing");
    private static readonly int AttackHash       = Animator.StringToHash("Attack");
    private static readonly int HurtHash         = Animator.StringToHash("Hurt");
    private static readonly int DeathHash        = Animator.StringToHash("Death");

    private Animator           _animator;
    private PlatformerMovement _movement;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlatformerMovement>();
    }

    private void Update()
    {
        if (_movement == null || _animator.runtimeAnimatorController == null) return;

        _animator.SetFloat(SpeedHash,       Mathf.Abs(_movement.Velocity.x));
        _animator.SetBool (IsGroundedHash,  _movement.IsGrounded);
        _animator.SetFloat(VelocityYHash,   _movement.Velocity.y);
        _animator.SetBool (IsWallSlideHash, _movement.IsWallSliding);
        _animator.SetBool (IsDashingHash,   _movement.IsDashing);
    }

    // ── Trigger helpers ───────────────────────────────────────────────────

    public void PlayAttack() => _animator.SetTrigger(AttackHash);
    public void PlayHurt()   => _animator.SetTrigger(HurtHash);
    public void PlayDeath()  => _animator.SetTrigger(DeathHash);

    // ── Animation Event callbacks (called from clips when added later) ────

    public void OnAttackHitStart() => GetComponent<CharacterCombat>()?.ActivateHitbox();
    public void OnAttackHitEnd()   => GetComponent<CharacterCombat>()?.DeactivateHitbox();
}
