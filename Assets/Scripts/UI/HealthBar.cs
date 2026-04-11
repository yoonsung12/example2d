using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider        _slider;
    [SerializeField] private CharacterBase _target;

    private void Start()
    {
        // 타겟 미지정 시 플레이어 자동 탐색
        if (_target == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) _target = pc.GetComponent<CharacterBase>();
        }
    }

    private void Update()
    {
        if (_target == null || _slider == null) return;
        _slider.value = _target.CurrentHealth / _target.MaxHealth;
    }

    public void SetTarget(CharacterBase target) => _target = target;
}
