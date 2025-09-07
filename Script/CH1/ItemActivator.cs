using UnityEngine;
using System.Collections.Generic;

public class ItemActivator : MonoBehaviour
{
    [Header("활성화할 오브젝트들(순서대로 등록하세요)")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    [Header("비활성화시킬 콜라이더 오브젝트")]
    public GameObject colliderToDisable;

    private int currentIndex = 0;
    private bool hasActivatedAny = false;

    // 외부에서 호출 (버튼에서 연결)
    public void ActivateNext()
    {
        if (currentIndex < objectsToActivate.Count)
        {
            GameObject go = objectsToActivate[currentIndex];
            if (go != null)
            {
                go.SetActive(true);
                Debug.Log($"활성화됨: {go.name}");

                if (!hasActivatedAny && colliderToDisable != null)
                {
                    colliderToDisable.SetActive(false);
                    Debug.Log("콜라이더 오브젝트가 비활성화됨!");
                    hasActivatedAny = true;
                }
            }
            currentIndex++;
        }
        else
        {
            Debug.Log("더 이상 활성화할 오브젝트가 없습니다.");
        }
    }
}