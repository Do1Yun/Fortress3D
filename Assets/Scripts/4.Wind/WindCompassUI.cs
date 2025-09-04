using UnityEngine;
using TMPro;

public class WindCompassUI : MonoBehaviour
{
    [Header("UI ����")]
    public TextMeshProUGUI windStrengthText;

    [Header("3D ��ħ�� ����")]
    public Transform arrowTransform;
    public Camera mainCamera;

    private WindController windController;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        windController = WindController.instance;

        if (windController == null || windStrengthText == null || arrowTransform == null)
        {
            Debug.LogError("[WindCompassUI] ���� ����: Inspector�� ��� �ʵ尡 ����� ����Ǿ����� Ȯ�����ּ���!");
            enabled = false;
        }
    }

    void Update()
    {
        // ���� Update �Լ��� ����� �α� ���� ������ �ڱ� �ϸ� �մϴ�.
        float strength = windController.CurrentWindStrength;
        Vector3 windDirection = windController.CurrentWindDirection;
        windStrengthText.text = $"�ٶ�: {strength:F1}";

        if (mainCamera == null) return;

        Vector3 cameraRelativeDirection = mainCamera.transform.InverseTransformDirection(windDirection);

        if (cameraRelativeDirection != Vector3.zero)
        {
            arrowTransform.localRotation = Quaternion.LookRotation(cameraRelativeDirection);
        }
    }

    /// <summary>
    /// GameManager�� ���� �ٲ� �� ȣ���� ������ �Լ��Դϴ�.
    /// </summary>
    public void LogRotationOnTurnChange()
    {
        // �Լ� ȣ�� ������ �ֽ� �ٶ� ������ �����ɴϴ�.
        Vector3 windDirection = windController.CurrentWindDirection;

        // --- ȸ�� ���� ����� �α� ---
        Debug.Log("===== �� ����: ȸ�� ���� ������ =====");
        Debug.Log($"[1] ���� �ٶ� ����: {windDirection.ToString("F3")}");
        Debug.Log($"[2] ī�޶� Forward: {mainCamera.transform.forward.ToString("F3")}");

        Vector3 cameraRelativeDirection = mainCamera.transform.InverseTransformDirection(windDirection);
        Debug.Log($"[3] ���� ��� ����: {cameraRelativeDirection.ToString("F3")}");

        if (cameraRelativeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraRelativeDirection);
            Debug.Log($"[4] ���� ���� ȸ����: {targetRotation.eulerAngles.ToString("F1")}");
        }
        Debug.Log("=====================================");
    }
}