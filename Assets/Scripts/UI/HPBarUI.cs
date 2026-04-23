using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HP를 5칸 하트 이미지로 표시한다.
/// CharacterBase.OnHealthChanged 이벤트를 구독해 갱신한다.
/// </summary>
public class HPBarUI : MonoBehaviour
{
    [SerializeField] private CharacterBase _player;       // 플레이어 CharacterBase
    [SerializeField] private Image[]       _heartImages;  // 하트 이미지 5개 (인스펙터에서 연결)
    [SerializeField] private Sprite        _heartFull;    // 꽉 찬 하트 스프라이트
    [SerializeField] private Sprite        _heartEmpty;   // 빈 하트 스프라이트

    private void Start()
    {
        if (_player != null)
        {
            _player.OnHealthChanged += Refresh;
            Refresh(_player.CurrentHealth, _player.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (_player != null) _player.OnHealthChanged -= Refresh;
    }

    /// <summary>HP 비율에 따라 하트 이미지를 갱신한다</summary>
    private void Refresh(float current, float max)
    {
        // 최대 HP를 하트 수로 나눠 칸당 HP 계산
        float hpPerHeart = max / _heartImages.Length;

        for (int i = 0; i < _heartImages.Length; i++)
        {
            if (_heartImages[i] == null) continue;
            bool filled = current > hpPerHeart * i;
            _heartImages[i].sprite = filled ? _heartFull : _heartEmpty;
        }
    }
}
