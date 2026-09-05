using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoldenTimeUI : MonoBehaviour
{
    public Image gaugeFillImage;        // Fill 속성이 설정된 게이지 Image
    public TextMeshProUGUI timerText;   // 남은 시간을 보여줄 텍스트

    private float maxTime;

    private void OnEnable()
    {
        // RescueManager의 타이머 갱신 이벤트 구독
        if (RescueManager.Instance != null)
        {
            RescueManager.Instance.OnTimerUpdated += UpdateUI;
            
            // 초기 시간 세팅
            if (RescueManager.Instance.currentRescueData != null)
            {
                maxTime = RescueManager.Instance.currentRescueData.goldenTime;
                UpdateUI(RescueManager.Instance.remainingGoldenTime);
            }
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (RescueManager.Instance != null)
        {
            RescueManager.Instance.OnTimerUpdated -= UpdateUI;
        }
    }

    private void UpdateUI(float remainingTime)
    {
        if (maxTime <= 0f) return;

        // remainingGoldenTime 비율 계산
        float fillRatio = Mathf.Clamp01(remainingTime / maxTime);

        if (gaugeFillImage != null)
        {
            gaugeFillImage.fillAmount = fillRatio;
        }

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}