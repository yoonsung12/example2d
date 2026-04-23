using UnityEngine;

/// <summary>
/// 모든 도구의 공통 기반 클래스.
/// 사용 시 _visualRoot를 캐릭터 앞에 활성화하고 매 프레임 위치를 갱신한다.
/// </summary>
public abstract class ToolBase : MonoBehaviour, IUsableTool
{
    [SerializeField] private ToolType _toolType; // 인스펙터에서 도구 종류 지정

    [Header("Visual")]
    [SerializeField] protected Transform _visualRoot;    // 도구 비주얼 루트 오브젝트 (인스펙터 연결)
    [SerializeField] private float       _visualOffset = 1f; // 캐릭터 앞쪽 배치 거리

    /// <summary>이 도구의 종류</summary>
    public ToolType ToolType => _toolType;

    /// <summary>현재 사용 중 여부</summary>
    public bool IsActive { get; protected set; }

    protected PlatformerMovement _movement; // 바라보는 방향 참조용

    protected virtual void Awake()
    {
        _movement = GetComponentInParent<PlatformerMovement>();

        // 시작 시 비주얼 숨김
        if (_visualRoot != null) _visualRoot.gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (!IsActive || _visualRoot == null) return;

        // 비주얼을 캐릭터가 바라보는 방향 앞쪽으로 매 프레임 이동
        float dir = (_movement != null && _movement.IsFacingRight) ? 1f : -1f;
        _visualRoot.position = transform.position + new Vector3(dir * _visualOffset, 0f, 0f);
    }

    // ── IUsableTool ───────────────────────────────────────────────────────

    /// <summary>도구 사용 시작 — 비주얼 활성 후 OnUse 호출</summary>
    public void Use(Vector2 direction)
    {
        if (IsActive) return;
        IsActive = true;
        if (_visualRoot != null) _visualRoot.gameObject.SetActive(true);
        OnUse(direction);
    }

    /// <summary>도구 사용 종료 — OnStopUse 호출 후 비주얼 비활성</summary>
    public void StopUse()
    {
        if (!IsActive) return;
        OnStopUse();
        if (_visualRoot != null) _visualRoot.gameObject.SetActive(false);
        IsActive = false;
    }

    /// <summary>도구별 실제 효과 구현 (사용 시작)</summary>
    protected abstract void OnUse(Vector2 direction);

    /// <summary>도구별 종료 처리 구현 (필요 시 override)</summary>
    protected virtual void OnStopUse() { }
}
