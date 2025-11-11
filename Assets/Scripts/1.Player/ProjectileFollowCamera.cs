using UnityEngine;
using System.Collections;

public class ProjectileFollowCamera : MonoBehaviour
{
    public static ProjectileFollowCamera instance;

    public Transform target;
    public Vector3 offset = new Vector3(0f, 3f, -8f);

    // ▼▼▼ [수정됨] ▼▼▼
    [Header("카메라 스무딩")]
    [Tooltip("카메라가 타겟을 따라잡는데 걸리는 시간 (작을수록 빠름)")]
    public float smoothTime = 0.1f; // 0.1초 정도가 적당할 수 있습니다.
    private Vector3 cameraVelocity = Vector3.zero; // SmoothDamp가 내부적으로 사용할 변수
    // ▲▲▲ [여기까지 수정] ▲▲▲

    private Camera cam;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        cam = GetComponent<Camera>();
        cam.enabled = false;
    }

    void LateUpdate()
    {
        if (!cam.enabled)
        {
            return;
        }

        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;

            // ▼▼▼ [수정됨] Lerp 대신 SmoothDamp 사용 ▼▼▼
            // 현재 위치에서 desiredPosition까지 smoothTime동안 도달하도록 부드럽게 이동시킵니다.
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref cameraVelocity, smoothTime);
            // ▲▲▲ [여기까지 수정] ▲▲▲

            transform.LookAt(target);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        StopAllCoroutines();

        target = newTarget;

        // ▼▼▼ [수정됨] SmoothDamp를 위해 내부 속도도 리셋 ▼▼▼
        Vector3 desiredPosition = target.position + offset;
        transform.position = desiredPosition;
        transform.LookAt(target);
        cameraVelocity = Vector3.zero; // 카메라 속도를 0으로 초기화
        // ▲▲▲ [여기까지 수정] ▲▲▲

        cam.enabled = true;
        Debug.Log("[FollowCam] 추적 시작: " + newTarget.name);
    }

    public void Deactivate()
    {
        target = null;
        cam.enabled = false;
        Debug.Log("[FollowCam] 추적 종료.");
    }

    public void StartDeactivationDelay(float delay)
    {
        target = null;
        StartCoroutine(DeactivationCoroutine(delay));
    }

    private IEnumerator DeactivationCoroutine(float delay)
    {
        Debug.Log($"[FollowCam] 폭발 감지. {delay}초 후 카메라 종료.");
        yield return new WaitForSeconds(delay);
        Deactivate();
    }
}