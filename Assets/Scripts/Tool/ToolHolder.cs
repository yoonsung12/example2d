using UnityEngine;

/// <summary>
/// 플레이어의 3가지 도구(선풍기·우산·라이터)를 관리한다.
/// PlayerController로부터 각 키의 pressed/released 이벤트를 받아
/// 단일 도구 사용 또는 2-키 콤보를 결정한다.
///
/// 규칙:
///   1개 키 유지  → 해당 도구 활성
///   2개 키 동시  → 콤보 즉발 (개별 도구 비활성)
///   0개 키      → 모든 도구 비활성
/// </summary>
public class ToolHolder : MonoBehaviour
{
    [Header("Tool References")]
    [SerializeField] private FanTool      _fanTool;      // 선풍기 (A키)
    [SerializeField] private UmbrellaTool _umbrellaTool; // 우산   (S키)
    [SerializeField] private LighterTool  _lighterTool;  // 라이터 (D키)

    private bool _fanHeld;
    private bool _umbrellaHeld;
    private bool _lighterHeld;

    private ToolComboSystem   _comboSystem;
    private PlatformerMovement _movement;

    private void Awake()
    {
        _comboSystem = GetComponent<ToolComboSystem>();
        _movement    = GetComponent<PlatformerMovement>();
    }

    // ── 키 이벤트 수신 API (PlayerController에서 호출) ────────────────────

    /// <summary>선풍기 키 상태 변경</summary>
    public void SetFanHeld(bool held)
    {
        _fanHeld = held;
        EvaluateState();
    }

    /// <summary>PlayerController에서 매 프레임 이동 입력을 전달 (선풍기 조준 방향 갱신)</summary>
    public void SetMoveInput(Vector2 input) => _fanTool?.SetAimInput(input);

    /// <summary>우산 키 상태 변경</summary>
    public void SetUmbrellaHeld(bool held)
    {
        _umbrellaHeld = held;
        EvaluateState();
    }

    /// <summary>라이터 키 상태 변경</summary>
    public void SetLighterHeld(bool held)
    {
        _lighterHeld = held;
        EvaluateState();
    }

    // ── 상태 평가 ─────────────────────────────────────────────────────────

    /// <summary>현재 눌린 키 조합에 따라 도구/콤보 활성화를 결정</summary>
    private void EvaluateState()
    {
        int count = (_fanHeld ? 1 : 0) + (_umbrellaHeld ? 1 : 0) + (_lighterHeld ? 1 : 0);

        StopAllTools(); // 상태 전환 시 항상 초기화 후 재적용

        switch (count)
        {
            case 2: TriggerCombo();          break; // 2키 동시 → 콤보
            case 1: ActivateSingleTool();    break; // 1키     → 단일 도구
            // 0: 이미 StopAllTools() 처리됨
        }
    }

    /// <summary>눌린 2개 키 조합으로 콤보 발동 (순서 무관)</summary>
    private void TriggerCombo()
    {
        if (_comboSystem == null) return;

        Vector2 dir = GetFacingDirection();
        if (_fanHeld      && _umbrellaHeld) _comboSystem.TryCombo(ToolType.Fan,     ToolType.Umbrella, dir);
        else if (_fanHeld && _lighterHeld)  _comboSystem.TryCombo(ToolType.Fan,     ToolType.Lighter,  dir);
        else                               _comboSystem.TryCombo(ToolType.Umbrella, ToolType.Lighter,  dir);
    }

    /// <summary>눌린 1개 키에 해당하는 도구를 활성화</summary>
    private void ActivateSingleTool()
    {
        Vector2 dir = GetFacingDirection();
        if (_fanHeld)      _fanTool?.Use(dir);
        if (_umbrellaHeld) _umbrellaTool?.Use(dir);
        if (_lighterHeld)  _lighterTool?.Use(dir);
    }

    /// <summary>모든 도구 사용 중단</summary>
    private void StopAllTools()
    {
        _fanTool?.StopUse();
        _umbrellaTool?.StopUse();
        _lighterTool?.StopUse();
    }

    /// <summary>캐릭터가 바라보는 수평 방향 벡터 반환</summary>
    private Vector2 GetFacingDirection()
    {
        return (_movement != null && _movement.IsFacingRight) ? Vector2.right : Vector2.left;
    }
}
