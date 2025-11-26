using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour
{
    public PlayerController playerControl;
    public PlayerShooting playerShooting;

    [Header("조준선 관련 설정")]
    public int resolution = 30;
    public float maxTime = 2.0f;
    public Gradient lineColor;
    public bool isPainted = true;

    [Header("추가 설정")]
    public float fixedLaunchPower = 50.0f;

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
    public void ShowFixedTrajectory() // 고정조준선 출력
    {
        if (!enabled || lr == null) return;
        lr.enabled = true;
        DrawFixedPowerTrajectory(firePoint.forward);
    }

    public void ShowTrajectory()
    {
        if (!enabled || lr == null) return;
        lr.enabled = true;

        DrawTrajectory(firePoint.forward, playerShooting.GetCurrentLaunchPower());
    }
    public void HideTrajectory()
    {
        if (lr == null) return;
        lr.enabled = false;
    }

    // ★★★ 기존 로직 (현재 파워를 사용하는 함수) ★★★
    public void DrawTrajectory(Vector3 aimDirection, float currentPower)
    {
        if (lr == null) return;
        Debug.Log("현재 파워 : " + currentPower);

        List<Vector3> points = new List<Vector3>();
        Vector3 startPosition = firePoint.position;
        Vector3 initialVelocity = aimDirection.normalized * currentPower;

        Vector3 gravity = Physics.gravity;
        Vector3 windForce = Vector3.zero;

        //if (WindController.instance != null)
        //{
        //    float projectileMass = 1.0f;
        //    GameObject currentPrefab = playerShooting.GetCurrentProjectilePrefab();
        //    if (currentPrefab != null)
        //    {
        //        Rigidbody projRb = currentPrefab.GetComponent<Rigidbody>();
        //        if (projRb != null)
        //        {
        //            projectileMass = projRb.mass;
        //        }
        //    }
        //    if (projectileMass > 0)
        //    {
        //        Vector3 windVector = WindController.instance.CurrentWindDirection * WindController.instance.CurrentWindStrength;
        //        windForce = windVector / projectileMass;
        //    }
        //}

        Vector3 totalAcceleration = gravity + windForce;

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            Vector3 point = startPosition + initialVelocity * t + 0.5f * totalAcceleration * t * t;
            points.Add(point);

            if (i > 0)
            {
                if (Physics.Linecast(points[i - 1], points[i], out RaycastHit hit, LayerMask.GetMask("Ground")))
                {
                    points[i] = hit.point;
                    lr.positionCount = i + 1;
                    lr.SetPositions(points.ToArray());
                    return;
                }
            }
        }

        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }

    // ★★★ 새로 추가된 함수 (고정된 파워 50을 사용하는 함수) ★★★
    public void DrawFixedPowerTrajectory(Vector3 aimDirection)
    {
        // 궤적을 그리는 핵심 로직은 DrawTrajectory 함수를 재활용
        DrawTrajectory(aimDirection, fixedLaunchPower);
    }
}