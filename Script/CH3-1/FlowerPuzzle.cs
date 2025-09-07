using UnityEngine;
using System.Collections.Generic;

[System.Serializable]

public class FlowerTypeSettings
{
    [Header("꽃 타입 설정")]
    public Flower.FlowerType flowerType;
    public Flower.RiskLevel riskLevel;
    public AudioClip pickSound;

    [Header("이 타입의 꽃들")]
    public List<GameObject> flowerObjects = new List<GameObject>();
}

public class FlowerPuzzle : MonoBehaviour
{   
    [Header("꽃 타입별 설정")]
    public FlowerTypeSettings[] flowerTypeSettings;

    [Header("경고 효과")]
    [SerializeField] private WarningEffect warningEffect;
    
    [Header("게임 설정")]
    private int pickedCount = 0;
    private const int maxPickCount = 4; // 4송이 이상이면 모든 꽃이 시들게

    [Header("레이어 설정")]
    [SerializeField] private LayerMask flowerLayerMask = -1; // 인스펙터에서 설정 가능

    [Header("커서 설정")]
    [SerializeField] private Texture2D hoverCursor; // 꽃 위에 마우스를 올렸을 때의 커서
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero; // 커서의 핫스팟 (클릭 지점)

    [Header("UI Controller")]
    [SerializeField] private FlowerPuzzleController flowerPuzzleController;

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<GameObject, FlowerTypeSettings> flowerSettingsDict = new Dictionary<GameObject, FlowerTypeSettings>();
    private Dictionary<GameObject, bool> pickedFlowers = new Dictionary<GameObject, bool>();

    // 호버 관련 변수들
    private GameObject currentHoveredFlower = null;
    private bool isHoveringFlower = false;

    void Start()
    {
        InitializeFlowerDictionary();
        ValidateAudioSettings();
        InitializeWarningEffect();
    }

    void Update()
    {
        // 마우스 호버 체크
        CheckFlowerHover();

        // 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            CheckFlowerClick();
        }
    }

    // WarningEffect 초기화
    void InitializeWarningEffect()
    {
        if (warningEffect == null)
        {
            warningEffect = FindObjectOfType<WarningEffect>();
            if (warningEffect == null)
            {
                Debug.LogWarning("WarningEffect를 찾을 수 없습니다. Caution 레벨 꽃의 경고 효과가 작동하지 않습니다.");
            }
            else
            {
                Debug.Log("WarningEffect가 자동으로 연결되었습니다.");
            }
        }
    }

    // 오디오 설정 검증
    void ValidateAudioSettings()
    {
        // Audio Listener 확인
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener == null)
        {
            Debug.LogError("씬에 AudioListener가 없습니다! Main Camera에 추가하세요.");
        }

        // 각 꽃 타입별 오디오 클립 확인
        for (int i = 0; i < flowerTypeSettings.Length; i++)
        {
            FlowerTypeSettings setting = flowerTypeSettings[i];
            if (setting.pickSound == null && setting.flowerType != Flower.FlowerType.Black)
            {
                Debug.LogWarning($"{setting.flowerType} 타입의 pickSound가 설정되지 않았습니다!");
            }
        }
    }

    // 딕셔너리 초기화
    void InitializeFlowerDictionary()
    {
        flowerSettingsDict.Clear();
        pickedFlowers.Clear();

        foreach (FlowerTypeSettings setting in flowerTypeSettings)
        {
            foreach (GameObject flowerObj in setting.flowerObjects)
            {
                if (flowerObj != null)
                {
                    flowerSettingsDict[flowerObj] = setting;
                    pickedFlowers[flowerObj] = false;
                }
                else
                {
                    Debug.LogWarning($"{setting.flowerType} 타입에 null 오브젝트가 있습니다!");
                }
            }
        }
    }

    // 마우스 호버 체크 (커서 변경 기능 추가)
    void CheckFlowerHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        GameObject hoveredFlower = null;

        // Flower 레이어만 감지
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, flowerLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            // 클릭된 오브젝트가 꽃인지 확인
            if (flowerSettingsDict.ContainsKey(hitObject) && !pickedFlowers[hitObject])
            {
                hoveredFlower = hitObject;
            }
            else
            {
                // 부모나 자식에서 꽃을 찾아보기
                GameObject parentFlower = FindFlowerInHierarchy(hitObject);
                if (parentFlower != null && !pickedFlowers[parentFlower])
                {
                    hoveredFlower = parentFlower;
                }
            }
        }

        // 호버 상태 변경 처리 (커서 변경)
        if (hoveredFlower != currentHoveredFlower)
        {
            if (hoveredFlower != null)
            {
                // 꽃 위에 마우스가 올라왔을 때 - 커서 변경
                SetHoverCursor(true);
            }
            else if (currentHoveredFlower != null)
            {
                // 꽃에서 마우스가 벗어났을 때 - 기본 커서로 복귀
                SetHoverCursor(false);
            }

            currentHoveredFlower = hoveredFlower;
        }
    }

    // 커서 변경 메서드
    void SetHoverCursor(bool isHovering)
    {
        if (isHovering && hoverCursor != null)
        {
            // 호버 커서로 변경
            Cursor.SetCursor(hoverCursor, cursorHotspot, CursorMode.Auto);
            isHoveringFlower = true;
        }
        else
        {
            // 기본 커서로 복귀
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isHoveringFlower = false;
        }
    }

    // 테스트용 메서드 - 수동으로 커서 테스트
    [ContextMenu("커서 테스트 (3초간 호버 커서)")]
    public void TestCursor()
    {
        if (hoverCursor != null)
        {
            StartCoroutine(TestCursorCoroutine());
        }
        else
        {
            Debug.LogWarning("hoverCursor가 설정되지 않았습니다!");
        }
    }

    private System.Collections.IEnumerator TestCursorCoroutine()
    {
        SetHoverCursor(true);
        yield return new WaitForSeconds(3f);
        SetHoverCursor(false);
    }

    void CheckFlowerClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Flower 레이어만 감지하도록 수정
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, flowerLayerMask))
        {
            GameObject clickedObject = hit.collider.gameObject;

            // 클릭된 오브젝트가 꽃인지 확인
            if (flowerSettingsDict.ContainsKey(clickedObject))
            {
                PickFlower(clickedObject);
            }
            else
            {
                // 추가 디버깅: 부모나 자식에서 꽃을 찾아보기
                GameObject parentFlower = FindFlowerInHierarchy(clickedObject);
                if (parentFlower != null)
                {
                    PickFlower(parentFlower);
                }
            }
        }
    }

    // 계층구조에서 꽃 오브젝트 찾기
    GameObject FindFlowerInHierarchy(GameObject obj)
    {
        // 부모 방향으로 탐색
        Transform current = obj.transform;
        while (current != null)
        {
            if (flowerSettingsDict.ContainsKey(current.gameObject))
            {
                return current.gameObject;
            }
            current = current.parent;
        }

        // 자식 방향으로 탐색
        foreach (var kvp in flowerSettingsDict)
        {
            if (kvp.Key != null && kvp.Key.transform.IsChildOf(obj.transform))
            {
                return kvp.Key;
            }
        }

        return null;
    }

    public void PickFlower(GameObject flowerObject)
    {
        if (pickedFlowers[flowerObject])
        {
            return;
        }

        // 꽃 설정 가져오기
        FlowerTypeSettings setting = flowerSettingsDict[flowerObject];

        // 현재 호버된 꽃이라면 커서를 기본으로 변경
        if (currentHoveredFlower == flowerObject)
        {
            SetHoverCursor(false);
            currentHoveredFlower = null;
        }

        // UI 업데이트
        flowerPuzzleController.AddFlowerColor(setting.flowerType);

        // 꽃 꺾기
        pickedFlowers[flowerObject] = true;
        pickedCount++;

        // **모든 레벨에서 효과음을 먼저 재생**
        ShowPickEffect(flowerObject, setting);

        // Danger 레벨의 꽃을 꺾었을 때 즉시 모든 꽃이 시들게 함
        if (setting.riskLevel == Flower.RiskLevel.Danger)
        {
            Debug.Log($"{setting.flowerType} 꽃을 꺾었습니다. (위험도: {setting.riskLevel}) - 모든 꽃이 시듭니다!");
            ChangeFlowerAppearance(flowerObject, true);
            TriggerWitherAll();
            return;
        }
        // Caution 레벨의 꽃은 시들게 하고 경고 효과 발동
        else if (setting.riskLevel == Flower.RiskLevel.Caution)
        {
            Debug.Log($"{setting.flowerType} 꽃을 꺾어서 시들었습니다. (위험도: {setting.riskLevel}) - 경고 효과 발동!");
            ChangeFlowerAppearance(flowerObject, true);
            
            // *** 경고 효과 발동 ***
            TriggerWarningEffect($"위험한 {setting.flowerType} 꽃을 꺾었습니다!");
        }
        // Safe 레벨의 꽃은 삭제
        else if (setting.riskLevel == Flower.RiskLevel.Safe)
        {
            Debug.Log($"{setting.flowerType} 꽃을 꺾어서 삭제했습니다. (위험도: {setting.riskLevel})");
            Destroy(flowerObject);
        }

        // 4송이 이상 꺾었는지 확인
        if (pickedCount >= maxPickCount)
        {
            TriggerWitherAll();
        }
    }

    // 경고 효과 발동 메서드
    void TriggerWarningEffect(string reason)
    {
        if (warningEffect != null)
        {
            Debug.Log($"[꽃 퍼즐 경고] {reason}");
            warningEffect.TriggerFade();
        }
        else
        {
            Debug.LogWarning("WarningEffect가 설정되지 않아 경고 효과를 발동할 수 없습니다!");
        }
    }

    void ShowPickEffect(GameObject flowerObject, FlowerTypeSettings setting)
    {
        // 사운드 재생 (Black 꽃은 원래 소리가 나지 않음)
        if (setting.pickSound != null)
        {
            Vector3 soundPosition = flowerObject.transform.position;

            // AudioSource.PlayClipAtPoint 대신 더 안정적인 방법 사용
            GameObject tempAudioObj = new GameObject("TempAudio_" + setting.flowerType);
            tempAudioObj.transform.position = soundPosition;

            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            tempSource.clip = setting.pickSound;
            tempSource.spatialBlend = 0.5f; // 2D와 3D의 중간
            tempSource.volume = 0.8f;
            tempSource.Play();

            // 오디오 재생 완료 후 오브젝트 삭제
            Destroy(tempAudioObj, setting.pickSound.length + 0.1f);
        }
        else if (setting.flowerType != Flower.FlowerType.Black)
        {
            // Black 꽃이 아닌 경우에만 에러 로그 출력
            Debug.LogError($"{setting.flowerType} 꽃의 pickSound가 null입니다!");
        }
    }

    // 모든 꽃이 시드는 메서드
    private void TriggerWitherAll()
    {
        // 커서를 기본으로 복귀
        SetHoverCursor(false);
        currentHoveredFlower = null;

        // 씬에 있는 모든 꽃을 찾아서 시들게 함 (딕셔너리에 없는 꽃도 포함)
        Flower[] allFlowers = FindObjectsOfType<Flower>();

        foreach (Flower flower in allFlowers)
        {
            GameObject flowerObj = flower.gameObject;

            // 이미 삭제된 꽃은 스킵
            if (flowerObj == null) continue;

            // 아직 시들지 않은 꽃만 처리
            if (!pickedFlowers.ContainsKey(flowerObj) || !pickedFlowers[flowerObj])
            {
                // 딕셔너리에 없다면 추가
                if (!pickedFlowers.ContainsKey(flowerObj))
                {
                    pickedFlowers[flowerObj] = false;
                }

                pickedFlowers[flowerObj] = true;
                ChangeFlowerAppearance(flowerObj, true);
            }
        }

        // UI
        UIManager.Instance.ShowResultFlowerPuzzle(false);
    }

    // 꽃이 꺾인 후 외형 변화
    void ChangeFlowerAppearance(GameObject flowerObj, bool isPicked)
    {
        if (flowerObj == null) return;

        if (isPicked)
        {
            // 애니메이터 컴포넌트 찾기 (부모와 자식 모두 검색)
            Animator animator = flowerObj.GetComponent<Animator>();
            if (animator == null)
            {
                // 부모에 없으면 자식에서 찾기
                animator = flowerObj.GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                // idle에서 fail 애니메이션으로 전환 (트리거 사용)
                animator.SetTrigger("Fail");
            }
            else
            {
                Debug.LogWarning($"꽃 {flowerObj.name}과 그 자식들에 Animator 컴포넌트가 없습니다.");
            }
        }
    }

    // 스크립트가 비활성화될 때 커서를 기본으로 복귀
    void OnDisable()
    {
        SetHoverCursor(false);
    }

    // 애플리케이션이 포커스를 잃을 때도 커서를 기본으로 복귀
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SetHoverCursor(false);
        }
    }

    // 게임 상태 확인 메서드들
    public int GetPickedCount() => pickedCount;
    public int GetMaxPickCount() => maxPickCount;
    public bool CanPickMore() => pickedCount < maxPickCount;

    // 특정 타입의 꽃이 몇 개 꺾였는지 확인
    public int GetPickedCountByType(Flower.FlowerType flowerType)
    {
        int count = 0;
        FlowerTypeSettings setting = System.Array.Find(flowerTypeSettings, s => s.flowerType == flowerType);

        if (setting != null)
        {
            foreach (GameObject flowerObj in setting.flowerObjects)
            {
                if (pickedFlowers.ContainsKey(flowerObj) && pickedFlowers[flowerObj])
                {
                    count++;
                }
            }
        }

        return count;
    }

    // 인스펙터에서 호출할 수 있는 디버그 메서드
    [ContextMenu("오디오 설정 다시 검증")]
    public void RevalidateAudioSettings()
    {
        ValidateAudioSettings();
    }

    [ContextMenu("꽃 딕셔너리 다시 초기화")]
    public void ReinitializeFlowerDictionary()
    {
        InitializeFlowerDictionary();
    }

    [ContextMenu("경고 효과 테스트")]
    public void TestWarningEffect()
    {
        TriggerWarningEffect("테스트 경고 효과");
    }
}