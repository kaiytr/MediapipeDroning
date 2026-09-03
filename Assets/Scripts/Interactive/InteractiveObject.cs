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
        Debug.Log("★ 상호작용 성공: " + target.name);
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