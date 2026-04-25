using UnityEngine;

/// <summary>
/// 꽃가루·산성비·눈 공용 낙하 입자.
/// 우산 방어막(UmbrellaShield)에 닿으면 차단, 플레이어에 닿으면 계절 게이지 추가.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FallingParticle : MonoBehaviour
{
    [SerializeField] private SeasonType _season;          // 이 입자가 속하는 계절 (게이지 종류 결정)
    [SerializeField] private float _gaugeAmount   = 3f;   // 플레이어 충돌 시 추가할 게이지량
    [SerializeField] private float _fallSpeed     = 2f;   // 낙하 속도 (단위/초)
    [SerializeField] private float _swayAmplitude = 0f;   // 좌우 흔들림 폭 (0이면 직선 낙하)
    [SerializeField] private float _swayFrequency = 1f;   // 좌우 흔들림 빈도
    [SerializeField] private float _lifetime      = 12f;  // 최대 생존 시간 — 화면 밖 입자 자동 제거
    [SerializeField] private AudioClip _umbrellaHitClip;  // 우산에 맞을 때 재생 (여름 전용)

    private float _startX;   // 생성 시 X 좌표 — 흔들림 기준점으로 사용
    private float _elapsed;  // 경과 시간 — 흔들림·수명 계산용

    private static int _umbrellaLayer = -1;  // UmbrellaShield 레이어 인덱스 캐시
    private static int _playerLayer   = -1;  // Player 레이어 인덱스 캐시

    private void Awake()
    {
        // 레이어 인덱스는 변하지 않으므로 최초 1회만 조회
        if (_umbrellaLayer == -1) _umbrellaLayer = LayerMask.NameToLayer("UmbrellaShield");
        if (_playerLayer   == -1) _playerLayer   = LayerMask.NameToLayer("Player");

        _startX  = transform.position.x;             // 흔들림 기준 X 저장
        _elapsed = Random.Range(0f, Mathf.PI * 2f);  // 위상 랜덤화 — 동시 생성 입자 동기화 방지
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;  // 경과 시간 증가

        // 아래 방향으로 낙하
        transform.position += Vector3.down * _fallSpeed * Time.deltaTime;

        // 좌우 sin 곡선 흔들림 (꽃가루·눈 살랑살랑 효과)
        if (_swayAmplitude > 0f)
        {
            float x = _startX + Mathf.Sin(_elapsed * _swayFrequency) * _swayAmplitude;  // sin으로 X 위치 결정
            transform.position = new Vector3(x, transform.position.y, transform.position.z);  // X만 교체
        }

        // 수명 초과 시 자동 제거
        if (_elapsed >= _lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 우산 방어막에 닿은 경우
        if (other.gameObject.layer == _umbrellaLayer)
        {
            // 여름 비만 우산 충돌 시 빗소리 재생
            if (_season == SeasonType.Summer && _umbrellaHitClip != null)
                AudioSource.PlayClipAtPoint(_umbrellaHitClip, transform.position);

            Destroy(gameObject);  // 우산에 막혀 사라짐
            return;
        }

        // 플레이어에 닿은 경우 — 해당 계절 게이지 추가
        if (other.gameObject.layer == _playerLayer)
        {
            SeasonalGauge.Instance?.AddGauge(_season, _gaugeAmount);  // 계절 게이지 누적
            Destroy(gameObject);  // 입자 제거
        }
    }
}
