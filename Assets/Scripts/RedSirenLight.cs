using UnityEngine;

public class RedSirenLight : MonoBehaviour
{
    [SerializeField]private Light targetLight;
    
    [Header("조명 설정")]
    public float minIntensity = 0f;
    public float maxIntensity = 8f;
    public float speed = 6f; // 깜빡이는 속도

    void Start()
    {
        targetLight = GetComponent<Light>();
        targetLight.color = Color.red; // 라이트 색상을 빨간색으로 지정
    }

    void Update()
    {
        // Sin 파형을 이용해 Intensity를 부드럽게 왕복
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
