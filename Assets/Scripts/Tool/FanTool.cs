using System.Collections;
using UnityEngine;

/// <summary>
/// 선풍기 도구 (A키).
/// 위키를 누른 상태면 위쪽으로, 아니면 바라보는 좌우 방향으로 바람을 발사한다.
/// 플레이어에게 반대 방향 반동을 가하며, 파티클 이펙트로 방향을 시각화한다.
/// </summary>
public class FanTool : ToolBase
{
    [Header("Wind")]
    [SerializeField] private float     _windForce    = 18f;   // 오브젝트에 가하는 힘
    [SerializeField] private float     _windRange    = 5f;    // 바람 도달 거리
    [SerializeField] private float     _windInterval = 0.05f; // 바람 반복 주기(초)
    [SerializeField] private LayerMask _affectedLayers;       // 바람에 반응할 레이어

    [Header("Recoil")]
    [SerializeField] private float _selfRecoil = 5f; // 플레이어 반동 세기

    [Header("Wind Effect")]
    [SerializeField] private ParticleSystem _windParticles;  // 바람 파티클 (인스펙터 연결)
    [SerializeField] private float          _upwardOffset = 1f; // 위 방향 발사 시 머리 위 높이 오프셋

    private Rigidbody2D _ownerRb;
    private Coroutine   _windRoutine;
    private Vector2     _aimInput;              // PlayerController에서 매 프레임 전달받는 이동 입력
    private Vector3     _particlesDefaultLocalPos; // 파티클의 기본 로컬 위치 (좌우 발사 시 복원용)

    protected override void Awake()
    {
        base.Awake();
        _ownerRb = GetComponentInParent<Rigidbody2D>();
        if (_windParticles != null)
            _particlesDefaultLocalPos = _windParticles.transform.localPosition;
    }

    /// <summary>PlayerController → ToolHolder를 통해 매 프레임 이동 입력을 전달받음</summary>
    public void SetAimInput(Vector2 input) => _aimInput = input;

    /// <summary>현재 바람 발사 방향을 반환 (위키 우선, 아니면 좌우 방향)</summary>
    private Vector2 GetWindDirection()
    {
        if (_aimInput.y > 0.5f) return Vector2.up;
        return (_movement != null && _movement.IsFacingRight) ? Vector2.right : Vector2.left;
    }

    /// <summary>매 프레임 파티클 방향을 현재 바람 방향에 맞춰 갱신</summary>
    protected override void Update()
    {
        base.Update();

        if (!IsActive || _windParticles == null) return;

        // 월드 회전으로 설정 (localEulerAngles는 부모 스케일 반전에 영향받아 오작동)
        // 위: X=-90° → 로컬Z = 월드+Y / 오른쪽: Y=-90° / 왼쪽: Y=+90°
        Vector2 dir = GetWindDirection();
        UnityEngine.Quaternion rot;
        if (dir == Vector2.up)
        {
            // 위 발사: 파티클을 머리 위로 이동 후 위쪽 방향으로 회전
            _windParticles.transform.localPosition = new UnityEngine.Vector3(0f, _upwardOffset, 0f);
            rot = UnityEngine.Quaternion.Euler(-90f, 0f, 0f);
        }
        else
        {
            // 좌우 발사: 원래 위치 복원 후 방향 회전
            _windParticles.transform.localPosition = _particlesDefaultLocalPos;
            rot = UnityEngine.Quaternion.Euler(0f, dir.x > 0f ? 90f : -90f, 0f);
        }

        _windParticles.transform.rotation = rot;
    }

    /// <summary>선풍기 가동: 파티클 재생 + 지속 바람 코루틴 시작</summary>
    protected override void OnUse(Vector2 direction)
    {
        _windParticles?.Play();
        _windRoutine = StartCoroutine(WindRoutine());
    }

    /// <summary>선풍기 종료: 파티클 중단 + 코루틴 중단</summary>
    protected override void OnStopUse()
    {
        _windParticles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (_windRoutine != null)
        {
            StopCoroutine(_windRoutine);
            _windRoutine = null;
        }
    }

    /// <summary>일정 간격으로 현재 방향으로 바람 발사 + 반동 적용</summary>
    private IEnumerator WindRoutine()
    {
        var wait = new WaitForSeconds(_windInterval);
        while (IsActive)
        {
            Vector2 dir = GetWindDirection();
            BlowWind(dir);
            ApplyRecoil(dir);
            yield return wait;
        }
    }

    /// <summary>바람 방향 원형 범위 내 오브젝트에 힘 + IWindAffectable 이벤트 전달</summary>
    private void BlowWind(Vector2 dir)
    {
        // 위 방향이면 머리 위에서 판정 시작, 좌우는 플레이어 중심에서 시작
        Vector2 origin = (Vector2)transform.position + (dir == Vector2.up ? Vector2.up * _upwardOffset : Vector2.zero);
        Vector2 center = origin + dir * (_windRange * 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _windRange * 0.5f, _affectedLayers);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.TryGetComponent<Rigidbody2D>(out var rb))
                rb.AddForce(dir * _windForce, ForceMode2D.Force);
            if (hit.TryGetComponent<IWindAffectable>(out var wa))
                wa.OnWind(dir, _windForce);
        }
    }

    /// <summary>플레이어를 발사 반대 방향으로 밀어냄</summary>
    private void ApplyRecoil(Vector2 dir)
    {
        _ownerRb?.AddForce(-dir * _selfRecoil, ForceMode2D.Force);
    }
}
