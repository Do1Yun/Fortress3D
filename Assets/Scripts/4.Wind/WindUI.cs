using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [Tooltip("바람 세기를 표시할 TextMeshPro UI")]
    public TextMeshProUGUI windStrengthText;

    [Tooltip("바람 방향을 가리킬 화살표 이미지의 RectTransform")]
    public RectTransform windArrow;

    void Update()
    {
        if (WindController.instance == null) return;

        // WindController에서 현재 바람 정보를 가져옵니다.
        float strength = WindController.instance.CurrentWindStrength;
        Vector3 direction = WindController.instance.CurrentWindDirection;

        // 텍스트 업데이트 (예: "바람: 5.4")
        if (windStrengthText != null)
        {
            windStrengthText.text = $"바람: {strength:F1}";
        }

        // 화살표 방향 업데이트
        if (windArrow != null)
        {
            // 3D 방향을 2D UI에 맞게 변환하여 화살표를 회전시킵니다.
            // 바람의 x, z 방향을 사용하여 각도를 계산합니다.
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            windArrow.rotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}