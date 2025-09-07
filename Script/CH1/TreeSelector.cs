using UnityEngine;
using System.Collections.Generic;

public class TreeSelector : MonoBehaviour
{
    private PaperTree currentSelectedTree;
    private List<PaperTree> cutTrees = new List<PaperTree>(); // 잘린 나무 추적
    private HashSet<PaperTree> alreadySelectedTrees = new HashSet<PaperTree>(); // 이미 선택된 나무 집합

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
            PaperTree clickedTree = hit.collider.GetComponentInParent<PaperTree>();

            if (clickedTree == null)
            {
                clickedTree = FindTreeFromCutParts(hit.collider.gameObject);
            }

            if (clickedTree != null)
            {
                SelectTree(clickedTree);
            }
        }
    }

    PaperTree FindTreeFromCutParts(GameObject clickedObject)
    {
        foreach (PaperTree tree in cutTrees)
        {
            if (tree != null && tree.IsCut)
            {
                if (IsPartOfTree(clickedObject, tree))
                {
                    Debug.Log($"잘린 파츠에서 나무 발견: {tree.name}");
                    return tree;
                }
            }
        }
        return null;
    }

    bool IsPartOfTree(GameObject clickedObject, PaperTree tree)
    {
        if (tree == null) return false;
        foreach (GameObject part in tree.parts)
        {
            if (part != null && (part == clickedObject || IsChildOf(clickedObject, part)))
            {
                return true;
            }
        }
        return false;
    }

    bool IsChildOf(GameObject child, GameObject parent)
    {
        Transform current = child.transform;
        while (current != null)
        {
            if (current.gameObject == parent)
                return true;
            current = current.parent;
        }
        return false;
    }

    void SelectTree(PaperTree tree)
    {
        if (alreadySelectedTrees.Contains(tree))
        {
            Debug.Log($"이미 한 번 선택된 나무라서 다시 선택 불가: {tree.name}");
            return;
        }

        if (currentSelectedTree == tree)
        {
            Debug.Log($"이미 현재 선택된 나무 - 선택 유지: {tree.name}");
            return;
        }

        if (currentSelectedTree != null)
        {
            currentSelectedTree.SetSelected(false);

            alreadySelectedTrees.Add(currentSelectedTree);
            Debug.Log($"이미 선택된 나무 집합에 추가: {currentSelectedTree.name}");
        }

        currentSelectedTree = tree;
        currentSelectedTree.SetSelected(true);

        if (tree.IsCut && !cutTrees.Contains(tree))
        {
            cutTrees.Add(tree);
            Debug.Log($"잘린 나무 리스트에 추가: {tree.name}");
        }

        Debug.Log($"나무 선택: {tree.name}");
    }

    public PaperTree GetCurrentSelectedTree()
    {
        return currentSelectedTree;
    }

    public void DeselectCurrentTree()
    {
        if (currentSelectedTree != null)
        {
            currentSelectedTree.SetSelected(false);

            alreadySelectedTrees.Add(currentSelectedTree);
            Debug.Log($"이미 선택된 나무 집합에 추가(해제시): {currentSelectedTree.name}");

            currentSelectedTree = null;
            Debug.Log("나무 선택 해제");
        }
    }

    public void RegisterCutTree(PaperTree tree)
    {
        if (tree != null && tree.IsCut && !cutTrees.Contains(tree))
        {
            cutTrees.Add(tree);
            Debug.Log($"수동으로 잘린 나무 등록: {tree.name}");
        }
    }
}
