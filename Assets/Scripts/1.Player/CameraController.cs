using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum CameraMode { Default, SideView, TopDownView }
    private CameraMode currentMode;

    private Transform target;

    [Header("기본 모드 설정")]
    public Vector3 defaultOffset = new Vector3(0, 5f, -6f);
    public float defaultSmoothSpeed = 5f;

    [Header("측면(수직 조준) 모드 설정")]
    public Vector3 sideViewOffset = new Vector3(-5f, 2f, 0f);
    public float sideViewSmoothSpeed = 10f;

    [Header("탑다운(수평 조준) 모드 설정")]
    public Vector3 topDownOffset = new Vector3(0f, 10f, 0f);
    public float topDownSmoothSpeed = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        switch (currentMode)
        {
            case CameraMode.Default:
                Vector3 desiredPosDefault = target.position + target.rotation * defaultOffset;
                transform.position = Vector3.Lerp(transform.position, desiredPosDefault, defaultSmoothSpeed * Time.deltaTime);
                transform.LookAt(target);
                break;

            case CameraMode.SideView:
                Vector3 desiredPosSide = target.position + (target.right * sideViewOffset.x) + (Vector3.up * sideViewOffset.y) + (target.forward * sideViewOffset.z);
                transform.position = Vector3.Lerp(transform.position, desiredPosSide, sideViewSmoothSpeed * Time.deltaTime);
                transform.LookAt(target.position + Vector3.up * 1.5f);
                break;

            case CameraMode.TopDownView:
                Vector3 desiredPosTop = target.position + topDownOffset;
                transform.position = Vector3.Lerp(transform.position, desiredPosTop, topDownSmoothSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(90, target.eulerAngles.y, 0), topDownSmoothSpeed * Time.deltaTime);
                break;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SwitchMode(CameraMode mode)
    {
        currentMode = mode;
    }
}