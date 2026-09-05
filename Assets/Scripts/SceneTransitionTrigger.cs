using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("이동할 씬 설정")]
    [Tooltip("이동하고자 하는 씬의 정확한 이름을 입력하세요.")]
    public string targetSceneName = "SceneB";

    [Header("태그 설정")]
    [Tooltip("씬 전환을 감지할 플레이어의 태그")]
    public string playerTag = "Player";

    private bool isTransitioning = false; // 중복 씬 로딩 방지 플래그

    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트의 태그가 Player인지 확인
        if (other.CompareTag(playerTag) && !isTransitioning)
        {
            isTransitioning = true;
            Debug.Log($"[SceneTransition] {targetSceneName} 씬으로 이동합니다.");
            
            // 지정한 씬으로 이동
            SceneManager.LoadScene(targetSceneName);
        }
    }
}