using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;

    [Header("ī�޶� �⺻ ����")]
    public CameraModeSettings defaultSettings = new CameraModeSettings { distance = 7.0f, yaw = 0.0f, pitch = 20.0f, lookAtOffset = new Vector3(0, 1.5f, 0) };

    [Header("���콺 ���� ����")]
    public float freeLook_x_Speed = 120.0f;
    public float freeLook_y_Speed = 120.0f;
    public float yMinLimit = -20f;
    public float yMaxLimit = 89f;

    [Header("���� �Ǻ�(�ޱ� ����) ����")]
    public float pivotPanSpeed = 2.0f;
    public float maxPivotOffset = 5.0f;

    [Header("ī�޶� �ε巯�� ����")]
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

        // --- �Է� ó�� ---
        if (Input.GetMouseButton(1)) // ��ġ ȸ�� (��Ŭ��)
        {
            // [����] LateUpdate�� Time.deltaTime�� ����ؾ� �մϴ�.
            currentX += Input.GetAxis("Mouse X") * freeLook_x_Speed * Time.deltaTime;
            currentY -= Input.GetAxis("Mouse Y") * freeLook_y_Speed * Time.deltaTime;
            currentY = ClampAngle(currentY, yMinLimit, yMaxLimit);
        }
        else // �⺻ ��ġ�� ����
        {
            float desiredYaw = target.eulerAngles.y + currentSettings.yaw;
            currentX = Mathf.LerpAngle(currentX, desiredYaw, rotationDamping * Time.deltaTime);
            currentY = Mathf.LerpAngle(currentY, currentSettings.pitch, rotationDamping * Time.deltaTime);
        }

        if (Input.GetMouseButton(2)) // �ޱ� ���� (�� Ŭ��)
        {
            float panX = -Input.GetAxis("Mouse X") * pivotPanSpeed * Time.deltaTime;
            float panY = -Input.GetAxis("Mouse Y") * pivotPanSpeed * Time.deltaTime;
            lookAtPivot += transform.right * panX + transform.up * panY;
            Vector3 relativePivot = lookAtPivot - currentSettings.lookAtOffset;
            lookAtPivot = currentSettings.lookAtOffset + Vector3.ClampMagnitude(relativePivot, maxPivotOffset);
        }
        else // �ޱ� �߽����� ����
        {
            lookAtPivot = Vector3.Lerp(lookAtPivot, currentSettings.lookAtOffset, pivotReturnDamping * Time.deltaTime);
        }

        // --- ���� ��ġ �� ȸ�� ��� ---
        Quaternion positionRotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 desiredPosition = (target.position + currentSettings.lookAtOffset) - (positionRotation * Vector3.forward * currentSettings.distance);
        Vector3 lookAtPoint = target.position + lookAtPivot;
        Quaternion desiredRotation = Quaternion.LookRotation(lookAtPoint - transform.position);

        // --- ��ȯ ���� ---
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
