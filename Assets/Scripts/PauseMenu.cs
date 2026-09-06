using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI 오브젝트")]
    public GameObject pauseMenuUI;

    [Header("씬 이름")]
    public string titleSceneName = "TitleScene";

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // 1. 이어하기 (게임 재개 + 마우스 커서 숨기기)
    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // 마우스 커서 비활성화 및 화면 중앙 고정
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // 2. 일시정지 (설정창 켜기 + 마우스 커서 표시)
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // 마우스 커서 활성화 및 이동 자유화
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // 3. 타이틀로 돌아가기
    public void GoToTitle()
    {
        Time.timeScale = 1f;
        
        // 타이틀 화면에서도 마우스가 보이도록 설정
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        SceneManager.LoadScene(titleSceneName);
    }

    // 4. 게임 종료
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}