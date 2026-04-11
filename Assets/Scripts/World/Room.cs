using UnityEngine;

/// <summary>
/// 룸 영역 트리거. 플레이어 진입 시 저장 기록 + 카메라 바운드 갱신.
/// Cinemachine CinemachineConfiner2D 세팅 시 _cameraBounds에 PolygonCollider2D 할당.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Room : MonoBehaviour
{
    [SerializeField] private string     _roomId;
    [SerializeField] private Collider2D _cameraBounds;

    public string     RoomId       => _roomId;
    public Collider2D CameraBounds => _cameraBounds;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        SaveManager.Instance?.AddVisitedRoom(_roomId);
        // TODO: Cinemachine Confiner 갱신은 카메라 세팅 후 여기서 호출
    }
}
