using UnityEngine;

public class WindDirectionalObject : MonoBehaviour
{
    // 이 3D 모델이 바람을 따라 회전할 때,
    // 모델의 어느 축이 바람 방향을 가리켜야 하는지 설정합니다.
    [SerializeField] private Vector3 modelForwardAxis = Vector3.forward;

    // 회전 속도를 조절하여 부드럽게 회전하도록 합니다.
    [SerializeField] private float rotationSpeed = 5f;

    [Header("바람 세기에 따른 색상 설정")]
    [Tooltip("바람이 약할 때 모델의 색상입니다.")]
    [SerializeField] private Color minColor = Color.cyan;
    [Tooltip("바람이 강할 때 모델의 색상입니다.")]
    [SerializeField] private Color maxColor = Color.red;
    [Tooltip("색상 변화 속도를 조절합니다.")]
    [SerializeField] private float colorChangeSpeed = 5f;

    [Header("카메라 연동")]
    [Tooltip("메인 카메라의 Transform을 할당하여 시점에 따라 회전하도록 합니다.")]
    [SerializeField] private Transform mainCameraTransform;

    private Renderer modelRenderer;

    private void Awake()
    {
        modelRenderer = GetComponent<Renderer>();
        if (modelRenderer == null)
        {
            Debug.LogError("Renderer 컴포넌트를 찾을 수 없습니다. 이 스크립트는 Renderer가 있는 오브젝트에 사용해야 합니다.");
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
        // WindController에서 바람 방향을 가져옵니다.
        Vector3 worldWindDirection = WindController.instance.CurrentWindDirection;

        if (worldWindDirection.magnitude < 0.001f)
        {
            return;
        }

        // 1. 바람의 월드 좌표를 카메라의 로컬 좌표로 변환하여 상대적인 방향을 구합니다.
        // 이렇게 하면 모델이 항상 카메라를 기준으로 회전하게 됩니다.
        Vector3 relativeWindDirection = mainCameraTransform.InverseTransformDirection(worldWindDirection);

        // 2. 모델의 "앞" 축을 바람 방향(상대적인)으로 향하게 할 목표 회전 쿼터니언을 계산합니다.
        Quaternion targetRotation = Quaternion.LookRotation(relativeWindDirection, Vector3.up);

        // 3. 모델의 로컬 회전을 부드럽게 업데이트합니다.
        // 우리는 이미 3D 모델이 UI 카메라에 의해 렌더링되므로, 여기서는 로컬 회전만 조정합니다.
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