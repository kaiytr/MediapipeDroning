using System.Collections;
using UnityEngine;

public class VictimInteraction : MonoBehaviour
{
    [Header("신호탄 설정")]
    [SerializeField] private GameObject flarePrefab;     // 발사할 플레어 프리팹
    [SerializeField] private Transform flareSpawnPoint;  // 발사 위치
    [SerializeField] private float checkInterval = 0.5f; // 타임 체크 주기 (초)

    private bool isRescued = false;
    private bool hasFiredFlare = false;                  // 단 1회 발사 확인용 플래그
    private Coroutine flareCoroutine;

    private void Start()
    {
        if (flareSpawnPoint == null)
        {
            flareSpawnPoint = transform;
        }

        flareCoroutine = StartCoroutine(SpawnFlareRoutine());
    }

    private IEnumerator SpawnFlareRoutine()
    {
        while (!isRescued && !hasFiredFlare)
        {
            if (RescueManager.Instance != null && RescueManager.Instance.currentRescueData != null)
            {
                float totalTime = RescueManager.Instance.currentRescueData.goldenTime;
                float remainingTime = RescueManager.Instance.remainingGoldenTime;

                // 남은 골든 타임이 절반(50%) 이하일 때 단 1회 발사
                if (remainingTime <= (totalTime / 2f) && flarePrefab != null)
                {
                    Instantiate(flarePrefab, flareSpawnPoint.position, flareSpawnPoint.rotation);
                    hasFiredFlare = true;
                    yield break; // 발사 완료 후 코루틴 즉시 종료
                }
            }

            // 조건 도달 여부를 주기적으로 확인 (0.5초 추천)
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool success = RescueManager.Instance.TryCompleteRescue();

            if (success)
            {
                Debug.Log("성공");
                
                isRescued = true;
                if (flareCoroutine != null)
                {
                    StopCoroutine(flareCoroutine);
                }
            }
            else
            {
                Debug.Log("실패");
            }
        }
    }
}