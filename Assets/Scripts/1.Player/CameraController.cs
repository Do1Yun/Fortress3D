using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;

    [Header("카메라 기본 설정")]
    public CameraModeSettings defaultSettings = new CameraModeSettings { distance = 7.0f, yaw = 0.0f, pitch = 20.0f, lookAtOffset = new Vector3(0, 1.5f, 0) };

    [Header("마우스 조작 설정")]
    public float freeLook_x_Speed = 120.0f;
    public float freeLook_y_Speed = 120.0f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 89f;

    [Header("시점 피봇(앵글 조정) 설정")]
    public float pivotPanSpeed = 2.0f;
    public float maxPivotOffset = 5.0f;

    [Header("카메라 부드러움 설정")]
    public float rotationDamping = 8.0f;
    public float transitionDamping = 8.0f;
    public float pivotReturnDamping = 5.0f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private Vector3 lookAtPivot = Vector3.zero;

    [System.Serializable]
    public struct CameraModeSettings
    {
        public float distance;
        public float yaw;
        public float pitch;
        public Vector3 lookAtOffset;
    }
    void LateUpdate()
    {
        if (target == null) return;

        CameraModeSettings currentSettings = defaultSettings;

        // --- 입력 처리 ---
        if (Input.GetMouseButton(1)) // 위치 회전 (우클릭)
        {
            // [수정] LateUpdate는 Time.deltaTime을 사용해야 합니다.
            currentX += Input.GetAxis("Mouse X") * freeLook_x_Speed * Time.deltaTime;
            currentY -= Input.GetAxis("Mouse Y") * freeLook_y_Speed * Time.deltaTime;
            currentY = ClampAngle(currentY, yMinLimit, yMaxLimit);
        }
        else // 기본 위치로 복귀
        {
            float desiredYaw = target.eulerAngles.y + currentSettings.yaw;
            currentX = Mathf.LerpAngle(currentX, desiredYaw, rotationDamping * Time.deltaTime);
            currentY = Mathf.LerpAngle(currentY, currentSettings.pitch, rotationDamping * Time.deltaTime);
        }

        if (Input.GetMouseButton(2)) // 앵글 조정 (휠 클릭)
        {
            float panX = -Input.GetAxis("Mouse X") * pivotPanSpeed * Time.deltaTime;
            float panY = -Input.GetAxis("Mouse Y") * pivotPanSpeed * Time.deltaTime;
            lookAtPivot += transform.right * panX + transform.up * panY;
            Vector3 relativePivot = lookAtPivot - currentSettings.lookAtOffset;
            lookAtPivot = currentSettings.lookAtOffset + Vector3.ClampMagnitude(relativePivot, maxPivotOffset);
        }
        else // 앵글 중심으로 복귀
        {
            lookAtPivot = Vector3.Lerp(lookAtPivot, currentSettings.lookAtOffset, pivotReturnDamping * Time.deltaTime);
        }

        // --- 최종 위치 및 회전 계산 ---
        Quaternion positionRotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 desiredPosition = (target.position + currentSettings.lookAtOffset) - (positionRotation * Vector3.forward * currentSettings.distance);
        Vector3 lookAtPoint = target.position + lookAtPivot;
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPoint - transform.position);

        // --- 변환 적용 ---
        transform.position = Vector3.Lerp(transform.position, desiredPosition, transitionDamping * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationDamping * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            currentX = target.eulerAngles.y + defaultSettings.yaw;
            currentY = defaultSettings.pitch;
            lookAtPivot = defaultSettings.lookAtOffset;
        }
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
