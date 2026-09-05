using System.Collections;
using UnityEngine;

public class VictimInteraction : MonoBehaviour
{
    [Header("신호탄 설정")]
    [SerializeField] private GameObject flarePrefab;     // 발사할 플레어 프리팹
    [SerializeField] private Transform flareSpawnPoint;  // 발사 위치
    [SerializeField] private float checkInterval = 0.5f; // 타임 체크 주기 (초)

    [Header("씬 이동 설정")]
    [SerializeField] private string nextSceneName = "ClearScene"; // 성공 시 이동할 씬 이름
    [SerializeField] private float fadeDuration = 1.0f;          // 페이드 연출 시간 (초)

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

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isRescued)
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

                // Fade 효과 적용 후 이동할 씬으로 전환
                if (FadeManager.Instance != null)
                {
                    FadeManager.Instance.LoadSceneWithFade(nextSceneName, fadeDuration);
                }
                else
                {
                    Debug.LogWarning("FadeManager가 씬에 존재하지 않습니다.");
                }
            }
            else
            {
                Debug.Log("실패");
            }
        }
    }
}