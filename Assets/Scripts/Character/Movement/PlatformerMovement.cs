using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float _moveSpeed    = 8f;
    [SerializeField] private float _acceleration = 50f;
    [SerializeField] private float _deceleration = 50f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce        = 16f;
    [SerializeField] private float _coyoteTime       = 0.15f;
    [SerializeField] private float _jumpBufferTime   = 0.1f;
    [SerializeField] private float _fallMultiplier   = 2.5f;
    [SerializeField] private float _lowJumpMultiplier = 2f;

    [Header("Dash")]
    [SerializeField] private float _dashForce    = 20f;
    [SerializeField] private float _dashDuration = 0.15f;
    [SerializeField] private float _dashCooldown = 0.5f;

    [Header("Wall")]
    [SerializeField] private float _wallSlideSpeed    = 2f;
    [SerializeField] private float _wallJumpForceX    = 10f;
    [SerializeField] private float _wallJumpForceY    = 14f;
    [SerializeField] private float _wallJumpLockout   = 0.25f;  // 재부착 방지 시간

    [Header("Detection")]
    [SerializeField] private Transform  _groundCheck;
    [SerializeField] private float      _groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask  _groundLayer;
    [SerializeField] private Transform  _wallCheckLeft;
    [SerializeField] private Transform  _wallCheckRight;
    [SerializeField] private float      _wallCheckDistance = 0.2f;

    // Ability flags — toggled by PlayerAbilities
    public bool CanDoubleJump { get; set; }
    public bool CanDash       { get; set; }
    public bool CanWallJump   { get; set; }

    /// <summary>활공 등 외부에서 낙하 가속을 억제할 때 true로 설정</summary>
    public bool SuppressFallMultiplier { get; set; }

    // ── 디버프 플래그 ─────────────────────────────────────────────────────
    /// <summary>봄 속박: 이동/점프/대시 불가</summary>
    public bool IsBound    { get; set; }
    /// <summary>가을 혼란: 좌우 입력 반전</summary>
    public bool IsConfused { get; set; }
    /// <summary>겨울 빙결: 모든 행동 불가</summary>
    public bool IsFrozen   { get; set; }
    /// <summary>여름 이속저하: 이동속도 배율 (기본 1)</summary>
    public float SpeedMultiplier { get; set; } = 1f;
    /// <summary>활공 중 수평 이동 배율 (ToolComboSystem에서 설정)</summary>
    public float GlideSpeedMultiplier { get; set; } = 1f;

    // Read-only state
    public bool    IsGrounded    { get; private set; }
    public bool    IsWallSliding { get; private set; }
    public bool    IsDashing     { get; private set; }
    public bool    IsFacingRight { get; private set; } = true;
    public bool    IsKnockedBack { get; private set; }
    public Vector2 Velocity      => _rb.linearVelocity;

    private Rigidbody2D _rb;
    private float _coyoteCounter;
    private float _jumpBufferCounter;
    private float _dashCooldownCounter;
    private float _dashTimer;
    private float _knockbackTimer;
    private int   _extraJumpsLeft;
    private bool  _wallLeft;
    private bool  _wallRight;

    // 낙하 여부와 무관하게 공중에서 벽에 닿았는지 여부
    private bool  _isTouchingWall;
    // 월점프 후 재부착 방지 타이머
    private float _wallJumpLockoutTimer;
    // 월점프 직후 밀어낼 방향 (+1 오른쪽, -1 왼쪽)
    private float _wallJumpPushDir;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        CheckGrounded();
        CheckWalls();
        TickTimers();
        HandleWallSlide();
    }

    private void FixedUpdate()
    {
        if (IsDashing)
        {
            _dashTimer -= Time.fixedDeltaTime;
            if (_dashTimer <= 0f) EndDash();
            return;
        }
        if (IsKnockedBack)
        {
            _knockbackTimer -= Time.fixedDeltaTime;
            if (_knockbackTimer <= 0f) IsKnockedBack = false;
        }
    }

    // ── Ground / Wall checks ──────────────────────────────────────────────

    private void CheckGrounded()
    {
        IsGrounded = _groundCheck != null &&
                     Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

        if (IsGrounded)
        {
            _coyoteCounter  = _coyoteTime;
            _extraJumpsLeft = CanDoubleJump ? 1 : 0;
            _wallJumpLockoutTimer = 0f;
        }
    }

    private void CheckWalls()
    {
        _wallLeft  = _wallCheckLeft  != null &&
                     Physics2D.Raycast(_wallCheckLeft.position,  Vector2.left,  _wallCheckDistance, _groundLayer);
        _wallRight = _wallCheckRight != null &&
                     Physics2D.Raycast(_wallCheckRight.position, Vector2.right, _wallCheckDistance, _groundLayer);

        // 락아웃 중에는 벽 부착 무효
        bool lockout = _wallJumpLockoutTimer > 0f;
        _isTouchingWall = CanWallJump && !IsGrounded && !lockout &&
                          ((IsFacingRight && _wallRight) || (!IsFacingRight && _wallLeft));
    }

    // ── Timers ────────────────────────────────────────────────────────────

    private void TickTimers()
    {
        if (!IsGrounded)               _coyoteCounter        -= Time.deltaTime;
        if (_jumpBufferCounter   > 0f) _jumpBufferCounter    -= Time.deltaTime;
        if (_dashCooldownCounter > 0f) _dashCooldownCounter  -= Time.deltaTime;
        if (_wallJumpLockoutTimer > 0f) _wallJumpLockoutTimer -= Time.deltaTime;
    }

    // ── Wall slide ────────────────────────────────────────────────────────

    private void HandleWallSlide()
    {
        // 슬라이딩: 벽에 닿아있고 낙하 중일 때만 속도 제한
        IsWallSliding = _isTouchingWall && _rb.linearVelocity.y < 0f;

        if (IsWallSliding)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x,
                Mathf.Max(_rb.linearVelocity.y, -_wallSlideSpeed));
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Move(float horizontal)
    {
        if (IsFrozen || IsBound) return;
        if (IsDashing || IsKnockedBack) return;

        if (IsConfused) horizontal = -horizontal; // 가을 혼란: 좌우 반전

        // 월점프 락아웃 중: 입력과 무관하게 벽 반대 방향으로 강제 이동
        if (_wallJumpLockoutTimer > 0f)
        {
            float targetSpeed = _wallJumpPushDir * _moveSpeed;
            float speedDiff   = targetSpeed - _rb.linearVelocity.x;
            float force       = Mathf.Pow(Mathf.Abs(speedDiff) * _acceleration, 0.9f) * Mathf.Sign(speedDiff);
            _rb.AddForce(force * Vector2.right);
            return;
        }

        float tgtSpeed  = horizontal * _moveSpeed * SpeedMultiplier * GlideSpeedMultiplier; // 이속저하 + 활공 배율 반영
        float diff      = tgtSpeed - _rb.linearVelocity.x;
        float accelRate = Mathf.Abs(tgtSpeed) > 0.01f ? _acceleration : _deceleration;
        float f         = Mathf.Pow(Mathf.Abs(diff) * accelRate, 0.9f) * Mathf.Sign(diff);
        _rb.AddForce(f * Vector2.right);

        if      (horizontal > 0.01f && !IsFacingRight) Flip();
        else if (horizontal < -0.01f &&  IsFacingRight) Flip();
    }

    public void RequestJump()
    {
        if (IsFrozen || IsBound) return;
        _jumpBufferCounter = _jumpBufferTime;
        TryConsumeJump();
    }

    private void TryConsumeJump()
    {
        if (_jumpBufferCounter <= 0f) return;

        if (_coyoteCounter > 0f)
        {
            PerformJump(_jumpForce);
            _coyoteCounter     = 0f;
            _jumpBufferCounter = 0f;
        }
        else if (_isTouchingWall)
        {
            // 현재 붙어있는 벽의 반대 방향으로 점프
            float dir = IsFacingRight ? -1f : 1f;
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(new Vector2(dir * _wallJumpForceX, _wallJumpForceY), ForceMode2D.Impulse);
            Flip();

            // 락아웃 시작: 재부착 방지 + 밀어낼 방향 저장
            _wallJumpLockoutTimer = _wallJumpLockout;
            _wallJumpPushDir      = dir;
            _jumpBufferCounter    = 0f;
        }
        else if (_extraJumpsLeft > 0)
        {
            PerformJump(_jumpForce);
            _extraJumpsLeft--;
            _jumpBufferCounter = 0f;
        }
    }

    private void PerformJump(float force)
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    // Call on jump button release for variable-height jump
    public void ApplyJumpCut()
    {
        if (_rb.linearVelocity.y > 0f)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * 0.5f);
    }

    // Call every FixedUpdate from PlayerController for better-feeling gravity
    public void ApplyFallMultiplier()
    {
        if (SuppressFallMultiplier) return; // 활공 등 외부 억제 중에는 스킵

        if (_rb.linearVelocity.y < 0f)
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_fallMultiplier - 1f) * Time.fixedDeltaTime;
        else if (_rb.linearVelocity.y > 0f)
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
    }

    public void RequestDash()
    {
        if (IsFrozen || IsBound) return;
        if (!CanDash || IsDashing || _dashCooldownCounter > 0f) return;

        IsDashing            = true;
        _dashTimer           = _dashDuration;
        _dashCooldownCounter = _dashCooldown;

        _rb.linearVelocity  = Vector2.zero;
        float dir = IsFacingRight ? 1f : -1f;
        _rb.AddForce(new Vector2(dir * _dashForce, 0f), ForceMode2D.Impulse);
        _rb.gravityScale = 0f;
    }

    private void EndDash()
    {
        IsDashing          = false;
        _rb.gravityScale   = 1f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.5f, _rb.linearVelocity.y);
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        IsKnockedBack      = true;
        _knockbackTimer    = 0.2f;
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    private void Flip()
    {
        IsFacingRight        = !IsFacingRight;
        Vector3 s            = transform.localScale;
        s.x                 *= -1f;
        transform.localScale = s;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }
        if (_wallCheckLeft != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_wallCheckLeft.position,  Vector2.left  * _wallCheckDistance);
        }
        if (_wallCheckRight != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_wallCheckRight.position, Vector2.right * _wallCheckDistance);
        }
    }
}
