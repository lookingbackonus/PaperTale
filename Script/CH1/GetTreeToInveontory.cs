using UnityEngine;

public class GetTreeToInveontory : MonoBehaviour
{
    private string TAG = "[GetTreeToInveontory]";

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectTreeFromMouse();
        }
    }

    void SelectTreeFromMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Debug.Log($"{TAG} SelectTreeFromMouse : " + hit.collider.gameObject.name);
            GameObject go = hit.collider.gameObject;
            if (go.name.Contains("Wood"))
            {
                // Debug.Log($"{TAG} SelectTreeFromMouse : Wood");
                if (go.transform.parent == null)
                {
                    // Debug.Log($"{TAG} SelectTreeFromMouse : 부모없음");
                    UIManager.Instance.AddItemOnclicked((int)ItemNum.WOOD);
                    Destroy(go);
                }
            }
            else // Debug.Log($"{TAG} SelectTreeFromMouse : not wood");
            {
            }

        }
    }
}
