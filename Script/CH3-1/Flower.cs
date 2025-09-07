using UnityEngine;

public class Flower : MonoBehaviour
{
    // 꽃 타입과 위험도 enum 정의
    public enum FlowerType { Red, Yellow, Blue, White, Star, Black }
    public enum RiskLevel { Safe, Caution, Danger }

    // 이 스크립트는 단순히 enum 정의용임
    // 실제 로직은 FlowerPuzzle에서 처리

    [Header("디버그용 - 실제로는 FlowerPuzzle에서 관리됨")]
    [SerializeField] private FlowerType currentType;
    [SerializeField] private RiskLevel currentRisk;
    [SerializeField] private bool isPicked = false;

    // 에디터에서 현재 설정을 확인할 수 있도록 하는 메서드
    public void SetDebugInfo(FlowerType type, RiskLevel risk, bool picked)
    {
        currentType = type;
        currentRisk = risk;
        isPicked = picked;
    }

    void OnDrawGizmos()
    {
        // FlowerPuzzle에서 설정된 정보를 바탕으로 기즈모 색상 결정
        Color gizmoColor = GetGizmoColorByName();

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    Color GetGizmoColorByName()
    {
        string name = gameObject.name;

        switch (name)
        {
            case "Red": return Color.red;
            case "Yellow": return Color.yellow;
            case "Blue": return Color.blue;
            case "White": return Color.white;
            case "Star": return Color.magenta;
            case "Black": return Color.black;
            default: return Color.gray;
        }
    }
}