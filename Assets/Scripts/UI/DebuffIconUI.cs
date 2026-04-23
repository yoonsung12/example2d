using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 활성 디버프 아이콘을 표시한다.
/// DebuffManager.OnDebuffChanged 이벤트를 구독해 갱신한다.
/// </summary>
public class DebuffIconUI : MonoBehaviour
{
    [Header("아이콘 이미지 (없으면 숨김)")]
    [SerializeField] private Image _iconImage;

    [Header("디버프별 스프라이트")]
    [SerializeField] private Sprite _boundSprite;    // 봄 속박
    [SerializeField] private Sprite _slowSprite;     // 여름 이속저하
    [SerializeField] private Sprite _confusedSprite; // 가을 혼란
    [SerializeField] private Sprite _frozenSprite;   // 겨울 빙결

    private void Start()
    {
        if (DebuffManager.Instance != null)
            DebuffManager.Instance.OnDebuffChanged += Refresh;

        Refresh(DebuffType.None);
    }

    private void OnDestroy()
    {
        if (DebuffManager.Instance != null)
            DebuffManager.Instance.OnDebuffChanged -= Refresh;
    }

    /// <summary>디버프 종류에 맞는 아이콘을 표시하고, None이면 숨긴다</summary>
    private void Refresh(DebuffType type)
    {
        if (_iconImage == null) return;

        Sprite icon = type switch
        {
            DebuffType.Bound    => _boundSprite,
            DebuffType.Slow     => _slowSprite,
            DebuffType.Confused => _confusedSprite,
            DebuffType.Frozen   => _frozenSprite,
            _                   => null
        };

        _iconImage.gameObject.SetActive(icon != null);
        if (icon != null) _iconImage.sprite = icon;
    }
}
