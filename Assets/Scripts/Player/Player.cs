using UnityEngine;

/// <summary>
/// CharacterBase 구현체. Player GameObject에 붙이는 최상위 컴포넌트.
/// PlayerController, PlayerAbilities와 함께 사용.
/// </summary>
public class Player : CharacterBase
{
    // 씬 전환 후에도 플레이어 인스턴스를 유지하기 위한 싱글턴 참조
    public static Player Instance { get; private set; }

    protected override void Awake()
    {
        // 이미 다른 씬에서 넘어온 플레이어가 존재하면 이 오브젝트(씬 기본 배치)를 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
