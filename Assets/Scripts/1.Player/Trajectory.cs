using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour
{
    public PlayerController playerControl;
    public PlayerShooting playerShooting;

    [Header("조준선 설정")]
    public int resolution = 30;
    public float maxTime = 2.0f;
    public Gradient lineColor;

    private float timeStep;
    private Transform firePoint;
    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.colorGradient = lineColor;
        timeStep = maxTime / resolution;

        if (playerControl == null || playerShooting == null)
        {
            enabled = false;
            return;
        }

        firePoint = playerShooting.firePoint;
    }

    public void ShowTrajectory()
    {
        if (!enabled || lr == null) return;
        lr.enabled = true;
        DrawTrajectory();
    }

    public void HideTrajectory()
    {
        if (lr == null) return;
        lr.enabled = false;
    }

    void DrawTrajectory()
    {
        Vector3[] points = new Vector3[resolution];
        Vector3 startPosition = firePoint.position;

        float currentPower = playerShooting.GetCurrentLaunchPower();
        Vector3 initialVelocity = firePoint.forward * currentPower;

        Vector3 gravity = Physics.gravity;
        Vector3 windForce = Vector3.zero;

        // ★★★★★ 핵심 수정 부분 ★★★★★
        // WindManager 대신 새로 만든 WindController가 존재하는지 확인합니다.
        if (WindController.instance != null)
        {
            float projectileMass = 1.0f; // 기본 질량
            // 현재 선택된 포탄 프리팹을 가져옵니다. (이제 "Bullet" 태그가 필요 없습니다)
            GameObject currentPrefab = playerShooting.GetCurrentProjectilePrefab();
            if (currentPrefab != null)
            {
                Rigidbody projRb = currentPrefab.GetComponent<Rigidbody>();
                if (projRb != null)
                {
                    projectileMass = projRb.mass;
                }
            }

            if (projectileMass > 0)
            {
                // WindController로부터 현재 전역 바람 정보를 가져옵니다.
                Vector3 windVector = WindController.instance.CurrentWindDirection * WindController.instance.CurrentWindStrength;
                // 질량을 고려하여 바람의 가속도를 계산합니다.
                windForce = windVector / projectileMass;
            }
        }

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            // 중력과 바람의 힘을 합산한 총 가속도를 계산합니다.
            Vector3 totalAcceleration = gravity + windForce;
            // 포물선 운동 공식을 사용하여 시간 t에서의 위치를 계산합니다.
            points[i] = startPosition + initialVelocity * t + 0.5f * totalAcceleration * t * t;

            if (i > 0)
            {
                // 충돌 지점을 계산하여 궤적이 지형을 뚫지 않도록 합니다.
                if (Physics.Linecast(points[i - 1], points[i], out RaycastHit hit, LayerMask.GetMask("Ground")))
                {
                    points[i] = hit.point;
                    lr.positionCount = i + 1;
                    lr.SetPositions(points);
                    return;
                }
            }
        }

        lr.positionCount = resolution;
        lr.SetPositions(points);
    }
}