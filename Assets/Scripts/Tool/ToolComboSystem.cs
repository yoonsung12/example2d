using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 두 도구 조합 시 발동되는 특수 효과를 관리한다.
/// ToolHolder가 2개 도구를 보유한 상태에서 사용 시도 시 이 클래스에 위임한다.
/// 조합표: 선풍기+우산→공중부양 / 선풍기+라이터→원거리열풍 / 우산+라이터→활공
/// </summary>
public class ToolComboSystem : MonoBehaviour
{
    // 조합 테이블 — (a,b) 순서 무관하게 TryCombo에서 양방향 조회
    private static readonly Dictionary<(ToolType, ToolType), ComboType> ComboMap =
        new Dictionary<(ToolType, ToolType), ComboType>
        {
            { (ToolType.Fan,      ToolType.Umbrella), ComboType.AerialLift },
            { (ToolType.Fan,      ToolType.Lighter),  ComboType.HeatWind   },
            { (ToolType.Umbrella, ToolType.Lighter),  ComboType.Glide      },
        };

    [Header("AerialLift — 공중 부양")]
    [SerializeField] private float _aerialLiftForce = 28f; // 위로 가해지는 충격량

    [Header("HeatWind — 원거리 열풍")]
    [SerializeField] private float _heatWindRange   = 10f; // 열풍 범위
    [SerializeField] private float _heatWindForce   = 22f; // 적에게 가해지는 힘
    [SerializeField] private float _heatWindDamage  = 1f;  // 열풍 피해량

    [Header("Glide — 활공")]
    [SerializeField] private float _glideGravityScale   = 0.15f; // 활공 중 중력 배율
    [SerializeField] private float _glideMoveMultiplier = 0.3f;  // 활공 중 수평 이동 배율

    private Rigidbody2D        _rb;
    private PlatformerMovement _movement;

    public bool IsGliding { get; private set; } // 현재 활공 중 여부

    private void Awake()
    {
        _rb       = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlatformerMovement>();
    }

    /// <summary>두 도구 타입으로 조합을 시도한다. 조합 성공 시 true 반환</summary>
    public bool TryCombo(ToolType a, ToolType b, Vector2 direction)
    {
        // 순서 무관 조회: (a,b) 없으면 (b,a) 시도
        if (!ComboMap.TryGetValue((a, b), out ComboType combo) &&
            !ComboMap.TryGetValue((b, a), out combo))
            return false;

        ExecuteCombo(combo, direction);
        return true;
    }

    /// <summary>콤보 종류에 따라 해당 효과 실행</summary>
    private void ExecuteCombo(ComboType combo, Vector2 direction)
    {
        switch (combo)
        {
            case ComboType.AerialLift: DoAerialLift();        break;
            case ComboType.HeatWind:   DoHeatWind(direction); break;
            case ComboType.Glide:      DoGlide();             break;
        }
    }

    /// <summary>공중 부양: 위로 강한 충격량 적용</summary>
    private void DoAerialLift()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f); // 수직 속도 초기화
        _rb.AddForce(Vector2.up * _aerialLiftForce, ForceMode2D.Impulse);
    }

    /// <summary>원거리 열풍: 범위 내 IDamageable에 피해 + Rigidbody2D에 날림 힘</summary>
    private void DoHeatWind(Vector2 direction)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _heatWindRange);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<Rigidbody2D>(out var rb))
                rb.AddForce(direction * _heatWindForce, ForceMode2D.Impulse);

            if (hit.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(_heatWindDamage, (Vector2)transform.position);
        }
    }

    /// <summary>활공 시작: S+D 누르는 동안 중력 축소 + 이동 속도 감소</summary>
    private void DoGlide() => StartGlide();

    /// <summary>활공 시작 (ToolHolder에서 S+D 감지 시 호출)</summary>
    public void StartGlide()
    {
        if (IsGliding) return;                                        // 이미 활공 중이면 무시
        IsGliding              = true;
        _rb.gravityScale       = _glideGravityScale;                  // 중력 축소
        if (_movement != null)
        {
            _movement.SuppressFallMultiplier = true;                  // 낙하 가속 억제
            _movement.GlideSpeedMultiplier   = _glideMoveMultiplier;  // 수평 이동 감소
        }
    }

    /// <summary>활공 종료 (ToolHolder에서 S 또는 D 뗄 때 호출)</summary>
    public void StopGlide()
    {
        if (!IsGliding) return;                                       // 활공 중이 아니면 무시
        IsGliding        = false;
        _rb.gravityScale = 1f;                                        // 중력 복원
        if (_movement != null)
        {
            _movement.SuppressFallMultiplier = false;                 // 낙하 가속 복원
            _movement.GlideSpeedMultiplier   = 1f;                    // 이동 속도 복원
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _heatWindRange);
    }
}
