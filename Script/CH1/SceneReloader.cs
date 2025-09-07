using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReloader : MonoBehaviour
{
    [Header("플레이어의 태그(예: Player)")]
    public string playerTag = "Player";

    private bool isReloading = false; // 중복 방지

    private void OnTriggerEnter(Collider other)
    {
        if (!isReloading && other.CompareTag(playerTag))
        {
            isReloading = true;
            // FadeOut이 끝난 뒤 씬을 로드
            FadeManager.Instance.FadeOut(() =>
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }
    }

    // 씬 로드 후 페이드 인
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FadeManager.Instance.FadeIn();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        isReloading = false; // 필요시 리셋
    }
}
