using UnityEngine;

/// <summary>
/// 꽃가루가 바람을 받으면 날려가고, 설정된 맵 경계를 벗어나면 제거되는 컴포넌트.
/// FallingParticle의 자체 낙하 이동과 합산되어 동작한다.
/// </summary>
public class PollenWindBehavior : MonoBehaviour, IWindAffectable
{
    [Header("Wind Response")]
    [SerializeField] private float _windForceMultiplier = 0.8f; // FanTool force에 곱해 실제 날아가는 세기 보정
    [SerializeField] private float _drag = 1.5f;                // 공기 저항 — 높을수록 빠르게 감속

    [Header("Despawn Bounds")]
    [SerializeField] private float _boundsMinX = -30f; // 이 X 좌표 미만이면 제거
    [SerializeField] private float _boundsMaxX =  30f; // 이 X 좌표 초과하면 제거
    [SerializeField] private float _boundsMinY = -15f; // 이 Y 좌표 미만이면 제거
    [SerializeField] private float _boundsMaxY =  20f; // 이 Y 좌표 초과하면 제거

    private Vector2 _windVelocity; // 바람에 의해 누적된 속도 — FallingParticle 이동에 더해짐

    private void Update()
    {
        // 바람 속도를 이동에 반영 (FallingParticle.Update의 낙하 이동과 합산)
        transform.position += (Vector3)(_windVelocity * Time.deltaTime);

        // 지수 감쇠 방식 공기 저항 — 프레임률에 무관하게 동일한 감속 곡선 유지
        _windVelocity *= Mathf.Max(0f, 1f - _drag * Time.deltaTime);

        // 맵 경계 초과 시 제거
        Vector3 pos = transform.position;
        if (pos.x < _boundsMinX || pos.x > _boundsMaxX ||
            pos.y < _boundsMinY || pos.y > _boundsMaxY)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>FanTool이 바람 방향과 세기를 전달 — 누적 속도에 더함</summary>
    public void OnWind(Vector2 windDirection, float force)
    {
        _windVelocity += windDirection * (force * _windForceMultiplier);
    }
}
