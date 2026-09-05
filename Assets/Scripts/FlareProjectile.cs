using UnityEngine;

public class FlareProjectile : MonoBehaviour
{
    [Header("이동 및 수명 설정")]
    [SerializeField] private float speed = 15f;      // 하늘로 날아가는 속도
    [SerializeField] private float lifeTime = 3f;    // 유지 시간 후 자동 파괴 (초)

    private void Start()
    {
        // 지정한 시간(lifeTime)이 지나면 플레어 오브젝트 자동 삭제
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 매 프레임마다 위쪽(하늘) 방향으로 이동
        transform.Translate(Vector3.up * speed * Time.deltaTime, Space.World);
    }
}
