using UnityEngine;

/// <summary>
/// CharacterBase 구현체. Player GameObject에 붙이는 최상위 컴포넌트.
/// PlayerController, PlayerAbilities와 함께 사용.
/// </summary>
public class Player : CharacterBase
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDamaged(float damage)
    {
        // TODO: 피격 이펙트, 무적 프레임 등 추가 가능
    }

    protected override void OnDeath()
    {
        // 사망 처리 — GameManager에 알림
        GameManager.Instance?.GameOver();
    }
}
