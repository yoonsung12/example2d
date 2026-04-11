using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 카메라에 붙여서 플레이어를 부드럽게 추적.
/// Cinemachine 없이 동작하는 경량 버전 — 나중에 Cinemachine으로 교체 가능.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float     _smoothSpeed   = 5f;
    [SerializeField] private Vector3   _offset        = new Vector3(0f, 1f, -10f);
    [SerializeField] private bool      _useBounds;
    [SerializeField] private Vector2   _minBounds;
    [SerializeField] private Vector2   _maxBounds;

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // 씬 로드 완료 후 DontDestroyOnLoad로 살아있는 실제 플레이어를 직접 할당
    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        if (Player.Instance != null)
            _target = Player.Instance.transform;
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            if (Player.Instance != null) _target = Player.Instance.transform;
            return;
        }

        Vector3 desired = _target.position + _offset;

        if (_useBounds)
        {
            desired.x = Mathf.Clamp(desired.x, _minBounds.x, _maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, _minBounds.y, _maxBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 룸 진입 시 Room.cs에서 호출. 카메라가 룸 바깥을 비추지 않도록 바운드를 설정합니다.
    /// 직교 카메라 반-크기를 고려하여 실제 이동 가능 범위를 계산합니다.
    /// </summary>
    public void SetRoomBounds(Bounds roomBounds)
    {
        _useBounds = true;

        var cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        _minBounds = new Vector2(roomBounds.min.x + halfW, roomBounds.min.y + halfH);
        _maxBounds = new Vector2(roomBounds.max.x - halfW, roomBounds.max.y - halfH);

        // 룸이 카메라보다 작으면 룸 중심에 고정
        if (_minBounds.x > _maxBounds.x)
        {
            float cx = roomBounds.center.x;
            _minBounds.x = cx;
            _maxBounds.x = cx;
        }
        if (_minBounds.y > _maxBounds.y)
        {
            float cy = roomBounds.center.y;
            _minBounds.y = cy;
            _maxBounds.y = cy;
        }
    }
}
