using UnityEngine;

public class FirstPersonInteraction : MonoBehaviour
{
    public Transform playerCamera; // 1인칭 메인 카메라
    public float interactDistance = 4f; // 상호작용 가능한 거리
    public string targetTag = "Interactable";

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        if (playerCamera == null) return;

        // 카메라 정중앙 시선 방향으로 레이를 쏨
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // 바라보는 오브젝트의 태그 확인
            if (hit.collider.CompareTag(targetTag))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Interact(hit.collider.gameObject);
                }
            }
        }
    }

    void Interact(GameObject target)
    {
        // 1. 아이템 습득 처리 (ItemObject가 붙어있는 경우)
        ItemObject itemObj = target.GetComponent<ItemObject>();
        if (itemObj != null && itemObj.itemData != null)
        {
            if (RescueManager.Instance != null)
            {
                RescueManager.Instance.AddItem(itemObj.itemData);
            }

            Destroy(target);
            return;
        }

        // 2. 요구조자 상호작용 처리 (Scene B일 경우)
        if (RescueManager.Instance != null && RescueManager.Instance.isRescueActive)
        {
            // B씬 요구조자와 상호작용 시 구조 시도
            if (target.name.Contains("Victim") || target.GetComponent<Collider>() != null)
            {
                bool isSuccess = RescueManager.Instance.TryCompleteRescue();
                if (isSuccess)
                {
                    Debug.Log("구조 성공!");
                }
                else
                {
                    Debug.Log("필요한 아이템이 부족합니다!");
                }
            }
        }
    }

    // 에디터 씬 뷰에서 카메라 레이 가이드라인 표시
    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(playerCamera.position, playerCamera.forward * interactDistance);
        }
    }
}