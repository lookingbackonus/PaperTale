using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MapPuzzleManager : MonoBehaviour
{
    [Header("16개 퍼즐 조각 프리팹을 순서대로 등록하세요!")]
    public List<GameObject> puzzlePiecePrefabs;

    [Header("UI 설정")]
    public RectTransform puzzleContainer;

    [Header("초기 배치 간격")]
    public float initialSpacingX = 430f;
    public float initialSpacingY = 280f;

    [Header("합칠 때 간격")]
    public float joinedSpacingX = 400f;
    public float joinedSpacingY = 250f;

    public int gridSize = 4;

    [HideInInspector]
    public List<UIPuzzlePiece> pieces = new List<UIPuzzlePiece>();

    UIPuzzlePiece selectedForSwap = null;
    bool isJoined = false;
    bool isInitialized = false; // 초기화 상태 확인용

    List<Vector2> initialPositions = new List<Vector2>();
    List<int> initialGridX = new List<int>();
    List<int> initialGridY = new List<int>();

    void Start()
    {
        // 타임라인 완료 후에만 시작하므로 CreateGrid() 호출 안함
        // CreateGrid();
    }

    public void StartPuzzle()
    {
        // 외부에서 호출하여 퍼즐을 시작하는 메서드
        if (!isInitialized)
        {
            CreateGrid();
            Debug.Log("MapPuzzleManager가 시작되었습니다!");
        }
    }

    public void CreateGrid()
    {
        if (isInitialized) return; // 이미 초기화됐으면 중복 실행 방지

        isInitialized = true;

        pieces.Clear();
        initialPositions.Clear();
        initialGridX.Clear();
        initialGridY.Clear();

        foreach (Transform child in puzzleContainer)
        {
            DestroyImmediate(child.gameObject);
        }

        List<int> shuffledIndices = new List<int>();
        for (int i = 0; i < puzzlePiecePrefabs.Count; i++)
        {
            shuffledIndices.Add(i);
        }

        for (int i = shuffledIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[randomIndex];
            shuffledIndices[randomIndex] = temp;
        }

        int gridIndex = 0;
        float centerOffsetX = (gridSize - 1) / 2.0f;
        float centerOffsetY = (gridSize - 1) / 2.0f;

        for (int y = 0; y < gridSize; y++)
        {
            int reversedY = gridSize - 1 - y;
            for (int x = 0; x < gridSize; x++)
            {
                if (gridIndex >= puzzlePiecePrefabs.Count)
                {
                    Debug.LogWarning("프리팹이 부족합니다. 16개 프리팹을 모두 리스트에 넣으세요!");
                    return;
                }

                Vector2 pos = new Vector2(
                    (x - centerOffsetX) * initialSpacingX,
                    (reversedY - centerOffsetY) * initialSpacingY
                );

                int prefabIndex = shuffledIndices[gridIndex];
                var obj = Instantiate(puzzlePiecePrefabs[prefabIndex], puzzleContainer);
                var rectTransform = obj.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError("프리팹에 RectTransform이 없습니다! UI 프리팹을 사용하세요.");
                    return;
                }

                rectTransform.anchoredPosition = pos;

                var piece = obj.GetComponent<UIPuzzlePiece>();
                if (piece == null)
                {
                    Debug.LogError("프리팹에 UIPuzzlePiece 컴포넌트가 없습니다!");
                    return;
                }

                piece.Init(this, x, y, prefabIndex);

                bool randomFlip = Random.Range(0, 2) == 1;
                piece.SetFlipped(randomFlip, true);

                pieces.Add(piece);
                initialPositions.Add(pos);
                initialGridX.Add(x);
                initialGridY.Add(y);
                gridIndex++;
            }
        }
        isJoined = false;
        Debug.Log("퍼즐 조각들이 랜덤하게 섞여서 배치되었습니다!");
    }

    void Update()
    {
        // 초기화되지 않았으면 키 입력 무시
        if (!isInitialized) return;

        // G, R키는 언제나 동작
        if (Input.GetKeyDown(KeyCode.G))
        {
            // 퍼즐이 완성되었는지 확인하고 씬 전환
            if (CheckPuzzleCompletion())
            {
                HandlePuzzleCompletionSceneTransition();
            }
            else
            {
                JoinAllPieces();
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllPieces();
        }
        // S키로 새로 섞기
        if (Input.GetKeyDown(KeyCode.S))
        {
            ReCreateGrid(); // CreateGrid 대신 ReCreateGrid 호출
        }
    }

    /// <summary>
    /// 퍼즐 완성 여부를 확인하는 메서드
    /// </summary>
    /// <returns>모든 조각이 올바른 위치에 있으면 true</returns>
    private bool CheckPuzzleCompletion()
    {
        if (!isInitialized || pieces.Count != 16) return false;

        // 모든 조각이 올바른 위치에 있는지 확인
        for (int i = 0; i < pieces.Count; i++)
        {
            UIPuzzlePiece piece = pieces[i];

            // 각 조각의 현재 그리드 위치에서 올바른 조각 번호 계산
            int expectedPieceNum = piece.gridY * gridSize + piece.gridX;

            // 현재 조각의 번호와 예상 번호가 일치하지 않으면 미완성
            if (piece.pieceNum != expectedPieceNum)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 퍼즐 완성 상태에 따른 씬 전환 처리
    /// </summary>
    private void HandlePuzzleCompletionSceneTransition()
    {
        // 먼저 퍼즐을 완성된 상태로 보여주기 위해 중앙에 합치기
        JoinAllPieces();

        // 현재 퍼즐이 앞면인지 뒷면인지 확인
        bool isShowingFront = CheckIfShowingFront();

        if (isShowingFront)
        {
            Debug.Log("앞면 퍼즐 완성! 2초 후 CH3-1으로 이동합니다.");
            // 2초 후 씬 전환 실행
            StartCoroutine(DelayedSceneTransition("CH3-1", 2f));
        }
        else
        {
            Debug.Log("뒷면 퍼즐 완성! 2초 후 CH3-2로 이동합니다.");
            // 2초 후 씬 전환 실행
            StartCoroutine(DelayedSceneTransition("CH3-2", 2f));
        }
    }

    /// <summary>
    /// 지연된 씬 전환을 위한 코루틴
    /// </summary>
    /// <param name="targetScene">이동할 씬 이름</param>
    /// <param name="delay">지연 시간 (초)</param>
    /// <returns></returns>
    private System.Collections.IEnumerator DelayedSceneTransition(string targetScene, float delay)
    {
        // 지정된 시간만큼 대기
        yield return new UnityEngine.WaitForSeconds(delay);

        // 페이드아웃 후 씬 전환
        FadeManager.Instance.FadeOut(() => {
            if (targetScene == "CH3-1")
            {
                CustomSceneManager.Instance.LoadCH3_1();
            }
            else if (targetScene == "CH3-2")
            {
                CustomSceneManager.Instance.LoadCH3_2();
            }
        });
    }

    /// <summary>
    /// 현재 퍼즐이 주로 앞면을 보여주고 있는지 확인
    /// </summary>
    /// <returns>앞면 조각이 더 많으면 true</returns>
    private bool CheckIfShowingFront()
    {
        int frontCount = 0;
        int backCount = 0;

        foreach (UIPuzzlePiece piece in pieces)
        {
            if (piece.IsShowingFront())
            {
                frontCount++;
            }
            else
            {
                backCount++;
            }
        }

        return frontCount >= backCount;
    }

    // 퍼즐을 다시 섞는 메서드 (초기화 후에만 사용)
    public void ReCreateGrid()
    {
        if (!isInitialized) return;

        // 기존 조각들 삭제
        pieces.Clear();
        initialPositions.Clear();
        initialGridX.Clear();
        initialGridY.Clear();

        foreach (Transform child in puzzleContainer)
        {
            DestroyImmediate(child.gameObject);
        }

        // 다시 생성 (CreateGrid의 메인 로직 재사용)
        List<int> shuffledIndices = new List<int>();
        for (int i = 0; i < puzzlePiecePrefabs.Count; i++)
        {
            shuffledIndices.Add(i);
        }

        for (int i = shuffledIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[randomIndex];
            shuffledIndices[randomIndex] = temp;
        }

        int gridIndex = 0;
        float centerOffsetX = (gridSize - 1) / 2.0f;
        float centerOffsetY = (gridSize - 1) / 2.0f;

        for (int y = 0; y < gridSize; y++)
        {
            int reversedY = gridSize - 1 - y;
            for (int x = 0; x < gridSize; x++)
            {
                if (gridIndex >= puzzlePiecePrefabs.Count)
                {
                    Debug.LogWarning("프리팹이 부족합니다. 16개 프리팹을 모두 리스트에 넣으세요!");
                    return;
                }

                Vector2 pos = new Vector2(
                    (x - centerOffsetX) * initialSpacingX,
                    (reversedY - centerOffsetY) * initialSpacingY
                );

                int prefabIndex = shuffledIndices[gridIndex];
                var obj = Instantiate(puzzlePiecePrefabs[prefabIndex], puzzleContainer);
                var rectTransform = obj.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError("프리팹에 RectTransform이 없습니다! UI 프리팹을 사용하세요.");
                    return;
                }

                rectTransform.anchoredPosition = pos;

                var piece = obj.GetComponent<UIPuzzlePiece>();
                if (piece == null)
                {
                    Debug.LogError("프리팹에 UIPuzzlePiece 컴포넌트가 없습니다!");
                    return;
                }

                piece.Init(this, x, y, prefabIndex);

                bool randomFlip = Random.Range(0, 2) == 1;
                piece.SetFlipped(randomFlip, true);

                pieces.Add(piece);
                initialPositions.Add(pos);
                initialGridX.Add(x);
                initialGridY.Add(y);
                gridIndex++;
            }
        }
        isJoined = false;
        Debug.Log("퍼즐 조각들이 다시 랜덤하게 섞여서 배치되었습니다!");
    }

    public void SwapPieceByRightClick(UIPuzzlePiece clicked)
    {
        if (!isInitialized || isJoined) return; // 초기화 안됐거나 합쳐진 상태면 스왑 차단

        if (selectedForSwap == null)
        {
            selectedForSwap = clicked;
            string faceState = clicked.IsShowingFront() ? "앞면" : "뒷면";
            Debug.Log($"{clicked.pieceNum + 1}번 조각({faceState})이 선택됨.");
        }
        else
        {
            if (selectedForSwap != clicked)
            {
                Vector2 tempPos = selectedForSwap.rectTransform.anchoredPosition;
                selectedForSwap.rectTransform.anchoredPosition = clicked.rectTransform.anchoredPosition;
                clicked.rectTransform.anchoredPosition = tempPos;

                int tempX = selectedForSwap.gridX, tempY = selectedForSwap.gridY;
                selectedForSwap.SetGridPos(clicked.gridX, clicked.gridY);
                clicked.SetGridPos(tempX, tempY);

                string face1 = selectedForSwap.IsShowingFront() ? "앞면" : "뒷면";
                string face2 = clicked.IsShowingFront() ? "앞면" : "뒷면";
                Debug.Log($"{selectedForSwap.pieceNum + 1}번 조각({face1}) ↔ {clicked.pieceNum + 1}번 조각({face2}) 위치 교환!");
            }
            selectedForSwap = null;
        }
    }

    // 퍼즐 중앙으로 붙이기
    public void JoinAllPieces()
    {
        if (!isInitialized || isJoined) return;

        float centerOffsetX = (gridSize - 1) / 2.0f;
        float centerOffsetY = (gridSize - 1) / 2.0f;

        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            int x = piece.gridX;
            int y = piece.gridY;
            int reversedY = gridSize - 1 - y;

            Vector2 joinedPos = new Vector2(
                (x - centerOffsetX) * joinedSpacingX,
                (reversedY - centerOffsetY) * joinedSpacingY
            );

            piece.rectTransform.anchoredPosition = joinedPos;
        }

        isJoined = true;
        Debug.Log("퍼즐이 현재 배치 상태 그대로 중앙에 합쳐졌습니다!");
    }

    public void ResetAllPieces()
    {
        if (!isInitialized) return;

        for (int i = 0; i < pieces.Count; i++)
        {
            pieces[i].rectTransform.anchoredPosition = initialPositions[i];
            pieces[i].SetGridPos(initialGridX[i], initialGridY[i]);
            pieces[i].SetFlipped(false, true);
            pieces[i].ResetRotation();
        }

        selectedForSwap = null;
        isJoined = false;
        Debug.Log("퍼즐이 초기화되었습니다!");
    }

    public bool IsJoinedState()
    {
        return isJoined;
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }
}