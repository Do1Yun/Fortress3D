using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    public PlayerController playercontrol;
    public Transform firePoint;    // ��ź �߻� ��ġ
    public Vector3 launchVelocity; // �߻� �ӵ� (���� + ����)
    public int resolution = 30;    // ���ο� �׸� �� ����
    public float timeStep = 0.1f;  // �� ���� �ð�

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