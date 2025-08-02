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
    private WindZone currentWindZone;

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

        currentWindZone = FindObjectOfType<WindZone>();
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

        if (currentWindZone != null)
        {
            float projectileMass = 1.0f;
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
                Vector3 windVector = currentWindZone.windDirection.normalized * currentWindZone.windStrength;
                windForce = windVector / projectileMass;
            }
        }

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            Vector3 totalAcceleration = gravity + windForce;
            points[i] = startPosition + initialVelocity * t + 0.5f * totalAcceleration * t * t;

            if (i > 0)
            {
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