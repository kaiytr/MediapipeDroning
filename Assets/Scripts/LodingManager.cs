using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 시 필요 (기존 Text 사용 시 UnityEngine.UI의 Text 사용)

public class LoadingManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI loadingText; // 기존 UI Text 사용 시 Text 타입으로 변경
    void Start()
    {
        LoadScene("Map2");
    }
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // 1. 로딩 UI 활성화 및 초기 텍스트 설정
        loadingPanel.SetActive(true);
        if (loadingText != null)
        {
            loadingText.text = "로딩 중...";
        }

        // 2. 비동기 씬 로드 시작 (자동 전환 비활성화)
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 3. 씬 데이터 로딩 진행 (0.9 미만 구간)
        while (op.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            yield return null;
        }

        // 4. 로딩 완료 상태 처리
        if (progressBar != null)
        {
            progressBar.value = 1f; // 진행바 100% 고정
        }

        if (loadingText != null)
        {
            loadingText.text = "로딩 완료!"; // 텍스트 변경
        }

        // 5. 2초간 대기
        yield return new WaitForSeconds(2.0f);

        // 6. 씬 전환 실행
        op.allowSceneActivation = true;
    }
}
