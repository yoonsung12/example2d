using System.Collections;
using UnityEngine;

/// <summary>
/// 공격 시 간결한 슬래시 이펙트를 생성합니다.
/// CharacterCombat이 있는 오브젝트에 함께 붙여 사용합니다.
/// </summary>
[RequireComponent(typeof(CharacterCombat))]
public class AttackEffect : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private Color  _effectColor    = new Color(1f, 1f, 0.6f, 0.85f);
    [SerializeField] private float  _duration       = 0.12f;
    [SerializeField] private float  _size           = 0.6f;

    private CharacterCombat _combat;
    private bool            _wasAttacking;

    private void Awake()
    {
        _combat = GetComponent<CharacterCombat>();
    }

    private void Update()
    {
        bool isAttacking = !_combat.CanAttack;

        // 공격 시작 엣지 감지
        if (isAttacking && !_wasAttacking)
            StartCoroutine(SpawnEffect());

        _wasAttacking = isAttacking;
    }

    private IEnumerator SpawnEffect()
    {
        var go = new GameObject("AttackFX");
        go.transform.SetParent(null);

        // 히트박스 방향으로 오프셋
        float dir = transform.localScale.x > 0 ? 1f : -1f;
        go.transform.position = transform.position + new Vector3(dir * _size, 0.3f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GetBuiltinSprite();
        sr.color        = _effectColor;
        sr.sortingOrder = 10;
        go.transform.localScale = new Vector3(_size * 2.5f, _size * 1.2f, 1f);

        // 회전 (슬래시 느낌)
        go.transform.rotation = Quaternion.Euler(0f, 0f, dir > 0 ? -30f : 210f);

        // 페이드 아웃
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;
            float scale = Mathf.Lerp(1f, 1.4f, t);
            go.transform.localScale = new Vector3(_size * 2.5f * scale, _size * 1.2f, 1f);
            sr.color = new Color(_effectColor.r, _effectColor.g, _effectColor.b,
                                 Mathf.Lerp(_effectColor.a, 0f, t));
            yield return null;
        }

        Destroy(go);
    }

    private static Sprite GetBuiltinSprite()
    {
        var tex = new Texture2D(4, 4);
        tex.SetPixels(System.Array.ConvertAll(tex.GetPixels(), _ => Color.white));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}
