using UnityEngine;

public class MemoOpener : MonoBehaviour
{
    [Header("Memo UI Prefab")]
    public GameObject memoUIPrefab;   // 에디터에서 MemoUI 프리팹 연결

    private GameObject currentMemoUI; // 현재 열려있는 UI 인스턴스 저장용

    public void OpenMemoUI()
    {
        if (currentMemoUI != null)
        {
            // 이미 열려있으면 닫기
            CloseMemoUI();
            return;
        }

        if (memoUIPrefab == null)
        {
            Debug.LogWarning("MemoUI 프리팹이 할당되지 않았습니다.");
            return;
        }

        // UI 생성 및 캔버스(혹은 부모) 밑에 붙이기
        currentMemoUI = Instantiate(memoUIPrefab, FindObjectOfType<Canvas>().transform);
        currentMemoUI.transform.localPosition = Vector3.zero;
        currentMemoUI.transform.localScale = Vector3.one;
    }

    public void CloseMemoUI()
    {
        if (currentMemoUI != null)
        {
            Destroy(currentMemoUI);
            currentMemoUI = null;
        }
    }
}
