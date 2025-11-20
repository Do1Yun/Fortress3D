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
    [Tooltip("기본 폭발 반지름. PlayerController의 아이템 효과에 의해 덮어쓰일 수 있습니다.")]
    public float explosionRadius = 5.0f;
    private GameObject explosionEffectPrefab;
    public GameObject explosionEffectPrefab1;
    public GameObject explosionEffectPrefab2;
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
    private float scale;

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

    /// <summary>
    /// ChaserDeployerProjectile(알)에 의해 호출됩니다.
    /// </summary>
    public void Initialize(ProjectileType type, float newRadius)
    {
        this.explosionType = type;
        this.isActivated = true;
        this.explosionRadius = newRadius;

        // 수정 - 1120 by lee
        explosionEffectPrefab = (type == ProjectileType.TerrainPull || type == ProjectileType.TerrainPush) ? explosionEffectPrefab1 : explosionEffectPrefab2;
        explosionRadius = gameManager.players[gameManager.currentPlayerIndex].ExplosionRange;
        scale = explosionRadius / gameManager.players[gameManager.currentPlayerIndex].BasicExplosionRange;
        if (explosionEffectPrefab.transform.childCount > 1) explosionEffectPrefab.transform.localScale *= scale;
        else explosionEffectPrefab.transform.GetChild(0).localScale *= scale;
        transform.localScale *= scale;
        // 수정 - 1120 by lee

        float stopDist = (type == ProjectileType.TerrainPull) ? explosionRadius : detonationDistance;
        Debug.Log($"[CHASER_DEBUG] 추적자 활성화! 임무: {type}. 정지 거리: {stopDist}m (Radius: {this.explosionRadius}m / Detonation: {detonationDistance}m)");

        Invoke("Explode", selfDestructTime);
    }

    /// <summary>
    /// (호환성 유지용) 반지름 값 없이 호출될 경우, 
    /// 이 스크립트에 설정된 기본 explosionRadius 값을 사용합니다.
    /// </summary>
    public void Initialize(ProjectileType type)
    {
        Initialize(type, this.explosionRadius);
        Debug.LogWarning($"[CHASER_DEBUG] 구형 Initialize(type)가 호출되었습니다. ChaserDeployerProjectile이 newRadius를 전달하도록 수정해야 합니다.");
    }

    void Update()
    {
        explosionRadius = gameManager.players[gameManager.currentPlayerIndex].ExplosionRange;
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
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                this.transform.position,
                Quaternion.identity
            );

            // 파티클 길이를 모르니 3초 기본 삭제 (원하면 조절)
            Destroy(effect, 1f);
            // 수정 - 1120 by lee
            if (explosionEffectPrefab.transform.childCount > 1) explosionEffectPrefab.transform.localScale /= scale;
            else explosionEffectPrefab.transform.GetChild(0).localScale /= scale;
            transform.localScale /= scale;
            // 수정 - 1120 by lee
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

    // ▼▼▼ [수정됨] HandleInstantEffect에서 잘못된 코드를 모두 제거하고 정리 ▼▼▼
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
                // ▲▲▲ [수정됨] TerrainPull 케이스가 여기서 깔끔하게 끝납니다.
        }
    }
    // ▲▲▲ [여기까지 수정] ▲▲▲

    // ▼▼▼ [수정됨] 잘못된 위치에서 분리되어 별도 코루틴으로 복원 ▼▼▼
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
    // ▲▲▲ [여기까지 수정] ▲▲▲

    // ▼▼▼ [수정됨] 잘못된 위치에서 분리되어 클래스 레벨로 복원 ▼▼▼
    void OnDrawGizmosSelected()
    {
        if (controller == null) return;
        Vector3 cliffRayStart = transform.position + (transform.forward * cliffCheckForwardOffset) + (Vector3.up * raycastHeightOffset);
        Vector3 slopeRayStart = transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(cliffRayStart, cliffRayStart + (Vector3.down * cliffCheckRayLength));
        Gizmos.color = Color.green;

        Gizmos.DrawLine(slopeRayStart, slopeRayStart + (Vector3.down * slopeRaycastLength));
    }
    // ▲▲▲ [여기까지 수정] ▲▲▲

} // <- 클래스를 닫는 마지막 중괄호