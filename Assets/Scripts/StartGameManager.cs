using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameManager : MonoBehaviour
{
    // 1. Hand 트래킹 모드로 게임 시작 버튼에 연결
    public void OnClickHandControl()
    {
        PlayerPrefs.SetString("ControlType", "Hand");
        PlayerPrefs.Save();
        UnityEngine.Debug.Log("[StartGameManager] 컨트롤 모드: Hand 설정 완료");
        
        LoadGameScene();
    }

    // 2. Keyboard 모드로 게임 시작 버튼에 연결
    public void OnClickKeyboardControl()
    {
        PlayerPrefs.SetString("ControlType", "Keyboard");
        PlayerPrefs.Save();
        UnityEngine.Debug.Log("[StartGameManager] 컨트롤 모드: Keyboard 설정 완료");

        LoadGameScene();
    }

    // 기존 단일 시작 버튼용
    public void OnClickStartGame()
    {
        LoadGameScene();
    }

    public void OnClickQuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene("Game");
    }
}