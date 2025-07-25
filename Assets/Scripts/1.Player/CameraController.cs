using UnityEngine;

public class CameraController : MonoBehaviour
{
    // 따라다닐 목표 (플레이어 탱크)
    private Transform target;
    // 조준 시 바라볼 타겟 (포탑)
    private Transform aimTarget;

    [Header("기본 설정")]
    public Vector3 offset = new Vector3(0, 10f, -8f);
    public float smoothSpeed = 5f;

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

        // 1. 위치 이동
        Vector3 desiredPosition = target.position + currentOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, currentSpeed * Time.deltaTime);

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
    }
}