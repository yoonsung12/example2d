using UnityEngine;

/// <summary>
/// 선풍기 바람에 스윽 밀리되 날아가지 않는 오브젝트 컴포넌트.
/// Rigidbody2D의 높은 선형 감쇠와 최대 속도 제한으로 가벼운 슬라이딩만 허용한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ObjectWindBehavior : MonoBehaviour, IWindAffectable
{
    [Header("Push Settings")]
    [SerializeField] private float _pushForce  = 4f;  // OnWind 1회당 수평 방향으로 가하는 힘
    [SerializeField] private float _maxSpeed   = 1.5f; // 바람으로 인한 최대 허용 속도 (날아가지 않게)
    [SerializeField] private float _linearDrag = 10f;  // 선형 감쇠 — 높을수록 빠르게 정지

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.linearDamping = _linearDrag;                                    // 선형 감쇠 적용
        _rb.constraints   = RigidbodyConstraints2D.FreezeRotation           // 회전 고정
                          | RigidbodyConstraints2D.FreezePositionY;         // Y축 고정 — 위로 날아가지 않게
    }

    /// <summary>FanTool이 바람 방향과 세기를 전달 — 수평 성분만 힘으로 변환</summary>
    public void OnWind(Vector2 windDirection, float force)
    {
        // Y 성분 제거 — 수평 방향으로만 밀림 (위로 뜨는 현상 방지)
        Vector2 horizontalDir = new Vector2(windDirection.x, 0f);
        if (horizontalDir == Vector2.zero) return; // 위 방향 바람은 이 오브젝트에 영향 없음

        _rb.AddForce(horizontalDir * _pushForce, ForceMode2D.Force);

        // 속도 초과 시 클램프 — FanTool의 직접 AddForce와 합산되더라도 날아가지 않게
        if (_rb.linearVelocity.magnitude > _maxSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * _maxSpeed;
    }
}
