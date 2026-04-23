using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 바인딩 (InputSystem_Actions — Player map):
///   Arrow keys → 이동
///   C          → 점프
///   Z          → 대시
///   X          → 공격
///   E          → 상호작용
///   A          → 선풍기 (hold)
///   S          → 우산   (hold)
///   D          → 라이터 (hold)
///   A+S / A+D / S+D → 도구 콤보 (즉발)
/// </summary>
[RequireComponent(typeof(CharacterBase), typeof(PlatformerMovement), typeof(CharacterCombat))]
public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions _input;
    private PlatformerMovement  _movement;
    private CharacterBase       _character;
    private CharacterCombat     _combat;
    private PlayerAbilities     _abilities;
    private ToolHolder          _toolHolder; // 도구 시스템 (없을 수 있음)

    private Vector2 _moveInput; // Update에서 읽어 FixedUpdate에서 사용

    private void Awake()
    {
        _input      = new InputSystem_Actions();
        _movement   = GetComponent<PlatformerMovement>();
        _character  = GetComponent<CharacterBase>();
        _combat     = GetComponent<CharacterCombat>();
        _abilities  = GetComponent<PlayerAbilities>();
        _toolHolder = GetComponent<ToolHolder>();
    }

    private void OnEnable()
    {
        _input.Player.Enable();

        // 이동 / 점프 / 대시 / 공격
        _input.Player.Jump.performed    += OnJump;
        _input.Player.Jump.canceled     += OnJumpCanceled;
        _input.Player.Crouch.performed  += OnDash;
        _input.Player.Attack.performed  += OnAttack;
        _input.Player.Interact.performed += OnInteract;

        // 도구 (performed = 키 누름, canceled = 키 뗌)
        _input.Player.Fan.performed      += OnFanPressed;
        _input.Player.Fan.canceled       += OnFanReleased;
        _input.Player.Umbrella.performed += OnUmbrellaPressed;
        _input.Player.Umbrella.canceled  += OnUmbrellaReleased;
        _input.Player.Lighter.performed  += OnLighterPressed;
        _input.Player.Lighter.canceled   += OnLighterReleased;
    }

    private void OnDisable()
    {
        _input.Player.Jump.performed    -= OnJump;
        _input.Player.Jump.canceled     -= OnJumpCanceled;
        _input.Player.Crouch.performed  -= OnDash;
        _input.Player.Attack.performed  -= OnAttack;
        _input.Player.Interact.performed -= OnInteract;

        _input.Player.Fan.performed      -= OnFanPressed;
        _input.Player.Fan.canceled       -= OnFanReleased;
        _input.Player.Umbrella.performed -= OnUmbrellaPressed;
        _input.Player.Umbrella.canceled  -= OnUmbrellaReleased;
        _input.Player.Lighter.performed  -= OnLighterPressed;
        _input.Player.Lighter.canceled   -= OnLighterReleased;

        _input.Player.Disable();
    }

    private void Update()
    {
        if (_character.IsDead) return;
        _moveInput = _input.Player.Move.ReadValue<Vector2>(); // 입력값 캐시
        _toolHolder?.SetMoveInput(_moveInput);               // 선풍기 조준 방향 갱신
    }

    private void FixedUpdate()
    {
        if (_character.IsDead) return;
        _movement.Move(_moveInput.x);          // 물리 처리는 FixedUpdate에서
        _movement.ApplyFallMultiplier();
    }

    // ── 이동 / 점프 / 대시 / 공격 ────────────────────────────────────────

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (_character.IsDead) return;
        _movement.RequestJump();
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        _movement.ApplyJumpCut();
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (_character.IsDead) return;
        _movement.RequestDash();
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (_character.IsDead) return;
        _combat.StartAttack();
    }

    /// <summary>E키: 주변 IInteractable 오브젝트와 상호작용</summary>
    private void OnInteract(InputAction.CallbackContext ctx)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IInteractable>(out var target))
            {
                target.Interact();
                return;
            }
        }
    }

    // ── 도구 입력 ─────────────────────────────────────────────────────────

    private void OnFanPressed(InputAction.CallbackContext ctx)
    {
        if (_character.IsDead) return;
        _toolHolder?.SetFanHeld(true);
    }

    private void OnFanReleased(InputAction.CallbackContext ctx)
    {
        _toolHolder?.SetFanHeld(false);
    }

    private void OnUmbrellaPressed(InputAction.CallbackContext ctx)
    {
        if (_character.IsDead) return;
        _toolHolder?.SetUmbrellaHeld(true);
    }

    private void OnUmbrellaReleased(InputAction.CallbackContext ctx)
    {
        _toolHolder?.SetUmbrellaHeld(false);
    }

    private void OnLighterPressed(InputAction.CallbackContext ctx)
    {
        if (_character.IsDead) return;
        _toolHolder?.SetLighterHeld(true);
    }

    private void OnLighterReleased(InputAction.CallbackContext ctx)
    {
        _toolHolder?.SetLighterHeld(false);
    }
}
