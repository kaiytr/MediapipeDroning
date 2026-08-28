using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance { get; private set; }

    [Header("Game State")]
    public int currentScore = 0;
    public bool isGameOver = false;

    [Header("Control Settings")]
    public string currentControlType;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!PlayerPrefs.HasKey("ControlType"))
        {
            PlayerPrefs.SetString("ControlType", "Hand");
            PlayerPrefs.Save();
        }

        currentControlType = PlayerPrefs.GetString("ControlType", "Hand");
    }

    void Start()
    {
        UnityEngine.Debug.Log("[InGameManager] 현재 컨트롤 모드: " + currentControlType);
        InitGame();
    }

    void InitGame()
    {
        currentScore = 0;
        isGameOver = false;
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        currentScore += amount;
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        PlayerPrefs.SetInt("FinalScore", currentScore);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void GoToStartMenu()
    {
        SceneManager.LoadScene("Start");
    }
}