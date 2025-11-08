using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ChasingObject : MonoBehaviour
{
    [Header("타겟 설정")]
    public Transform player1;
    public Transform player2;
    private Transform currentTarget;
    private GameManager gameManager;

    [Header("이동 설정")]
    public float moveSpeed = 5.0f;
    public float turnSpeed = 180.0f;
    public float gravityValue = -9.81f;
    [Tooltip("이 거리 안으로 접근하면 폭발합니다. (인력탄 제외)")]
    public float detonationDistance = 2.0f;
    [Tooltip("이 시간(초) 안에 타겟에 도달하지 못하면 그 자리에서 자폭합니다.")]
    public float selfDestructTime = 5.0f;

    [Header("경사면 설정")]
    public float slopeAdaptSpeed = 10f;
    public float slopeRaycastLength = 1.5f;

    [Header("센서 설정")]
    public float cliffCheckForwardOffset = 1.0f;
    public float cliffCheckRayLength = 3.0f;
    public LayerMask groundLayer;
    public float raycastHeightOffset = 0.5f;

    [Header("폭발 설정")]
    public float explosionRadius = 5.0f;
    public GameObject explosionEffectPrefab;
    public float terrainModificationStrength = 2.0f;
    public float explosionForce = 500f;
    public float playerKnockbackForce = 20f;

    [Header("Slow Terrain Effect")]
    [Tooltip("지형 효과가 지속되는 총 시간")]
    public float terrainEffectDuration = 2.0f;
    [Tooltip("지형 효과를 몇 단계로 나눌지")]
    public int terrainEffectSteps = 10;

    private ProjectileType explosionType;
    private bool hasExploded = false;
    private bool isActivated = false;

    private CharacterController controller;
    private Vector3 playerVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null && gameManager.players.Count >= 2)
        {
            player1 = gameManager.players[0].transform;
            player2 = gameManager.players[1].transform;
        }
        else
        {
            Debug.LogError("ChasingObject가 GameManager에서 플레이어를 찾을 수 없습니다!", this);
            isActivated = false;
        }
    }

    public void Initialize(ProjectileType type)
    {
        this.explosionType = type;
        this.isActivated = true;

        float stopDist = (type == ProjectileType.TerrainPull) ? explosionRadius : detonationDistance;
        Debug.Log($"[CHASER_DEBUG] 추적자 활성화! 임무: {type}. 정지 거리: {stopDist}m (Radius: {explosionRadius}m / Detonation: {detonationDistance}m)");

        Invoke("Explode", selfDestructTime);
    }

    void Update()
    {
        if (!isActivated || hasExploded) return;

        FindClosestPlayer();
        if (currentTarget == null) return;

        ApplyGravityAndSlope();

        bool isCliffAhead = CheckForCliffs();

        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetFlatPos = new Vector3(currentTarget.position.x, 0, currentTarget.position.z);
        float currentHorizontalDistance = Vector3.Distance(flatPos, targetFlatPos);

        float stoppingDistance;
        if (explosionType == ProjectileType.TerrainPull)
        {
            stoppingDistance = explosionRadius;
        }
        else
        {
            stoppingDistance = detonationDistance;
        }

        if (currentHorizontalDistance <= stoppingDistance)
        {
            Explode();
            return;
        }

        (Vector3 potentialMoveDelta, bool isChasing) action;
        if (isCliffAhead)
        {
            action = HandleAvoidance();
        }
        else
        {
            action = HandleChasing();
        }

        Vector3 horizontalMove = new Vector3(action.potentialMoveDelta.x, 0, action.potentialMoveDelta.z);
        Vector3 verticalMove = new Vector3(0, action.potentialMoveDelta.y, 0);
        float horizontalMoveDistance = horizontalMove.magnitude;

        // distanceToBoundary가 0보다 클 때만 이 로직을 실행 (이미 경계를 넘었을 경우 제외)
        float distanceToBoundary = currentHorizontalDistance - stoppingDistance;

        if (action.isChasing && horizontalMoveDistance > distanceToBoundary && distanceToBoundary > 0)
        {
            Vector3 clampedHorizontalMove = horizontalMove.normalized * distanceToBoundary;
            controller.Move(clampedHorizontalMove + verticalMove);
            Explode();
            return;
        }

        controller.Move(action.potentialMoveDelta);
    }

    void FindClosestPlayer()
    {
        if (player1 == null || player2 == null)
        {
            if (player1 != null) currentTarget = player1;
            else if (player2 != null) currentTarget = player2;
            else currentTarget = null;
            return;
        }

        float distToPlayer1 = Vector3.Distance(transform.position, player1.position);
        float distToPlayer2 = Vector3.Distance(transform.position, player2.position);
        currentTarget = (distToPlayer1 < distToPlayer2) ? player1 : player2;
    }

    void ApplyGravityAndSlope()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRaycastLength, groundLayer))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * transform.rotation, slopeAdaptSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravityValue * Time.deltaTime;
    }

    bool CheckForCliffs()
    {
        Vector3 rayStart = transform.position + (transform.forward * cliffCheckForwardOffset) + (Vector3.up * raycastHeightOffset);
        if (Physics.Raycast(rayStart, Vector3.down, cliffCheckRayLength, groundLayer))
        {
            return false;
        }
        return true;
    }

    (Vector3, bool) HandleAvoidance()
    {
        transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        return (playerVelocity * Time.deltaTime, false);
    }

    (Vector3, bool) HandleChasing()
    {
        Vector3 directionToTarget = currentTarget.position - transform.position;
        directionToTarget.y = 0;
        directionToTarget.Normalize();

        Vector3 horizontalForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

        if (directionToTarget.sqrMagnitude > 0.01f && horizontalForward.sqrMagnitude > 0.01f)
        {
            float angle = Vector3.SignedAngle(horizontalForward, directionToTarget, Vector3.up);
            float rotateAmount = Mathf.Sign(angle) * turnSpeed * Time.deltaTime;

            if (Mathf.Abs(rotateAmount) > Mathf.Abs(angle))
            {
                rotateAmount = angle;
            }
            transform.Rotate(Vector3.up, rotateAmount);
        }

        Vector3 moveDirection = transform.forward * moveSpeed;

        return ((moveDirection + playerVelocity) * Time.deltaTime, true);
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        CancelInvoke("Explode");

        Debug.LogFormat("'{0}' 추적자 폭발! 위치: {1}", explosionType, transform.position);

        if (controller.enabled)
            controller.enabled = false;

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        if (explosionType == ProjectileType.TerrainDestruction || explosionType == ProjectileType.TerrainCreation)
        {
            StartCoroutine(SlowModifyTerrainEffect());
        }
        else
        {
            HandleInstantEffect();

            if (GameManager.instance != null)
            {
                GameManager.instance.OnProjectileDestroyed();
            }
            Destroy(gameObject);
        }
    }

    private void HandleInstantEffect()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        switch (explosionType)
        {
            case ProjectileType.NormalImpact:
                foreach (Collider hit in colliders)
                {
                    Rigidbody rb = hit.GetComponentInParent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                    }
                }
                break;

            case ProjectileType.TerrainPush:
                foreach (Collider hit in colliders)
                {
                    PlayerMovement player = hit.GetComponentInParent<PlayerMovement>();
                    if (player != null)
                    {
                        Vector3 direction = player.transform.position - transform.position;
                        player.ApplyKnockback(direction, playerKnockbackForce);
                    }
                }
                break;

            case ProjectileType.TerrainPull:
                foreach (Collider hit in colliders)
                {
                    PlayerMovement player = hit.GetComponentInParent<PlayerMovement>();
                    if (player != null)
                    {
                        Vector3 direction = transform.position - player.transform.position;
                        player.ApplyKnockback(direction, playerKnockbackForce);
                    }
                }
                break;
        }
    }

    private IEnumerator SlowModifyTerrainEffect()
    {
        if (terrainEffectSteps <= 0) terrainEffectSteps = 1;
        float durationPerStep = terrainEffectDuration / terrainEffectSteps;

        float totalStrength = (explosionType == ProjectileType.TerrainDestruction) ? -terrainModificationStrength : terrainModificationStrength;
        float strengthPerStep = totalStrength / terrainEffectSteps;

        if (World.Instance == null)
        {
            Debug.LogWarning("지형 효과를 실행하려 했으나 World.Instance를 찾을 수 없습니다.");
            if (GameManager.instance != null)
            {
                GameManager.instance.OnProjectileDestroyed();
            }
            Destroy(gameObject);
            yield break;
        }

        for (int i = 0; i < terrainEffectSteps; i++)
        {
            World.Instance.ModifyTerrain(transform.position, strengthPerStep, explosionRadius);
            yield return new WaitForSeconds(durationPerStep);
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnProjectileDestroyed();
        }
        Destroy(gameObject);
    }

    // ▼▼▼ [수정됨] CS0019 오류 수정 ▼▼▼
    void OnDrawGizmosSelected()
    {
        if (controller == null) return;
        Vector3 cliffRayStart = transform.position + (transform.forward * cliffCheckForwardOffset) + (Vector3.up * raycastHeightOffset);
        Vector3 slopeRayStart = transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(cliffRayStart, cliffRayStart + (Vector3.down * cliffCheckRayLength));
        Gizmos.color = Color.green;

        // Gizmos.DrawLine(slopeRayStart, slopeRayStart + (Vector3.down * slopeRayStart + (Vector3.down * slopeRaycastLength))); // <-- 문제의 코드
        Gizmos.DrawLine(slopeRayStart, slopeRayStart + (Vector3.down * slopeRaycastLength)); // <-- 수정된 코드
    }
    // ▲▲▲ [여기까지 수정] ▲▲▲
}