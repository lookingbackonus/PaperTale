using UnityEngine;

public class MultiActivator : MonoBehaviour
{
    [Header("한 번에 활성화할 오브젝트들")]
    [SerializeField] private GameObject[] objectsToActivate;

    /// <summary>
    /// 배열에 들어 있는 모든 오브젝트를 활성화합니다.
    /// </summary>
    public void ActivateAll()
    {
        for (int i = 0; i < objectsToActivate.Length; i++)
        {
            if (objectsToActivate[i] != null)
                objectsToActivate[i].SetActive(true);
        }
    }

    /// <summary>
    /// 배열에 들어 있는 모든 오브젝트를 비활성화합니다.
    /// </summary>
    public void DeactivateAll()
    {
        for (int i = 0; i < objectsToActivate.Length; i++)
        {
            if (objectsToActivate[i] != null)
                objectsToActivate[i].SetActive(false);
        }
    }

    // 예시: 스페이스바를 누르면 동시에 활성화
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateAll();
        }
    }
}
