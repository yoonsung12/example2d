using UnityEngine;

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

    private void LateUpdate()
    {
        if (_target == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) _target = pc.transform;
            return;
        }

        Vector3 desired  = _target.position + _offset;

        if (_useBounds)
        {
            desired.x = Mathf.Clamp(desired.x, _minBounds.x, _maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, _minBounds.y, _maxBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
    }
}
