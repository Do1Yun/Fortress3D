using UnityEngine;

public class WindDirectionalObject : MonoBehaviour
{
    // �� 3D ���� �ٶ��� ���� ȸ���� ��,
    // ���� ��� ���� �ٶ� ������ �����Ѿ� �ϴ��� �����մϴ�.
    [SerializeField] private Vector3 modelForwardAxis = Vector3.forward;

    // ȸ�� �ӵ��� �����Ͽ� �ε巴�� ȸ���ϵ��� �մϴ�.
    [SerializeField] private float rotationSpeed = 5f;

    [Header("�ٶ� ���⿡ ���� ���� ����")]
    [Tooltip("�ٶ��� ���� �� ���� �����Դϴ�.")]
    [SerializeField] private Color minColor = Color.cyan;
    [Tooltip("�ٶ��� ���� �� ���� �����Դϴ�.")]
    [SerializeField] private Color maxColor = Color.red;
    [Tooltip("���� ��ȭ �ӵ��� �����մϴ�.")]
    [SerializeField] private float colorChangeSpeed = 5f;

    [Header("ī�޶� ����")]
    [Tooltip("���� ī�޶��� Transform�� �Ҵ��Ͽ� ������ ���� ȸ���ϵ��� �մϴ�.")]
    [SerializeField] private Transform mainCameraTransform;

    private Renderer modelRenderer;

    private void Awake()
    {
        modelRenderer = GetComponent<Renderer>();
        if (modelRenderer == null)
        {
            Debug.LogError("Renderer ������Ʈ�� ã�� �� �����ϴ�. �� ��ũ��Ʈ�� Renderer�� �ִ� ������Ʈ�� ����ؾ� �մϴ�.");
        }
    }

    private void Update()
    {
        if (WindController.instance != null && modelRenderer != null && mainCameraTransform != null)
        {
            RotateTowardsWindDirection();
            ColorByWindStrength();
        }
    }

    private void RotateTowardsWindDirection()
    {
        // WindController���� �ٶ� ������ �����ɴϴ�.
        Vector3 worldWindDirection = WindController.instance.CurrentWindDirection;

        if (worldWindDirection.magnitude < 0.001f)
        {
            return;
        }

        // 1. �ٶ��� ���� ��ǥ�� ī�޶��� ���� ��ǥ�� ��ȯ�Ͽ� ������� ������ ���մϴ�.
        // �̷��� �ϸ� ���� �׻� ī�޶� �������� ȸ���ϰ� �˴ϴ�.
        Vector3 relativeWindDirection = mainCameraTransform.InverseTransformDirection(worldWindDirection);

        // 2. ���� "��" ���� �ٶ� ����(�������)���� ���ϰ� �� ��ǥ ȸ�� ���ʹϾ��� ����մϴ�.
        Quaternion targetRotation = Quaternion.LookRotation(relativeWindDirection, Vector3.up);

        // 3. ���� ���� ȸ���� �ε巴�� ������Ʈ�մϴ�.
        // �츮�� �̹� 3D ���� UI ī�޶� ���� �������ǹǷ�, ���⼭�� ���� ȸ���� �����մϴ�.
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    private void ColorByWindStrength()
    {
        float currentWindStrength = WindController.instance.CurrentWindStrength;
        float minWind = WindController.instance.minWindStrength;
        float maxWind = WindController.instance.maxWindStrength;

        float normalizedStrength = Mathf.InverseLerp(minWind, maxWind, currentWindStrength);

        Color targetColor = Color.Lerp(minColor, maxColor, normalizedStrength);

        modelRenderer.material.color = Color.Lerp(modelRenderer.material.color, targetColor, Time.deltaTime * colorChangeSpeed);
    }
}