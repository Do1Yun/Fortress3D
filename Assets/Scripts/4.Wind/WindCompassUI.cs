using UnityEngine;
using TMPro;

public class WindCompassUI : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI windStrengthText;

    [Header("3D 나침반 연결")]
    public Transform arrowTransform;
    public Camera mainCamera;

    private WindController windController;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        windController = WindController.instance;

        if (windController == null || windStrengthText == null || arrowTransform == null)
        {
            Debug.LogError("[WindCompassUI] 시작 실패: Inspector의 모든 필드가 제대로 연결되었는지 확인해주세요!");
            enabled = false;
        }
    }

    void Update()
    {
        // 이제 Update 함수는 디버그 로그 없이 조용히 자기 일만 합니다.
        float strength = windController.CurrentWindStrength;
        Vector3 windDirection = windController.CurrentWindDirection;
        windStrengthText.text = $"바람: {strength:F1}";

        if (mainCamera == null) return;

        Vector3 cameraRelativeDirection = mainCamera.transform.InverseTransformDirection(windDirection);

        if (cameraRelativeDirection != Vector3.zero)
        {
            arrowTransform.localRotation = Quaternion.LookRotation(cameraRelativeDirection);
        }
    }

    /// <summary>
    /// GameManager가 턴을 바꿀 때 호출할 디버깅용 함수입니다.
    /// </summary>
    public void LogRotationOnTurnChange()
    {
        // 함수 호출 시점의 최신 바람 정보를 가져옵니다.
        Vector3 windDirection = windController.CurrentWindDirection;

        // --- 회전 정보 디버깅 로그 ---
        Debug.Log("===== 턴 변경: 회전 정보 스냅샷 =====");
        Debug.Log($"[1] 월드 바람 방향: {windDirection.ToString("F3")}");
        Debug.Log($"[2] 카메라 Forward: {mainCamera.transform.forward.ToString("F3")}");

        Vector3 cameraRelativeDirection = mainCamera.transform.InverseTransformDirection(windDirection);
        Debug.Log($"[3] 계산된 상대 방향: {cameraRelativeDirection.ToString("F3")}");

        if (cameraRelativeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraRelativeDirection);
            Debug.Log($"[4] 최종 적용 회전값: {targetRotation.eulerAngles.ToString("F1")}");
        }
        Debug.Log("=====================================");
    }
}