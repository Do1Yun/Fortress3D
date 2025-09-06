using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour
{
    public PlayerController playerControl;
    public PlayerShooting playerShooting;

    [Header("조�???�� ?��?��")]
    public int resolution = 30;
    public float maxTime = 2.0f;
    public Gradient lineColor;
    public bool isPainted = true;

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
        if(!isPainted) {
            HideTrajectory();
            return;
        }
        
        Vector3[] points = new Vector3[resolution];
        Vector3 startPosition = firePoint.position;

        float currentPower = playerShooting.GetCurrentLaunchPower();
        Vector3 initialVelocity = firePoint.forward * currentPower;

        Vector3 gravity = Physics.gravity;
        Vector3 windForce = Vector3.zero;

        // ?��?��?��?��?�� ?��?�� ?��?�� �?�? ?��?��?��?��?��
        // WindManager ????�� ?���? 만든 WindController�? 존재?��?���? ?��?��?��?��?��.
        if (WindController.instance != null)
        {
            float projectileMass = 1.0f; // 기본 질량
            // ?��?�� ?��?��?�� ?��?�� ?��리팹?�� �??��?��?��?��. (?��?�� "Bullet" ?��그�?? ?��?�� ?��?��?��?��)
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
                // WindController로�???�� ?��?�� ?��?�� 바람 ?��보�?? �??��?��?��?��.
                Vector3 windVector = WindController.instance.CurrentWindDirection * WindController.instance.CurrentWindStrength;
                // 질량?�� 고려?��?�� 바람?�� �??��?���? 계산?��?��?��.
                windForce = windVector / projectileMass;
            }
        }

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            // 중력�? 바람?�� ?��?�� ?��?��?�� �? �??��?���? 계산?��?��?��.
            Vector3 totalAcceleration = gravity + windForce;
            // ?��물선 ?��?�� 공식?�� ?��?��?��?�� ?���? t?��?��?�� ?��치�?? 계산?��?��?��.
            points[i] = startPosition + initialVelocity * t + 0.5f * totalAcceleration * t * t;

            if (i > 0)
            {
                // 충돌 �??��?�� 계산?��?�� 궤적?�� �??��?�� ?���? ?��?���? ?��?��?��.
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