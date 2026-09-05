using UnityEngine;

public class VictimInteraction : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 요구조자 위치 도착시 구조 시도
            bool success = RescueManager.Instance.TryCompleteRescue();

            if (success)
            {
                Debug.Log("성공");
                // 성공 처리 (애니메이션, 연출 등)
            }
            else
            {
                Debug.Log("실패");
                // 실패 메시지 (아이템 부족 알림 등)
            }
        }
    }
}