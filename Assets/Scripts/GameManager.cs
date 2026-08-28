using UnityEngine;
using UnityEngine.SceneManagement;

public enum ControlMode
{
    Keyboard,
    HandGesture
}

public class GameManager : MonoBehaviour
{
    public static ControlMode SelectedMode = ControlMode.Keyboard;

    // Start 씬의 키보드 버튼 OnClick에 연결 (Index: 0)
    public void SelectKeyboardMode()
    {
        SelectedMode = ControlMode.Keyboard;
        LoadGameScene();
    }

    // Start 씬의 손 제스처 버튼 OnClick에 연결 (Index: 1)
    public void SelectHandGestureMode()
    {
        SelectedMode = ControlMode.HandGesture;
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        // Build Settings에서 Game 씬의 이름 또는 인덱스와 일치해야 합니다.
        SceneManager.LoadScene("Game");
    }
}