using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input binding (InputSystem_Actions):
///   Move    → 좌우 이동
///   Sprint  → 점프  (LeftShift — 에디터에서 Space로 리바인드 권장)
///   Crouch  → 대시  (C         — 에디터에서 LeftShift로 리바인드 권장)
///   Attack  → 공격
///   Interact→ 상호작용
/// </summary>
[RequireComponent(typeof(CharacterBase), typeof(PlatformerMovement), typeof(CharacterCombat))]
public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions _input;
    private PlatformerMovement  _movement;
    private CharacterBase       _character;
    private CharacterCombat     _combat;
    private PlayerAbilities     _abilities;

    private Vector2 _moveInput;

    private void Awake()
    {
        _input     = new InputSystem_Actions();
        _movement  = GetComponent<PlatformerMovement>();
        _character = GetComponent<CharacterBase>();
        _combat    = GetComponent<CharacterCombat>();
        _abilities = GetComponent<PlayerAbilities>();
    }

    private void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Jump.performed    += OnJump;
        _input.Player.Jump.canceled     += OnJumpCanceled;
        _input.Player.Sprint.performed  += OnJump;      // LeftShift 하위 호환
        _input.Player.Sprint.canceled   += OnJumpCanceled;
        _input.Player.Crouch.performed  += OnDash;
        _input.Player.Attack.performed  += OnAttack;
        _input.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        _input.Player.Jump.performed    -= OnJump;
        _input.Player.Jump.canceled     -= OnJumpCanceled;
        _input.Player.Sprint.performed  -= OnJump;
        _input.Player.Sprint.canceled   -= OnJumpCanceled;
        _input.Player.Crouch.performed  -= OnDash;
        _input.Player.Attack.performed  -= OnAttack;
        _input.Player.Interact.performed -= OnInteract;
        _input.Player.Disable();
    }

    private void Update()
    {
        if (_character.IsDead) return;
        _moveInput = _input.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (_character.IsDead) return;
        _movement.Move(_moveInput.x);
        _movement.ApplyFallMultiplier();
    }

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
}
