using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 라이터 도구 (D키).
/// 사용 시 Point Light2D가 서서히 확장되어 주변을 밝히고,
/// 종료 시 다시 수축하며 꺼진다.
/// 사용 중 반경 내 IMeltable 오브젝트를 녹인다.
/// </summary>
public class LighterTool : ToolBase
{
    [Header("Melt")]
    [SerializeField] private float     _meltRadius    = 2.5f; // 녹임 반경
    [SerializeField] private LayerMask _meltableLayers;       // 녹임 대상 레이어

    [Header("Light")]
    [SerializeField] private Light2D _pointLight;             // 플레이어 주변 Point Light2D

    [Header("Light Effect")]
    [SerializeField] private float _maxIntensity  = 3f;   // 완전히 켜졌을 때 밝기
    [SerializeField] private float _maxOuterRadius = 6f;  // 완전히 켜졌을 때 반경
    [SerializeField] private float _expandSpeed   = 6f;   // 확장/수축 속도 (단위/초)

    private Coroutine _lightRoutine;

    protected override void Awake()
    {
        base.Awake();

        // 초기 상태: 꺼진 조명 (크기 0, 강도 0)
        if (_pointLight != null)
        {
            _pointLight.intensity         = 0f;
            _pointLight.pointLightOuterRadius = 0f;
            _pointLight.enabled           = false;
        }
    }

    /// <summary>라이터 켜기 — 조명 서서히 확장 + 주변 IMeltable 녹임</summary>
    protected override void OnUse(Vector2 direction)
    {
        MeltNearby();
        StartLightTransition(expand: true);
    }

    /// <summary>라이터 끄기 — 조명 서서히 수축 후 비활성</summary>
    protected override void OnStopUse()
    {
        StartLightTransition(expand: false);
    }

    /// <summary>현재 진행 중인 전환 코루틴을 교체해 새 방향으로 시작</summary>
    private void StartLightTransition(bool expand)
    {
        if (_pointLight == null) return;
        if (_lightRoutine != null) StopCoroutine(_lightRoutine);
        _lightRoutine = StartCoroutine(LightTransitionRoutine(expand));
    }

    /// <summary>
    /// 조명의 intensity와 outerRadius를 목표값까지 부드럽게 변화시키는 코루틴.
    /// expand=true → 확장, expand=false → 수축 후 비활성.
    /// </summary>
    private IEnumerator LightTransitionRoutine(bool expand)
    {
        float targetIntensity = expand ? _maxIntensity  : 0f;
        float targetRadius    = expand ? _maxOuterRadius : 0f;

        if (expand) _pointLight.enabled = true;

        while (true)
        {
            _pointLight.intensity =
                Mathf.MoveTowards(_pointLight.intensity, targetIntensity, _expandSpeed * Time.deltaTime);
            _pointLight.pointLightOuterRadius =
                Mathf.MoveTowards(_pointLight.pointLightOuterRadius, targetRadius, _expandSpeed * 2f * Time.deltaTime);

            // 목표값 도달 판정
            bool done = Mathf.Approximately(_pointLight.intensity, targetIntensity) &&
                        Mathf.Approximately(_pointLight.pointLightOuterRadius, targetRadius);
            if (done) break;

            yield return null;
        }

        // 수축 완료 시 오브젝트 비활성
        if (!expand) _pointLight.enabled = false;
        _lightRoutine = null;
    }

    /// <summary>_meltRadius 범위 내 IMeltable 오브젝트에 Melt() 호출</summary>
    private void MeltNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _meltRadius, _meltableLayers);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IMeltable>(out var meltable))
                meltable.Melt();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _meltRadius);
    }
}
