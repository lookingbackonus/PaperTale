using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIPuzzlePiece : MonoBehaviour, IPointerClickHandler
{
    [Header("양면 이미지")]
    public Sprite frontSprite;
    public Sprite backSprite;

    [HideInInspector]
    public MapPuzzleManager manager;
    [HideInInspector]
    public int gridX, gridY;
    [HideInInspector]
    public int pieceNum;
    [HideInInspector]
    public RectTransform rectTransform;

    private Image pieceImage;
    private bool isFlipped = false;
    private Vector3 originalScale;
    private bool isFlipping = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        pieceImage = GetComponent<Image>();
        originalScale = transform.localScale;

        if (pieceImage != null && frontSprite != null)
        {
            pieceImage.sprite = frontSprite;
        }
    }

    public void Init(MapPuzzleManager mgr, int x, int y, int num)
    {
        manager = mgr;
        gridX = x;
        gridY = y;
        pieceNum = num;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager.IsJoinedState()) return; // 합쳐진 상태면 클릭 차단

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    public void OnLeftClick()
    {
        if (!isFlipping) // 뒤집는 중이 아닐 때만 실행
        {
            FlipPiece();
        }
    }

    public void OnRightClick()
    {
        manager.SwapPieceByRightClick(this);
    }

    private void FlipPiece()
    {
        StartCoroutine(FlipAnimation());
    }

    private System.Collections.IEnumerator FlipAnimation()
    {
        if (isFlipping) yield break;

        isFlipping = true;
        float duration = 0.3f;
        float halfDuration = duration / 2f;

        float elapsedTime = 0f;
        Vector3 startRotation = transform.eulerAngles;
        Vector3 middleRotation = new Vector3(90f, startRotation.y, startRotation.z);

        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            transform.eulerAngles = Vector3.Lerp(startRotation, middleRotation, progress);
            yield return null;
        }

        isFlipped = !isFlipped;
        if (pieceImage != null)
        {
            pieceImage.sprite = isFlipped ? backSprite : frontSprite;
        }

        elapsedTime = 0f;
        Vector3 endRotation = new Vector3(0f, startRotation.y, startRotation.z);

        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / halfDuration;
            transform.eulerAngles = Vector3.Lerp(middleRotation, endRotation, progress);
            yield return null;
        }

        // 최종 각도 보정
        transform.eulerAngles = endRotation;
        isFlipping = false;

        // 원본 프리팹 번호 + 1로 로그 출력!
        string faceState = IsShowingFront() ? "앞면" : "뒷면";
        Debug.Log($"{pieceNum + 1}번 조각 뒤집기: {faceState}");
    }

    public void SetGridPos(int x, int y)
    {
        gridX = x;
        gridY = y;
    }

    public void SetFlipped(bool flipped, bool immediate = false)
    {
        isFlipped = flipped;

        if (pieceImage != null)
        {
            pieceImage.sprite = isFlipped ? backSprite : frontSprite;
        }

        if (immediate)
        {
            transform.eulerAngles = Vector3.zero;
            isFlipping = false;
        }
    }

    public void ResetRotation()
    {
        transform.eulerAngles = Vector3.zero;
        isFlipping = false;
    }

    public bool IsShowingFront()
    {
        return !isFlipped;
    }

    public void SetSelected(bool selected)
    {
        if (pieceImage != null)
        {
            Color color = pieceImage.color;
            color.a = selected ? 0.7f : 1f;
            pieceImage.color = color;
        }
    }
}