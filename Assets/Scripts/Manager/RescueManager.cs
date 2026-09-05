using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RescueManager : MonoBehaviour
{
    public static RescueManager Instance { get; private set; }

    [Header("씬 설정")]
    [Tooltip("구조 요청을 받고 아이템을 준비하는 A 씬 이름")]
    public string sceneAName = "SceneA"; 
    [Tooltip("요구조자가 있고 힌트가 출력되는 B 씬 이름")]
    public string sceneBName = "SceneB";

    [Header("현재 구조 상태")]
    public RescueData currentRescueData;
    public List<ItemData> playerInventory = new List<ItemData>();
    public float remainingGoldenTime;
    public bool isRescueActive = false;

    [Header("요구조자 프리팹 (Scene B용)")]
    public GameObject victimPrefab;

    // UI 및 게임 상태 알림 이벤트
    public event Action<float> OnTimerUpdated;          // 남은 시간 알림
    public event Action<string> OnHintUpdated;           // 새 힌트 알림
    public event Action OnRescueSuccess;                 // 성공 알림
    public event Action OnRescueFailed;                  // 실패 알림

    private int currentHintIndex = 0;
    private float hintTimer = 0f;
    private const float HINT_INTERVAL = 20f;             // 힌트 출력 간격 (초)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
        StartRescueRequest(currentRescueData);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!isRescueActive) return;

        // 1. 골든 타임 줄어들기
        remainingGoldenTime -= Time.deltaTime;
        OnTimerUpdated?.Invoke(remainingGoldenTime);

        if (remainingGoldenTime <= 0f)
        {
            remainingGoldenTime = 0f;
            FailRescue();
            return;
        }

        // 2. 인스펙터에서 지정한 B 씬일 때 20초마다 힌트 UI 갱신
        if (SceneManager.GetActiveScene().name == sceneBName)
        {
            UpdateHintTimer();
        }
    }

    // [A 씬] 특정 함수 실행 시 구조 요청 시작
    public void StartRescueRequest(RescueData data)
    {
        currentRescueData = data;
        if (data != null)
        {
            remainingGoldenTime = data.goldenTime;
        }
        playerInventory.Clear();
        currentHintIndex = 0;
        hintTimer = 0f;
        isRescueActive = true;
    }

    // [A 씬] 플레이어가 아이템을 챙길 때 호출
    public void AddItem(ItemData item)
    {
        if (!playerInventory.Contains(item))
        {
            playerInventory.Add(item);
        }
    }

    // 씬 이동 시 자동으로 호출되는 이벤트
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isRescueActive) return;

        // 인스펙터에서 지정한 B 씬에 들어왔을 때 요구조자 생성 및 첫 힌트 출력
        if (scene.name == sceneBName)
        {
            SpawnVictim();
            ShowNextHint(); 
        }
    }

    private void UpdateHintTimer()
    {
        if (currentRescueData == null || currentRescueData.locationHints.Count == 0) return;
        if (currentHintIndex >= currentRescueData.locationHints.Count) return;

        hintTimer += Time.deltaTime;
        if (hintTimer >= HINT_INTERVAL)
        {
            hintTimer = 0f;
            ShowNextHint();
        }
    }

    private void ShowNextHint()
    {
        if (currentHintIndex < currentRescueData.locationHints.Count)
        {
            string hint = currentRescueData.locationHints[currentHintIndex];
            OnHintUpdated?.Invoke(hint);
            currentHintIndex++;
        }
    }

    private void SpawnVictim()
    {
        if (victimPrefab != null && currentRescueData != null)
        {
            Instantiate(victimPrefab, currentRescueData.rescuerPosition, Quaternion.identity);
        }
    }

    // [B 씬] 플레이어가 요구조자와 상호작용할 때 호출
    public bool TryCompleteRescue()
    {
        if (!isRescueActive) return false;

        // 아이템 일치 여부 검증
        foreach (var requiredItem in currentRescueData.requiredItems)
        {
            if (!playerInventory.Contains(requiredItem))
            {
                return false; // 필요 아이템 부족
            }
        }

        SucceedRescue();
        return true;
    }

    private void FailRescue()
    {
        isRescueActive = false;
        OnRescueFailed?.Invoke();

        // 골든 타임 종료 시 sceneAName(A 씬)으로 전환
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.LoadSceneWithFade(sceneAName);
        }
        else
        {
            SceneManager.LoadScene(sceneAName);
        }
    }

    private void SucceedRescue()
    {
        isRescueActive = false;
        OnRescueSuccess?.Invoke();
    }
}