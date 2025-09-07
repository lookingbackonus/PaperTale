using System;
using System.Collections.Generic;
using UnityEngine;
public class FlowerPuzzleController : MonoBehaviour
{
    private string TAG = "[FlowerPuzzleController]";
    private List<Flower.FlowerType> flowerPath = new();
    private bool isSuccess = false;
    private bool hasCautionFlower = false; // Caution 꽃이 꺾였는지 추적

    void Awake()
    {
        UIManager.Instance.OpenPuzzleUI<FlowerPuzzleUI>();
    }

    public void AddFlowerColor(Flower.FlowerType flowerType)
    {
        if (isSuccess) return;

        flowerPath.Add(flowerType);
        UIManager.Instance.UpdateFlowerPath(flowerPath);
        // UIManager.Instance.AddItemOnclicked((int)flowerType + 81001); 인벤 추가 안함

        // Danger 꽃을 꺾은 경우 즉시 실패
        FlowerPuzzle flowerPuzzle = FindObjectOfType<FlowerPuzzle>();
        if (flowerPuzzle != null)
        {
            // 현재 꺾은 꽃의 위험도 확인
            Flower.RiskLevel currentRiskLevel = GetFlowerRiskLevel(flowerType, flowerPuzzle);

            if (currentRiskLevel == Flower.RiskLevel.Danger)
            {
                Debug.Log($"{TAG} Danger 꽃({flowerType})을 꺾어서 즉시 실패!");
                // Danger 꽃을 꺾으면 FlowerPuzzle에서 TriggerWitherAll()이 호출되므로 
                // 여기서는 상태만 리셋
                ResetPuzzleState();
                return;
            }
            else if (currentRiskLevel == Flower.RiskLevel.Caution)
            {
                Debug.Log($"{TAG} Caution 꽃({flowerType})을 꺾었습니다. 4송이까지 기다린 후 실패 처리됩니다.");
                hasCautionFlower = true; // Caution 꽃이 꺾였음을 기록
            }
        }

        // 3송이를 꺾었을 때 체크
        if (flowerPath.Count == 3)
        {
            // Caution 꽃이 하나라도 있으면 4송이까지 기다림
            if (hasCautionFlower)
            {
                Debug.Log($"{TAG} Caution 꽃이 포함되어 있어 4송이까지 기다립니다.");
                return; // 4송이까지 기다림
            }

            // 모든 꽃이 Safe인지 확인 (Caution이 없으므로 Safe만 확인하면 됨)
            bool isAllSafeFlowers = CheckAllFlowersSafe();

            if (isAllSafeFlowers)
            {
                Debug.Log($"{TAG} 성공! Safe 꽃 3개를 연속으로 꺾었습니다.");
                UIManager.Instance.ShowResultFlowerPuzzle(true);
                isSuccess = true;
            }
            else
            {
                Debug.Log($"{TAG} 실패! Safe가 아닌 꽃이 포함되어 있습니다.");
                UIManager.Instance.ShowResultFlowerPuzzle(false);
                ResetPuzzleState();
            }
            return;
        }

        // 4송이를 꺾었을 때 (Caution이 있는 경우)
        if (flowerPath.Count >= 4)
        {
            Debug.Log($"{TAG} 4송이를 꺾었습니다. 실패!");
            UIManager.Instance.ShowResultFlowerPuzzle(false);
            ResetPuzzleState();
        }
    }

    // 특정 꽃 타입의 위험도를 가져오는 메서드
    private Flower.RiskLevel GetFlowerRiskLevel(Flower.FlowerType flowerType, FlowerPuzzle flowerPuzzle)
    {
        // FlowerPuzzle의 flowerTypeSettings 배열에서 해당 타입의 위험도 찾기
        var flowerTypeSettings = flowerPuzzle.flowerTypeSettings;

        foreach (var setting in flowerTypeSettings)
        {
            if (setting.flowerType == flowerType)
            {
                return setting.riskLevel;
            }
        }

        // 기본값으로 Safe 반환 (설정을 찾지 못한 경우)
        Debug.LogWarning($"{TAG} {flowerType}의 위험도 설정을 찾을 수 없습니다. Safe로 간주합니다.");
        return Flower.RiskLevel.Safe;
    }

    // 현재 경로의 모든 꽃이 안전한지 확인
    private bool CheckAllFlowersSafe()
    {
        FlowerPuzzle flowerPuzzle = FindObjectOfType<FlowerPuzzle>();
        if (flowerPuzzle == null)
        {
            Debug.LogError($"{TAG} FlowerPuzzle을 찾을 수 없습니다!");
            return false;
        }

        foreach (var flowerType in flowerPath)
        {
            Flower.RiskLevel riskLevel = GetFlowerRiskLevel(flowerType, flowerPuzzle);
            if (riskLevel != Flower.RiskLevel.Safe)
            {
                Debug.Log($"{TAG} {flowerType} 꽃이 안전하지 않습니다. (위험도: {riskLevel})");
                return false;
            }
        }

        Debug.Log($"{TAG} 모든 꽃이 안전합니다!");
        return true;
    }

    // 퍼즐 상태 리셋
    private void ResetPuzzleState()
    {
        flowerPath.Clear();
        isSuccess = false;
        hasCautionFlower = false; // Caution 플래그도 리셋
    }

    public void TimeOut()
    {
        if (!isSuccess)
        {
            Debug.Log($"{TAG} 시간 초과로 실패!");
            UIManager.Instance.ShowResultFlowerPuzzle(false);
            ResetPuzzleState();
        }
    }

    public void OnDestroy()
    {
        var ui = UIManager.Instance.GetUI<FlowerPuzzleUI>(out bool isAlreadyOpen);
        // Debug.Log($"{TAG} OnDestroy {ui}");
        UIManager.Instance.ClosePuzzleUI(ui);
    }
}