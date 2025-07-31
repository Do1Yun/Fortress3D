using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour
{
    // PlayerController와 PlayerShooting 모두 참조해야 합니다.
    public PlayerController playerControl;   // <-- 이 필드가 존재해야 합니다. (인스펙터에서 연결 필수)
    public PlayerShooting playerShooting;    // <-- 이 필드도 존재해야 합니다. (인스펙터에서 연결 필수)

    public int resolution = 30;    // 라인에 그릴 점 개수
    public float timeStep = 0.1f;  // 점 간격 시간

    private Transform firePoint;    // 포탄 발사 위치
    private LineRenderer lr;
    private WindZone currentWindZone; // 현재 활성화된 WindZone 참조

    void Awake()
    {
        lr = GetComponent<LineRenderer>();

        // 두 참조 모두 null 체크
        if (playerControl == null || playerShooting == null)
        {
            Debug.LogError("Trajectory 스크립트에 PlayerControl 또는 PlayerShooting이 할당되지 않았습니다.", this);
            enabled = false; // 스크립트 비활성화
            return;
        }
        
        // firePoint는 PlayerShooting 스크립트에서 가져옵니다.
        firePoint = playerShooting.firePoint;

        // 씬에서 WindZone을 찾아 참조 (간단한 예시, 복수의 WindZone이 있다면 더 복잡한 로직 필요)
        currentWindZone = FindObjectOfType<WindZone>();
    }

    void Update()
    {
        // playerControl이 할당되지 않았다면 처리하지 않음
        if (playerControl == null) return; 

        // PlayerController의 조준 상태 확인 함수를 호출
        if (playerControl.IsAimingOrSettingPower())
        {
            lr.enabled = true;
            DrawTrajectory();
        }
        else
        {
            lr.enabled = false;
        }
    }

    void DrawTrajectory()
    {
        Vector3[] points = new Vector3[resolution];
        Vector3 startPosition = firePoint.position;
        
        // 현재 발사 파워는 PlayerShooting 스크립트에서 가져옵니다.
        // 이 함수는 PlayerShooting에 정의되어 있습니다.
        float currentPower = playerShooting.GetCurrentLaunchPower(); 
        Vector3 initialVelocity = firePoint.forward * currentPower;
        
        // 중력 가속도
        Vector3 gravity = Physics.gravity;
        // 바람 영향 (WindZone이 있다면 적용)
        Vector3 windForce = Vector3.zero;
        if (currentWindZone != null)
        {
            float projectileMass = 1.0f; // Projectile 프리팹의 Rigidbody mass 값으로 대체 필요
            if (playerShooting.projectilePrefab != null) { // Projectile 프리팹은 PlayerShooting에 있습니다.
                Rigidbody projRb = playerShooting.projectilePrefab.GetComponent<Rigidbody>();
                if (projRb != null) projectileMass = projRb.mass;
            }
            windForce = currentWindZone.windDirection.normalized * currentWindZone.windStrength / projectileMass;
        }

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            
            Vector3 totalAcceleration = gravity + windForce; 

            points[i] = startPosition + initialVelocity * t + 0.5f * totalAcceleration * t * t;

            if (i > 0)
            {
                RaycastHit hit;
                // LayerMask를 "Ground" 또는 "Terrain" 등으로 설정하여 지형만 충돌 검사
                // (지형 레이어를 설정해두면 좋습니다)
                if (Physics.Linecast(points[i-1], points[i], out hit, LayerMask.GetMask("Ground")))
                {
                    lr.positionCount = i + 1;
                    points[i] = hit.point;
                    break;
                }
            }
        }

        lr.positionCount = resolution;
        lr.SetPositions(points);
    }
}