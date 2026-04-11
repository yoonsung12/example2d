using UnityEngine;

/// <summary>
/// 플레이어가 밟거나 상호작용하면 현재 위치와 HP를 저장.
/// </summary>
public class Checkpoint : MonoBehaviour, IInteractable
{
    [SerializeField] private string _checkpointId;

    private bool _activated;

    public string InteractPrompt => _activated ? "" : "Activate Checkpoint";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) Activate(other.transform);
    }

    public void Interact() => Activate(null);

    private void Activate(Transform playerTransform)
    {
        if (_activated) return;
        _activated = true;

        if (playerTransform == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
        }

        if (playerTransform == null) return;

        float health = playerTransform.GetComponent<CharacterBase>()?.CurrentHealth ?? 0f;
        SaveManager.Instance?.Save(playerTransform.position, health);
        Debug.Log($"[Checkpoint] {_checkpointId} activated.");
    }
}
