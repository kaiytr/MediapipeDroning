using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private CharacterController controller;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f * 2f;
    public float jumpHeight = 1.5f;

    [Header("Mouse Look Settings")]
    public Transform playerCamera; // 카메라(플레이어의 자식 오브젝트) 연결
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f; // 위아래 최대 시야 각도

    private float cameraPitch = 0f;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // 게임 시작 시 마우스 커서 숨기기 및 화면 중앙 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. 마우스 시점 회전 (1인칭 뷰)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 좌우 회전 (캐릭터 몸체 전체 회전)
        transform.Rotate(Vector3.up * mouseX);

        // 상하 회전 (카메라만 고개 숙이기/들기)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        // 2. 바닥 체크
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 바닥에 밀착 유지
        }

        // 3. 키보드 이동 입력 (WASD)
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDir = transform.right * moveX + transform.forward * moveZ;
        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        // 4. 점프 처리
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. 중력 적용
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}