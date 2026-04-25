using System.Collections;
using UnityEngine;

/// <summary>
/// 공격 시 SlashFX 스프라이트 시트 기반 슬래시 이펙트를 재생합니다.
/// CharacterCombat이 있는 오브젝트에 함께 붙여 사용합니다.
/// </summary>
[RequireComponent(typeof(CharacterCombat))]
public class AttackEffect : MonoBehaviour
{
    [Header("Slash Frames")]
    // Inspector에서 슬라이싱한 흰색 SlashFX 프레임들을 순서대로 배열에 드래그
    [SerializeField] private Sprite[] _slashFrames;

    [Header("Settings")]
    // 초당 재생할 프레임 수 (높을수록 빠른 이펙트)
    [SerializeField] private float _frameRate = 24f;
    // 캐릭터 중심으로부터 이펙트가 표시될 위치 오프셋 (x: 좌우, y: 상하)
    [SerializeField] private Vector2 _offset = new Vector2(0.8f, 0.3f);
    // 이펙트 스케일 크기
    [SerializeField] private float _size = 1f;

    // CharacterCombat 컴포넌트 참조 — 공격 상태 감지용
    private CharacterCombat _combat;
    // 직전 프레임의 공격 상태 저장 — 공격 시작 엣지 감지용
    private bool _wasAttacking;

    private void Awake()
    {
        // 같은 오브젝트의 CharacterCombat 컴포넌트를 캐시
        _combat = GetComponent<CharacterCombat>();
    }

    private void Update()
    {
        // CanAttack이 false면 쿨다운 or 공격 중 → 공격 상태로 판단
        bool isAttacking = !_combat.CanAttack;

        // 이전 프레임은 비공격, 현재 프레임은 공격 → 공격 시작 시점 감지
        if (isAttacking && !_wasAttacking)
            StartCoroutine(PlaySlashEffect());

        // 현재 상태를 다음 프레임 비교용으로 저장
        _wasAttacking = isAttacking;
    }

    private IEnumerator PlaySlashEffect()
    {
        // 프레임 배열이 비어있으면 이펙트 재생 불가
        if (_slashFrames == null || _slashFrames.Length == 0) yield break;

        // 이펙트용 임시 GameObject 생성 — 씬 루트에 배치 (캐릭터 이동 영향 없음)
        var go = new GameObject("SlashFX");
        go.transform.SetParent(null);

        // localScale.x 부호로 캐릭터 바라보는 방향 판단 (양수=오른쪽, 음수=왼쪽)
        float dir = transform.localScale.x >= 0f ? 1f : -1f;

        // 방향에 따라 오프셋 x를 반전하여 이펙트 위치 결정
        go.transform.position = transform.position + new Vector3(dir * _offset.x, _offset.y, 0f);

        // 이펙트 스케일 설정
        go.transform.localScale = Vector3.one * _size;

        // SpriteRenderer 추가 및 기본 설정
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10; // 캐릭터보다 앞에 렌더링
        // 왼쪽 방향이면 스프라이트를 X축으로 뒤집어 방향 반전
        sr.flipX = dir < 0f;

        // 프레임 간격 계산 (1초 / frameRate)
        float interval = 1f / _frameRate;

        // 배열의 모든 프레임을 순서대로 표시
        for (int i = 0; i < _slashFrames.Length; i++)
        {
            // null 프레임은 건너뜀
            if (_slashFrames[i] == null) { yield return new WaitForSeconds(interval); continue; }

            // 현재 프레임 스프라이트 적용
            sr.sprite = _slashFrames[i];

            // 한 프레임 간격만큼 대기
            yield return new WaitForSeconds(interval);
        }

        // 모든 프레임 재생 완료 후 이펙트 오브젝트 제거
        Destroy(go);
    }
}
