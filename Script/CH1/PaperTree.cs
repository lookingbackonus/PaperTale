using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaperTree : MonoBehaviour
{
    #region 상수 정의
    private const float CAMERA_DISTANCE = 5f;
    private const float LINE_WIDTH = 0.05f;
    private const float PHYSICS_DELAY = 0.5f;
    private const float VIBRATION_TIME = 0.5f;
    private const float VIBRATION_STRENGTH = 0.1f;
    private const float GENTLE_FORCE = 2f;
    private const float SEPARATION_DISTANCE = 0.5f;
    private const float SEPARATION_DELAY = 0.1f;
    #endregion

    #region 인스펙터 노출 필드
    [Header("나무 조각")]
    public List<GameObject> parts = new List<GameObject>();

    [Header("자르기 설정")]
    public List<BoxCollider> sliceLineColliders = new List<BoxCollider>();
    public Texture2D scissorsCursor;

    [Header("물리 설정")]
    [SerializeField] private float clickDetectionRadius = 1.5f;

    [Header("분리 설정")]
    [SerializeField] private float separationForce = 3f;
    [SerializeField] private float temporaryColliderDelay = 1f;
    #endregion

    #region 퍼블릭 상태 변수
    private bool isCut = false;
    private bool isPlaced = false;
    #endregion

    #region 프라이빗 변수
    private Vector3 startCutPosition;
    private Vector3 endCutPosition;
    private bool isDragging = false;
    private GameObject selectedPart;
    private Vector3 dragOffset;
    private LineRenderer lineRenderer;
    private bool sliceLine0Activated = false;
    private bool sliceLine1Activated = false;
    private bool hasKnife = false;
    #endregion

    #region 선택 상태 변수
    private bool isSelected = false;
    public bool IsSelected => isSelected;
    #endregion

    #region 프로퍼티
    public bool IsCut => isCut;
    public bool IsPlaced => isPlaced;
    public bool HasKnife => hasKnife;
    public List<GameObject> Parts => parts;
    #endregion

    #region 선택/해제 메서드
    public void SetSelected(bool value)
    {
        isSelected = value;

        // 선택 해제 시 커서 초기화
        if (!isSelected)
        {
            ResetCursor();
        }
        // 선택 시에는 상태에 따라 커서 설정
        else if (isSelected && !isCut && hasKnife && CanCut())
        {
            SetCuttingCursor();
        }
    }
    #endregion

    #region Unity 라이프사이클
    void Start()
    {
        InitializeComponents();
        CheckInventoryForKnife();
    }

    void Update()
    {
        // 클릭(선택)된 나무만 상호작용 허용
        if (!isSelected) return;
        HandlePuzzleInteraction();
    }
    #endregion

    #region 초기화
    void InitializeComponents()
    {
        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = LINE_WIDTH;
        lineRenderer.endWidth = LINE_WIDTH;
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.white;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.sortingOrder = 10;
    }

    void CheckInventoryForKnife()
    {
        hasKnife = true;
    }
    #endregion

    #region 메인 상호작용 로직
    void HandlePuzzleInteraction()
    {
        if (!isCut && hasKnife)
        {
            HandleCuttingMode();
        }
        // else if (isCut && !isPlaced)
        // {
        //     HandleMovingMode(); // [드래그 이동 기능 주석처리]
        // }
        else if (isSelected) // 선택되었지만 조건에 맞지 않으면 커서 리셋
        {
            ResetCursor();
        }
    }

    void HandleCuttingMode()
    {
        if (CanCut())
        {
            // 선택된 상태일 때만 커서 설정
            if (isSelected)
            {
                SetCuttingCursor();
            }
            HandleCuttingInput();
        }

        if (IsCuttingComplete())
        {
            CompleteCutting();
        }
    }

    /*
    // [드래그 이동 기능 주석처리]
    void HandleMovingMode()
    {
        ResetCursor();
        HandleMovingInput();
    }
    */
    #endregion

    #region 자르기 로직
    bool CanCut()
    {
        return !sliceLine0Activated || !sliceLine1Activated;
    }

    bool IsCuttingComplete()
    {
        return sliceLine0Activated && sliceLine1Activated;
    }

    void SetCuttingCursor()
    {
        Cursor.SetCursor(scissorsCursor, Vector2.zero, CursorMode.Auto);
    }

    void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void HandleCuttingInput()
    {
        if (Input.GetMouseButtonDown(0))
            StartCutting();

        if (isDragging)
            UpdateCutting();

        if (Input.GetMouseButtonUp(0))
            StopCutting();
    }

    void StartCutting()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        startCutPosition = mousePos;
        isDragging = true;
        SetupCuttingLine();
    }

    void UpdateCutting()
    {
        endCutPosition = GetMouseWorldPosition();
        lineRenderer.SetPosition(1, endCutPosition);
        CheckSliceCollision();
    }

    void StopCutting()
    {
        isDragging = false;
        lineRenderer.positionCount = 0;
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = CAMERA_DISTANCE;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    void SetupCuttingLine()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startCutPosition);
        lineRenderer.SetPosition(1, startCutPosition);
    }

    void CheckSliceCollision()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            BoxCollider boxCollider = hit.collider as BoxCollider;
            if (boxCollider != null && sliceLineColliders.Contains(boxCollider))
            {
                ProcessSliceHit(boxCollider);
            }
        }
    }

    void ProcessSliceHit(BoxCollider boxCollider)
    {
        int index = sliceLineColliders.IndexOf(boxCollider);

        if (index == 0 && !sliceLine0Activated)
        {
            Debug.Log("첫 번째 슬라이스 라인 활성화");
            Cut(0);
            sliceLine0Activated = true;

            RemoveSliceLine(0);
        }
        else if (index == 1 && sliceLine0Activated && !sliceLine1Activated)
        {
            Debug.Log("두 번째 슬라이스 라인 활성화");
            Cut(1);
            sliceLine1Activated = true;
        }
    }

    void CompleteCutting()
    {
        isCut = true;
        ResetCursor();
        ClearSliceLines();
        StartCoroutine(PlayCutAnimation());
        StartCoroutine(DelayedPuzzle2Start());
        LogCuttingComplete();
    }

    void ClearSliceLines()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        Debug.Log("슬라이스 라인 완전 정리 완료");
    }

    void Cut(int partIndex)
    {
        if (!hasKnife && partIndex >= 0)
        {
            Debug.Log("칼 아이템이 필요합니다!");
            return;
        }

        if (partIndex < 0)
        {
            partIndex = 0;
        }

        if (IsValidPartIndex(partIndex))
        {
            GameObject part = parts[partIndex];
            part.transform.SetParent(null);

            if (partIndex != 2)
            {
                ApplyBasicPhysics(part);
                Debug.Log($"Cut() 호출: {part.name} 자르기 완료 (물리 적용)");
            }
            else
            {
                Debug.Log($"Cut() 호출: {part.name} 자르기 완료 (2번 파츠는 퍼즐 1에서 고정)");
            }
        }
    }

    IEnumerator DelayedPuzzle2Start()
    {
        yield return new WaitForSeconds(1f);
        StartPuzzle2();
    }

    void StartPuzzle2()
    {
        RemoveSliceLineColliders();

        if (lineRenderer != null)
        {
            Destroy(lineRenderer);
            lineRenderer = null;
        }

        if (IsValidPartIndex(2))
        {
            GameObject part = parts[2];
            part.transform.SetParent(null);

            ApplyBasicPhysics(part);

            Debug.Log($"퍼즐 2 시작: {part.name}에 기본 물리 적용 완료");
        }

        Debug.Log("퍼즐 2 시작 - 모든 슬라이스 라인 제거 완료");
    }

    void RemoveSliceLine(int index)
    {
        if (index >= 0 && index < sliceLineColliders.Count && sliceLineColliders[index] != null)
        {
            BoxCollider sliceCollider = sliceLineColliders[index];
            Debug.Log($"슬라이스 라인 {index}번 ({sliceCollider.name}) 제거");

            if (sliceCollider.gameObject.name.Contains("SliceLine"))
            {
                sliceCollider.gameObject.SetActive(false);
            }
            else
            {
                Destroy(sliceCollider);
            }

            sliceLineColliders[index] = null;
        }
    }

    void RemoveSliceLineColliders()
    {
        for (int i = 0; i < sliceLineColliders.Count; i++)
        {
            if (sliceLineColliders[i] != null)
            {
                BoxCollider sliceCollider = sliceLineColliders[i];
                Debug.Log($"슬라이스 라인 콜라이더 {sliceCollider.name} 제거");

                if (sliceCollider.gameObject.name.Contains("SliceLine"))
                {
                    sliceCollider.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(sliceCollider);
                }
            }
        }

        // 리스트 클리어
        sliceLineColliders.Clear();
        Debug.Log("모든 슬라이스 라인 콜라이더 제거 완료");
    }

    void LogCuttingComplete()
    {
        Debug.Log("퍼즐 1 완료: 나무 자르기 성공!");
        Debug.Log("퍼즐 2 시작: 나무 조각을 다리 위로 옮기세요.");
    }
    #endregion

    #region 이동 로직 (수정된 버전)
    /*
    // [드래그 이동 기능 주석처리]
    void HandleMovingInput()
    {
        if (Input.GetMouseButtonDown(0))
            StartDragging();

        if (isDragging)
            UpdateDragging();

        if (Input.GetMouseButtonUp(0))
            StopDragging();
    }

    // [드래그 이동 기능 주석처리]
    void StartDragging()
    {
        Debug.Log("=== StartDragging 호출됨 ===");
        Debug.Log($"isCut: {isCut}, isSelected: {isSelected}");

        if (!isCut)
        {
            Debug.Log("아직 자르지 않은 상태 - 드래그 불가");
            return;
        }
        StartCoroutine(DelayedStartDragging());
    }

    // [드래그 이동 기능 주석처리]
    IEnumerator DelayedStartDragging()
    {
        yield return new WaitForEndOfFrame(); // 한 프레임 대기

        GameObject clickedPart = GetClickedPart();
        if (clickedPart != null)
        {
            selectedPart = clickedPart;
            isDragging = true;

            SetupDragOffsetFixed();

            Rigidbody rb = selectedPart.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                Debug.Log($"드래그 시작: {clickedPart.name} 키네마틱 활성화");
            }
            else
            {
                Debug.LogError($"Rigidbody가 없음: {clickedPart.name}");
            }

            Debug.Log($"나무 조각 {clickedPart.name} 선택됨 (드래그 시작)");
        }
        else
        {
            Debug.Log("드래그 가능한 나무 조각을 찾을 수 없음");
        }
    }

    // [드래그 이동 기능 주석처리]
    void SetupDragOffsetFixed()
    {
        Vector3 partScreenPos = Camera.main.WorldToScreenPoint(selectedPart.transform.position);
        Vector3 mouseScreenPos = Input.mousePosition;

        mouseScreenPos.z = partScreenPos.z;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        dragOffset = selectedPart.transform.position - mouseWorldPos;

        Debug.Log($"파츠 위치: {selectedPart.transform.position}");
        Debug.Log($"마우스 월드 위치: {mouseWorldPos}");
        Debug.Log($"드래그 오프셋: {dragOffset}");
    }

    // [드래그 이동 기능 주석처리]
    void UpdateDragging()
    {
        if (selectedPart != null)
        {
            Vector3 partScreenPos = Camera.main.WorldToScreenPoint(selectedPart.transform.position);
            Vector3 mouseScreenPos = Input.mousePosition;

            mouseScreenPos.z = partScreenPos.z;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            Vector3 newPosition = mouseWorldPos + dragOffset;
            selectedPart.transform.position = newPosition;
        }
    }

    // [드래그 이동 기능 주석처리]
    void StopDragging()
    {
        if (selectedPart != null)
        {
            Rigidbody rb = selectedPart.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            Debug.Log($"나무 조각 {selectedPart.name} 드래그 종료");
            CheckBridgePlacement();
        }

        isDragging = false;
        selectedPart = null;
    }

    // [드래그 이동 기능 주석처리]
    GameObject GetClickedPart()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Debug.Log("=== 파츠 클릭 감지 시작 ===");

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GameObject clickedObject = hit.collider.gameObject;
            Debug.Log($"레이캐스트 히트: {clickedObject.name}");
            Debug.Log($"히트 위치: {hit.point}");
            Debug.Log($"콜라이더 타입: {hit.collider.GetType().Name}");

            foreach (GameObject part in parts)
            {
                if (part != null && (part == clickedObject || IsChildOf(clickedObject, part)))
                {
                    Debug.Log($"유효한 파츠 발견: {part.name}");
                    return part;
                }
            }

            Debug.Log($"{clickedObject.name}은 parts 리스트에 없음");
        }
        else
        {
            Debug.Log("레이캐스트 실패 - 거리 기반 검색으로 전환");
        }

        GameObject closestPart = GetPartByDistanceFixed();
        if (closestPart != null)
        {
            Debug.Log($"거리 기반 선택: {closestPart.name}");
        }
        else
        {
            Debug.Log("선택 가능한 파츠 없음");

            Debug.Log("=== 현재 Parts 상태 ===");
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null)
                {
                    Debug.Log($"Part {i}: {parts[i].name}, 활성화: {parts[i].activeInHierarchy}, 위치: {parts[i].transform.position}");

                    Collider[] cols = parts[i].GetComponents<Collider>();
                    Debug.Log($"  콜라이더 개수: {cols.Length}");
                    foreach (var col in cols)
                    {
                        Debug.Log($"    - {col.GetType().Name}, 활성화: {col.enabled}");
                    }
                }
            }
        }

        return closestPart;
    }

    // [드래그 이동 기능 주석처리]
    GameObject GetPartByDistanceFixed()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        GameObject closestPart = null;
        float closestDistance = float.MaxValue;

        Debug.Log($"거리 기반 검색 시작 - 마우스 월드 좌표: {mouseWorldPos}");

        foreach (GameObject part in parts)
        {
            if (part != null && part.activeInHierarchy)
            {
                float distance = Vector3.Distance(mouseWorldPos, part.transform.position);
                Debug.Log($"파츠 {part.name} 위치: {part.transform.position}, 거리: {distance}, 임계값: {clickDetectionRadius}");

                if (distance <= clickDetectionRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPart = part;
                    Debug.Log($"새로운 가장 가까운 파츠: {closestPart.name}, 거리: {closestDistance}");
                }
            }
        }

        return closestPart;
    }

    // [드래그 이동 기능 주석처리]
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
    */

    void CheckBridgePlacement()
    {
        // 다리 위치 확인 로직 구현
        Debug.Log("다리 배치 확인 중...");
    }
    #endregion

    #region 코루틴들
    IEnumerator PlayCutAnimation()
    {
        foreach (GameObject part in parts)
        {
            if (part != null)
            {
                TriggerCutAnimation(part);
            }
        }
        yield return null;
    }
    #endregion

    #region 물리 설정 유틸리티
    Rigidbody GetOrAddRigidbody(GameObject part)
    {
        Rigidbody rb = part.GetComponent<Rigidbody>();
        if (rb == null)
            rb = part.AddComponent<Rigidbody>();
        rb.mass = 3000f;
        return rb;
    }

    void TriggerCutAnimation(GameObject part)
    {
        Animator animator = part.GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("FoldAnimation");
    }

    void ApplyBasicPhysics(GameObject part)
    {
        Rigidbody rb = GetOrAddRigidbody(part);
        rb.mass = 3000f;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        EnsureColliderForDragging(part);

        Vector3 forceDirection = transform.TransformDirection(Vector3.right + Vector3.back * 0.5f);
        rb.AddForce(forceDirection * separationForce, ForceMode.Impulse);

        Debug.Log($"{part.name}에 기본 물리 적용 완료 (콜라이더 설정 포함, mass=3000)");
    }

    void EnsureColliderForDragging(GameObject part)
    {
        Collider[] colliders = part.GetComponents<Collider>();
        bool hasValidCollider = false;

        foreach (Collider col in colliders)
        {
            if (col != null && col.enabled)
            {
                if (col is MeshCollider)
                {
                    MeshCollider meshCol = col as MeshCollider;
                    if (!meshCol.convex)
                    {
                        Debug.Log($"{part.name}의 MeshCollider를 Convex로 설정");
                        meshCol.convex = true;
                    }
                }
                hasValidCollider = true;
                Debug.Log($"{part.name}에 유효한 콜라이더 발견: {col.GetType().Name}");
            }
        }

        if (!hasValidCollider)
        {
            BoxCollider boxCol = part.AddComponent<BoxCollider>();
            Debug.Log($"{part.name}에 BoxCollider 추가");
        }
    }
    #endregion

    #region 퍼블릭 메서드 (필수 유지)
    public void Cut()
    {
        Cut(-1);
    }

    public void Move(Vector3 destination)
    {
        if (!isCut)
        {
            Debug.Log("나무를 먼저 잘라야 합니다.");
            return;
        }

        if (selectedPart != null)
        {
            selectedPart.transform.position = destination;
            Debug.Log($"Move() 호출: {selectedPart.name}을(를) {destination}로 이동");
            CheckBridgePlacement();
        }
        else
        {
            Debug.Log("이동할 나무 조각을 먼저 선택하세요!");
        }
    }
    #endregion

    #region 유틸리티 메서드
    bool IsValidPartIndex(int index)
    {
        return index < parts.Count && parts[index] != null;
    }
    #endregion

    #region 기즈모
    void OnDrawGizmos()
    {
        DrawCuttingGizmos();
        DrawDraggingGizmos();
        DrawClickDetectionGizmos();
        DrawStatusGizmos();
    }

    void DrawCuttingGizmos()
    {
        if (isDragging && !isCut)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startCutPosition, endCutPosition);
        }
    }

    void DrawDraggingGizmos()
    {
        if (selectedPart != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(selectedPart.transform.position, selectedPart.transform.localScale);
        }
    }

    void DrawClickDetectionGizmos()
    {
        if (parts.Count > 2 && parts[2] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(parts[2].transform.position, clickDetectionRadius);
        }
    }

    void DrawStatusGizmos()
    {
        if (isCut)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }
    #endregion
}
