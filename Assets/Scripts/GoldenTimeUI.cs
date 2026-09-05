using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoldenTimeUI : MonoBehaviour
{
    public Image gaugeFillImage;        // Fill 속성이 설정된 게이지 Image
    public TextMeshProUGUI timerText;   // 남은 시간을 보여줄 텍스트 (선택 사항)

    [Header("Test Settings")]
    public float testGoldenTime = 10f;  // 테스트용 제한시간

    private float maxTime;
    private float currentTime;
    private bool isTimerRunning = false;

    void Start()
    {
        StartTestTimer();
    }

    public void StartTestTimer()
    {
        maxTime = testGoldenTime;
        currentTime = maxTime;
        isTimerRunning = true;
        UpdateGaugeUI();
    }

    public void SetupRescueTimer(RescueData data)
    {
        if (data == null) return;

        maxTime = data.goldenTime;
        currentTime = maxTime;
        isTimerRunning = true;

        UpdateGaugeUI();
    }

    void Update()
    {
        if (!isTimerRunning) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateGaugeUI();
        }
        else
        {
            currentTime = 0;
            isTimerRunning = false;
            UpdateGaugeUI();
        }
    }

    private void UpdateGaugeUI()
    {
        float fillRatio = Mathf.Clamp01(currentTime / maxTime);

        if (gaugeFillImage != null)
        {
            gaugeFillImage.fillAmount = fillRatio;
        }

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60F);
            int seconds = Mathf.FloorToInt(currentTime % 60F);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}