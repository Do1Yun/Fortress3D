using UnityEngine;

public class CameraController : MonoBehaviour
{
<<<<<<< Updated upstream
    // 따라다닐 목표 (플레이어 탱크)
=======
>>>>>>> Stashed changes
    private Transform target;
    // 조준 시 바라볼 타겟 (포탑)
    private Transform aimTarget;

    [Header("기본 설정")]
    public Vector3 offset = new Vector3(0, 10f, -8f);
    public float smoothSpeed = 5f;

<<<<<<< Updated upstream
    [Header("조준 모드 설정")]
    public Vector3 aimOffset = new Vector3(2f, 7f, -3f); // 조준 시 더 가까운 위치
    public float aimSmoothSpeed = 10f; // 조준 시 더 빠른 회전 속도

    private bool isAimingMode = false;

    // LateUpdate는 모든 Update 함수가 호출된 후에 실행됩니다.
    // 플레이어가 움직인 '후'에 카메라가 따라가야 렉이나 떨림이 없으므로
    // 카메라 이동은 LateUpdate에서 처리하는 것이 좋습니다.
    void LateUpdate()
    {
        // 타겟이 설정되지 않았다면 아무것도 하지 않음
        if (target == null)
        {
            return;
        }

        // 현재 모드에 맞는 오프셋과 속도를 선택
        Vector3 currentOffset = isAimingMode ? aimOffset : offset;
        float currentSpeed = isAimingMode ? aimSmoothSpeed : smoothSpeed;
=======
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

    // ★★★ 핵심 수정: Rigidbody의 물리 움직임을 부드럽게 따라가기 위해 LateUpdate를 FixedUpdate로 변경합니다. ★★★
    void FixedUpdate()
    {
        if (target == null) return;

        CameraModeSettings currentSettings = defaultSettings;

        // --- 입력 처리 ---
        if (Input.GetMouseButton(1)) // 위치 회전 (우클릭)
        {
            currentX += Input.GetAxis("Mouse X") * freeLook_x_Speed * Time.fixedDeltaTime;
            currentY -= Input.GetAxis("Mouse Y") * freeLook_y_Speed * Time.fixedDeltaTime;
            currentY = ClampAngle(currentY, yMinLimit, yMaxLimit);
        }
        else // 기본 위치로 복귀
        {
            float desiredYaw = target.eulerAngles.y + currentSettings.yaw;
            currentX = Mathf.LerpAngle(currentX, desiredYaw, rotationDamping * Time.fixedDeltaTime);
            currentY = Mathf.LerpAngle(currentY, currentSettings.pitch, rotationDamping * Time.fixedDeltaTime);
        }

        if (Input.GetMouseButton(2)) // 앵글 조정 (휠 클릭)
        {
            float panX = -Input.GetAxis("Mouse X") * pivotPanSpeed * Time.fixedDeltaTime;
            float panY = -Input.GetAxis("Mouse Y") * pivotPanSpeed * Time.fixedDeltaTime;
            lookAtPivot += transform.right * panX + transform.up * panY;
            Vector3 relativePivot = lookAtPivot - currentSettings.lookAtOffset;
            lookAtPivot = currentSettings.lookAtOffset + Vector3.ClampMagnitude(relativePivot, maxPivotOffset);
        }
        else // 앵글 중심으로 복귀
        {
            lookAtPivot = Vector3.Lerp(lookAtPivot, currentSettings.lookAtOffset, pivotReturnDamping * Time.fixedDeltaTime);
        }
>>>>>>> Stashed changes

        // 1. 위치 이동
        Vector3 desiredPosition = target.position + currentOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, currentSpeed * Time.deltaTime);

<<<<<<< Updated upstream
        // 2. 회전 방식 변경
        if (isAimingMode && aimTarget != null)
        {
            // 조준 모드: 카메라의 방향을 포탑의 방향으로 부드럽게 일치시킴
            Quaternion desiredRotation = Quaternion.LookRotation(aimTarget.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, currentSpeed * Time.deltaTime);
        }
        else
        {
            // 기본 모드: 플레이어의 몸체를 바라봄
            transform.LookAt(target);
        }
=======
        // --- 변환 적용 ---
        transform.position = Vector3.Lerp(transform.position, desiredPosition, transitionDamping * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationDamping * Time.fixedDeltaTime);
>>>>>>> Stashed changes
    }

    // 모드 전환을 위한 함수
    public void ToggleAimMode(bool isAiming, Transform newAimTarget = null)
    {
        isAimingMode = isAiming;
        aimTarget = newAimTarget;
    }

    // GameManager가 호출할 함수: 따라다닐 타겟을 변경합니다.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
<<<<<<< Updated upstream
=======
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
>>>>>>> Stashed changes
    }
}
