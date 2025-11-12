// Scripts.zip/1.Player/CameraController.cs

using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;
    private Transform turretToFollow;
    private PlayerController playerController; // ▼▼▼ [1. PlayerController 변수 추가] ▼▼▼

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
            // ▼▼▼ [3. 핵심 수정] ▼▼▼

            // 1. 기본값은 'target' (플레이어 본체)을 따라갑니다.
            Transform horizontalTarget = target;

            // 2. '이동' 상태가 아닐 때만 포탑을 따라가도록 조건을 추가합니다.
            if (turretToFollow != null && playerController != null &&
                playerController.currentState != PlayerController.PlayerState.Moving)
            {
                // '이동' 상태가 아니면 'turretToFollow' (포탑)을 따라갑니다.
                horizontalTarget = turretToFollow;
            }

            float desiredYaw = horizontalTarget.eulerAngles.y + currentSettings.yaw;
            // ▲▲▲ [수정 완료] ▲▲▲

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

    // ▼▼▼ [2. 수정된 함수] ▼▼▼
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        turretToFollow = null;
        playerController = null; // 타겟 변경 시 초기화

        if (target != null)
        {
            // 타겟(PlayerController가 있는 오브젝트)에서 PlayerController 컴포넌트를 찾습니다.
            playerController = target.GetComponent<PlayerController>();

            // 타겟에서 PlayerAiming 컴포넌트를 찾습니다.
            PlayerAiming aimingComponent = target.GetComponent<PlayerAiming>();
            if (aimingComponent != null)
            {
                // PlayerAiming이 있다면, 그 컴포넌트의 turretPivot을 따라갈 대상으로 저장합니다.
                turretToFollow = aimingComponent.turretPivot;
            }

            // 카메라의 초기 X축 회전값을 설정합니다.
            // (참고: SetTarget 시점에는 아직 'Moving' 상태가 아닐 수 있으므로, 
            //  본체 기준으로 초기화하는 것이 더 안정적일 수 있습니다.)
            float initialYaw = target.eulerAngles.y;

            currentX = initialYaw + defaultSettings.yaw;
            currentY = defaultSettings.pitch;
            lookAtPivot = defaultSettings.lookAtOffset;
        }
    }
    // ▲▲▲ [수정 완료] ▲▲▲

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    public void SetCamera(Transform newTarget)  // 우당탕탕 만들때 만든 함수
    {
        transform.position = newTarget.position;
        transform.rotation = newTarget.rotation;
    }
}