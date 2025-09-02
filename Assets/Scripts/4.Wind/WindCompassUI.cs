using UnityEngine;
using TMPro;

public class WindCompassUI : MonoBehaviour
{
    public TextMeshProUGUI windStrengthText;
    public Transform arrowTransform;
    public Camera mainCamera;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (WindController.instance == null) return;

        if (arrowTransform == null)
        {
            // Inspector에 Arrow Transform이 연결되지 않았을 때 오류를 확실히 표시
            Debug.LogError("오류: Arrow Transform이 할당되지 않았습니다! Inspector 창을 확인해주세요.");
            return; // 여기서 멈춤
        }

        Vector3 windDirection = WindController.instance.CurrentWindDirection;

        // 콘솔 창에 현재 바람 방향(월드 기준)을 출력
        Debug.Log($"[1] 월드 바람 방향: {windDirection.ToString("F3")}");

        Vector3 cameraRelativeDirection = mainCamera.transform.InverseTransformDirection(windDirection);

        // 콘솔 창에 카메라 기준 상대 방향을 출력
        Debug.Log($"[2] 카메라 상대 방향: {cameraRelativeDirection.ToString("F3")}");

        if (cameraRelativeDirection != Vector3.zero)
        {
            arrowTransform.localRotation = Quaternion.LookRotation(cameraRelativeDirection);
        }
    }
}