using UnityEngine;

/// <summary>
/// 은행 개별 동작.
/// 낙하 중 우산에 닿으면 터지지 않고 사라짐.
/// 바닥에 닿으면 터지며 냄새 범위(트리거)를 생성, 플레이어가 범위 안에 있으면 가을 게이지 추가.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class GinkgoNut : MonoBehaviour
{
    [SerializeField] private float _gaugeAmountPerSec = 10f;  // 냄새 범위 내 초당 가을 게이지 증가량
    [SerializeField] private float _smellRadius       = 1.5f; // 터진 후 냄새 범위 반경
    [SerializeField] private float _smellDuration     = 3f;   // 냄새가 유지되는 시간 (초)
    [SerializeField] private float _swayAmplitude     = 0.15f;// 낙하 중 좌우 흔들림 폭
    [SerializeField] private float _swayFrequency     = 1f;   // 좌우 흔들림 빈도

    private Rigidbody2D      _rb;        // 물리 연산 컴포넌트 — 중력·속도 제어
    private CircleCollider2D _col;       // 충돌 콜라이더 — 낙하 충돌 및 냄새 범위로 전환
    private SpriteRenderer   _sr;        // 스프라이트 렌더러 — 터질 때 비주얼 숨김
    private bool             _hasBurst;  // 이미 터진 상태 플래그 — 중복 처리 방지
    private float            _elapsed;   // 경과 시간 — 흔들림 계산용

    private static int _umbrellaLayer = -1;  // UmbrellaShield 레이어 인덱스 캐시
    private static int _groundLayer   = -1;  // Ground 레이어 인덱스 캐시
    private static int _playerLayer   = -1;  // Player 레이어 인덱스 캐시

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();     // Rigidbody2D 캐시
        _col = GetComponent<CircleCollider2D>(); // CircleCollider2D 캐시
        _sr  = GetComponent<SpriteRenderer>();   // SpriteRenderer 캐시 (없을 수도 있음)

        // 레이어 인덱스 최초 1회 조회
        if (_umbrellaLayer == -1) _umbrellaLayer = LayerMask.NameToLayer("UmbrellaShield");
        if (_groundLayer   == -1) _groundLayer   = LayerMask.NameToLayer("Ground");
        if (_playerLayer   == -1) _playerLayer   = LayerMask.NameToLayer("Player");
    }

    private void Update()
    {
        if (_hasBurst) return;  // 터진 후에는 Update 로직 불필요

        _elapsed += Time.deltaTime;  // 경과 시간 누적

        // sin 곡선 도함수(cos)로 수평 속도를 제어 — 좌우 흔들림 구현
        float swayVelocityX = Mathf.Cos(_elapsed * _swayFrequency) * _swayAmplitude * _swayFrequency;
        _rb.linearVelocity = new Vector2(swayVelocityX, _rb.linearVelocity.y);  // 수직은 중력 유지

        // 흔들림 방향에 따라 자연스럽게 회전
        _rb.angularVelocity = swayVelocityX * 60f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 낙하 중에 우산 방어막에 닿으면 터지지 않고 사라짐
        if (!_hasBurst && other.gameObject.layer == _umbrellaLayer)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // 바닥에 닿으면 터짐 (이미 터졌으면 무시)
        if (!_hasBurst && col.gameObject.layer == _groundLayer)
            Burst();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 냄새 범위(트리거 전환 후)에 플레이어가 머물면 가을 게이지 지속 추가
        if (_hasBurst && other.gameObject.layer == _playerLayer)
            SeasonalGauge.Instance?.AddGauge(SeasonType.Autumn, _gaugeAmountPerSec * Time.deltaTime);
    }

    private void Burst()
    {
        _hasBurst = true;  // 터진 상태로 전환 — 이후 중복 충돌 무시

        if (_sr != null) _sr.enabled = false;  // 은행 비주얼 숨김

        _rb.linearVelocity = Vector2.zero;  // 이동 즉시 정지
        _rb.isKinematic    = true;          // 물리 비활성 — 중력·외부 힘 영향 차단

        _col.isTrigger = true;         // 콜라이더를 트리거로 전환 — 냄새 범위로 사용
        _col.radius    = _smellRadius; // 냄새 범위 반경으로 확장

        Destroy(gameObject, _smellDuration);  // smellDuration 초 후 냄새와 함께 제거
    }
}
