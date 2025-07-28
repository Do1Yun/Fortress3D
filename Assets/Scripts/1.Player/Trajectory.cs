using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    public PlayerController playercontrol;
    public Transform firePoint;    // 포탄 발사 위치
    public Vector3 launchVelocity; // 발사 속도 (방향 + 세기)
    public int resolution = 30;    // 라인에 그릴 점 개수
    public float timeStep = 0.1f;  // 점 간격 시간

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        launchVelocity = firePoint.forward * (playercontrol.minLaunchPower + playercontrol.maxLaunchPower) / 2;
        if (playercontrol.isSetting())
        {
            lr.enabled = true;
            DrawTrajectory();
        }
        else
            lr.enabled = false;
    }

    void DrawTrajectory()
    {
        Vector3[] points = new Vector3[resolution];
        Vector3 startPosition = firePoint.position;
        Vector3 velocity = launchVelocity;

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            points[i] = startPosition + velocity * t + 0.5f * Physics.gravity * t * t;
        }

        lr.positionCount = resolution;
        lr.SetPositions(points);
    }
}