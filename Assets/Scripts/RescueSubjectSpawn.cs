using UnityEngine;

public class RescuerVectorSpawner : MonoBehaviour
{
    [Header("스폰 좌표 목록 (X, Y, Z)")]
    public Vector3[] spawnPoints;

    private int currentIndex = -1;

    void Start()
    {
        MoveToRandomPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MoveToRandomPosition();
        }
    }

    public void MoveToRandomPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("스폰 좌표(Spawn Points)가 설정되지 않았습니다.");
            return;
        }

        int randomIndex;

        // 이전 위치와 중복되지 않게 무작위 좌표 선택
        do
        {
            randomIndex = Random.Range(0, spawnPoints.Length);
        } 
        while (spawnPoints.Length > 1 && randomIndex == currentIndex);

        currentIndex = randomIndex;
        
        // Vector3 값으로 위치 직접 설정
        transform.position = spawnPoints[currentIndex];
    }
}
