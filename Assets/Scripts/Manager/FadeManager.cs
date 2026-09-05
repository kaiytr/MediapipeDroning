using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [Header("UI 요소")]
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 페이드 효과용 CanvasGroup
    [SerializeField] private float defaultFadeDuration = 1.0f; // 기본 페이드 시간

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 첫 씬 시작 시 화면을 밝게 만들어 줌 (Fade In)
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            StartCoroutine(FadeInRoutine(defaultFadeDuration));
        }
    }

    /// <summary>
    /// Fade Out 효과 후 지정한 씬으로 이동
    /// </summary>
    public void LoadSceneWithFade(string sceneName, float duration = -1f)
    {
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        StartCoroutine(FadeOutAndLoadRoutine(sceneName, fadeDuration));
    }

    private IEnumerator FadeOutAndLoadRoutine(string sceneName, float duration)
    {
        fadeCanvasGroup.blocksRaycasts = true; // 페이드 중 클릭 방지

        // 1. 화면 어둡게 (Fade Out)
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // 2. 비동기 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 3. 화면 다시 밝게 (Fade In)
        yield return StartCoroutine(FadeInRoutine(duration));
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / duration));
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false; // 클릭 방지 해제
    }
}