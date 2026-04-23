using System.Collections;
using UnityEngine;

/// <summary>
/// SeasonalGauge의 디버프 발동 이벤트를 받아 플레이어에게 계절별 디버프를 적용한다.
/// 봄=속박 / 여름=이속저하+HP드레인 / 가을=혼란(방향반전) / 겨울=빙결
/// </summary>
public class DebuffManager : MonoBehaviour
{
    public static DebuffManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlatformerMovement _movement;  // 플레이어 이동 컴포넌트
    [SerializeField] private CharacterBase      _character; // 플레이어 캐릭터

    [Header("Summer — 이속저하 + HP드레인")]
    [SerializeField] private float _slowMultiplier = 0.4f;  // 이동속도 배율
    [SerializeField] private float _drainPerSecond = 5f;    // 초당 HP 감소량

    public DebuffType CurrentDebuff { get; private set; } = DebuffType.None;

    /// <summary>디버프 변경 시 UI 등에 알림</summary>
    public event System.Action<DebuffType> OnDebuffChanged;

    private Coroutine _debuffCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (SeasonalGauge.Instance != null)
        {
            SeasonalGauge.Instance.OnDebuffTriggered += ApplyDebuff;
            SeasonalGauge.Instance.OnDebuffEnded     += RemoveDebuff;
        }
    }

    private void OnDestroy()
    {
        if (SeasonalGauge.Instance != null)
        {
            SeasonalGauge.Instance.OnDebuffTriggered -= ApplyDebuff;
            SeasonalGauge.Instance.OnDebuffEnded     -= RemoveDebuff;
        }
    }

    /// <summary>계절에 따라 디버프 코루틴 시작</summary>
    private void ApplyDebuff(SeasonType season)
    {
        if (_debuffCoroutine != null) StopCoroutine(_debuffCoroutine);

        CurrentDebuff = season switch
        {
            SeasonType.Spring => DebuffType.Bound,
            SeasonType.Summer => DebuffType.Slow,
            SeasonType.Autumn => DebuffType.Confused,
            SeasonType.Winter => DebuffType.Frozen,
            _                 => DebuffType.None
        };

        OnDebuffChanged?.Invoke(CurrentDebuff);
        _debuffCoroutine = StartCoroutine(DebuffRoutine(CurrentDebuff));
    }

    /// <summary>디버프 해제</summary>
    private void RemoveDebuff()
    {
        if (_debuffCoroutine != null) { StopCoroutine(_debuffCoroutine); _debuffCoroutine = null; }
        ClearAllDebuffFlags();
        CurrentDebuff = DebuffType.None;
        OnDebuffChanged?.Invoke(DebuffType.None);
    }

    private IEnumerator DebuffRoutine(DebuffType type)
    {
        switch (type)
        {
            case DebuffType.Bound:    yield return ApplyBound();    break;
            case DebuffType.Slow:     yield return ApplySlow();     break;
            case DebuffType.Confused: yield return ApplyConfused(); break;
            case DebuffType.Frozen:   yield return ApplyFrozen();   break;
        }
    }

    /// <summary>봄 — 속박: 이동/공격/도구 불가, 공중 부양 불가</summary>
    private IEnumerator ApplyBound()
    {
        if (_movement != null) _movement.IsBound = true;
        yield return new WaitUntil(() => !SeasonalGauge.Instance.IsDebuffActive);
        if (_movement != null) _movement.IsBound = false;
    }

    /// <summary>여름 — 이속저하 + HP드레인</summary>
    private IEnumerator ApplySlow()
    {
        if (_movement != null) _movement.SpeedMultiplier = _slowMultiplier;
        while (SeasonalGauge.Instance.IsDebuffActive)
        {
            _character?.TakeDamage(_drainPerSecond * Time.deltaTime);
            yield return null;
        }
        if (_movement != null) _movement.SpeedMultiplier = 1f;
    }

    /// <summary>가을 — 혼란: 방향키 반전</summary>
    private IEnumerator ApplyConfused()
    {
        if (_movement != null) _movement.IsConfused = true;
        yield return new WaitUntil(() => !SeasonalGauge.Instance.IsDebuffActive);
        if (_movement != null) _movement.IsConfused = false;
    }

    /// <summary>겨울 — 빙결: 모든 행동 불가</summary>
    private IEnumerator ApplyFrozen()
    {
        if (_movement != null) _movement.IsFrozen = true;
        yield return new WaitUntil(() => !SeasonalGauge.Instance.IsDebuffActive);
        if (_movement != null) _movement.IsFrozen = false;
    }

    private void ClearAllDebuffFlags()
    {
        if (_movement == null) return;
        _movement.IsBound        = false;
        _movement.SpeedMultiplier = 1f;
        _movement.IsConfused     = false;
        _movement.IsFrozen       = false;
    }
}
