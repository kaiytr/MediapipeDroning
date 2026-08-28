using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeliveryItem : MonoBehaviour
{
    [Header("음식 설정")]
    public string itemName = "따끈한 피자";
    public float maxTemp = 100f;        // 최고 온도
    public float currentTemp;          // 현재 온도
    public float coolingRate = 2f;      // 초당 식는 속도
    public bool isDelivering = false;   // 배달 진행 여부

    [Header("구역 태그 설정")]
    public string startZoneTag = "StartZone"; // 시작 구역 태그
    public string endZoneTag = "EndZone";     // 종료 구역 태그

    [Header("UI 컴포넌트 연결")]
    public Slider tempSlider;           // 온도 게이지 바
    public Image fillImage;            // 게이지 채우기 이미지
    public TextMeshProUGUI tempText;   // 온도 수치 텍스트
    public Gradient tempColorGradient; // 온도가 낮아짐에 따른 색상 변화

    void Start()
    {
        currentTemp = maxTemp;
        UpdateUI();
    }

    void Update()
    {
        if (!isDelivering) return;

        if (currentTemp > 0)
        {
            currentTemp -= coolingRate * Time.deltaTime;
            currentTemp = Mathf.Clamp(currentTemp, 0f, maxTemp);
            UpdateUI();
        }
        else
        {
            OnFoodCold();
        }
    }

    // 3D 트리거 감지 (2D 게임인 경우 OnTriggerEnter2D(Collider2D other) 사용)
    private void OnTriggerEnter(Collider other)
    {
        // 시작 구역 진입 시
        if (other.CompareTag(startZoneTag) && !isDelivering)
        {
            StartDelivery();
            Debug.Log("배달 시작 구역 진입: 온도가 떨어지기 시작합니다.");
        }
        // 종료 구역 진입 시
        else if (other.CompareTag(endZoneTag) && isDelivering)
        {
            StopDelivery();
            Debug.Log($"배달 완료! 최종 음식 온도: {Mathf.RoundToInt(currentTemp)}°C");
        }
    }

    void UpdateUI()
    {
        float ratio = currentTemp / maxTemp;

        if (tempSlider != null)
        {
            tempSlider.value = ratio;
        }

        if (fillImage != null && tempColorGradient != null)
        {
            fillImage.color = tempColorGradient.Evaluate(ratio);
        }

        if (tempText != null)
        {
            tempText.text = $"{Mathf.RoundToInt(currentTemp)}°C";
        }
    }

    public void StartDelivery() => isDelivering = true;
    public void StopDelivery() => isDelivering = false;

    private void OnFoodCold()
    {
        Debug.Log($"{itemName}이(가) 완전히 식었습니다!");
    }
}