using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
    [Header("조준 관련 오브젝트")]
    public Transform turretPivot;    // 포탑 회전 기준점
    public Transform cannonBarrel;   // 포신 회전 기준점 (수직 조준)

    [Header("조준 속도 및 각도")]
    public float verticalAimSpeed = 20f;
    public float horizontalAimSpeed = 30f;
    public float maxAimAngle = 45.0f;
    public float minAimAngle = -20.0f;

    [HideInInspector] public float currentVerticalAngle = 0.0f; // 현재 수직 조준 각도

    // 수직 조준 처리 로직
    public void HandleVerticalAim()
    {
        float input = 0;
        if (Input.GetKey(KeyCode.I)) input = -1;
        else if (Input.GetKey(KeyCode.K)) input = 1;

        currentVerticalAngle += input * verticalAimSpeed * Time.deltaTime;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minAimAngle, maxAimAngle); // 각도 제한
        cannonBarrel.localEulerAngles = new Vector3(currentVerticalAngle, 0, 0); // 로컬 축 기준으로 회전
    }

    // 수평 조준 처리 로직
    public void HandleHorizontalAim()
    {
        float input = 0;
        if (Input.GetKey(KeyCode.J)) input = -1;
        else if (Input.GetKey(KeyCode.L)) input = 1;

        turretPivot.Rotate(Vector3.up, input * horizontalAimSpeed * Time.deltaTime); // 월드 Y축 기준 회전
    }
}