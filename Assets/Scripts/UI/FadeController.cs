using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 전환용 검정 페이드 인/아웃.
/// Managers 오브젝트와 별개로 씬 루트에 단독 배치 — DontDestroyOnLoad.
/// FadeOut(): 투명→검정, FadeIn(): 검정→투명 (씬 로드 시 자동 호출).
/// </summary>
public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [SerializeField] private float _fadeDuration = 0.5f;

    private CanvasGroup _group;

    // ── Unity 생명주기 ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        _group.alpha = 1f; // 시작 시 검정(로드 직후 FadeIn 대기)
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // 씬 로드 완료 → 자동 페이드 인
    private void OnSceneLoaded(Scene _, LoadSceneMode __) => StartCoroutine(DoFadeIn());

    // ── 공개 API ────────────────────────────────────────────────────────────

    /// <summary>페이드 아웃(투명→검정). SceneTransition에서 yield return으로 사용.</summary>
    public IEnumerator FadeOut() => Fade(0f, 1f);

    // ── 내부 구현 ────────────────────────────────────────────────────────────

    private IEnumerator DoFadeIn()
    {
        _group.alpha = 1f;
        yield return null; // 씬 첫 프레임 안정화 대기
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        _group.alpha = from;
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
            yield return null;
        }
        _group.alpha = to;
    }

    // 런타임에 Canvas + 검정 이미지를 자신의 자식으로 생성
    private void BuildOverlay()
    {
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        _group = canvasGo.AddComponent<CanvasGroup>();
        _group.blocksRaycasts  = false;
        _group.interactable    = false;

        var panelGo = new GameObject("BlackPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);

        var img = panelGo.AddComponent<Image>();
        img.color = Color.black;

        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
