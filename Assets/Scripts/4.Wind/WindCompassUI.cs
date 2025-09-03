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
            // Inspector�� Arrow Transform�� ������� �ʾ��� �� ������ Ȯ���� ǥ��
            Debug.LogError("����: Arrow Transform�� �Ҵ���� �ʾҽ��ϴ�! Inspector â�� Ȯ�����ּ���.");
            return; // ���⼭ ����
        }

        Vector3 windDirection = WindController.instance.CurrentWindDirection;

        // �ܼ� â�� ���� �ٶ� ����(���� ����)�� ���
        Debug.Log($"[1] ���� �ٶ� ����: {windDirection.ToString("F3")}");

        Vector3 cameraRelativeDirection = mainCamera.transform.InverseTransformDirection(windDirection);

        // �ܼ� â�� ī�޶� ���� ��� ������ ���
        Debug.Log($"[2] ī�޶� ��� ����: {cameraRelativeDirection.ToString("F3")}");

        if (cameraRelativeDirection != Vector3.zero)
        {
            arrowTransform.localRotation = Quaternion.LookRotation(cameraRelativeDirection);
        }
    }
}