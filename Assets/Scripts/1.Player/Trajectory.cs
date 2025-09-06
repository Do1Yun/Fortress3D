using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour
{
    public PlayerController playerControl;
    public PlayerShooting playerShooting;

    [Header("ì¡°ì???„  ?„¤? •")]
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

        // ?˜…?˜…?˜…?˜…?˜… ?•µ?‹¬ ?ˆ˜? • ë¶?ë¶? ?˜…?˜…?˜…?˜…?˜…
        // WindManager ????‹  ?ƒˆë¡? ë§Œë“  WindControllerê°? ì¡´ì¬?•˜?Š”ì§? ?™•?¸?•©?‹ˆ?‹¤.
        if (WindController.instance != null)
        {
            float projectileMass = 1.0f; // ê¸°ë³¸ ì§ˆëŸ‰
            // ?˜„?¬ ?„ ?ƒ?œ ?¬?ƒ„ ?”„ë¦¬íŒ¹?„ ê°?? ¸?˜µ?‹ˆ?‹¤. (?´? œ "Bullet" ?ƒœê·¸ê?? ?•„?š” ?—†?Šµ?‹ˆ?‹¤)
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
                // WindControllerë¡œë???„° ?˜„?¬ ? „?—­ ë°”ëŒ ? •ë³´ë?? ê°?? ¸?˜µ?‹ˆ?‹¤.
                Vector3 windVector = WindController.instance.CurrentWindDirection * WindController.instance.CurrentWindStrength;
                // ì§ˆëŸ‰?„ ê³ ë ¤?•˜?—¬ ë°”ëŒ?˜ ê°??†?„ë¥? ê³„ì‚°?•©?‹ˆ?‹¤.
                windForce = windVector / projectileMass;
            }
        }

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            // ì¤‘ë ¥ê³? ë°”ëŒ?˜ ?˜?„ ?•©?‚°?•œ ì´? ê°??†?„ë¥? ê³„ì‚°?•©?‹ˆ?‹¤.
            Vector3 totalAcceleration = gravity + windForce;
            // ?¬ë¬¼ì„  ?š´?™ ê³µì‹?„ ?‚¬?š©?•˜?—¬ ?‹œê°? t?—?„œ?˜ ?œ„ì¹˜ë?? ê³„ì‚°?•©?‹ˆ?‹¤.
            points[i] = startPosition + initialVelocity * t + 0.5f * totalAcceleration * t * t;

            if (i > 0)
            {
                // ì¶©ëŒ ì§?? ?„ ê³„ì‚°?•˜?—¬ ê¶¤ì ?´ ì§??˜•?„ ?š«ì§? ?•Š?„ë¡? ?•©?‹ˆ?‹¤.
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