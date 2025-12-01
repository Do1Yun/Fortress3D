using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
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

    // --- [수정] 오디오 관련 설정 ---
    [Header("오디오 설정")]
    public AudioClip movementClip;

    [Range(0f, 1f)]
    public float volume = 1.0f; // [추가] 기본 볼륨 조절 슬라이더

    [Tooltip("이 거리 안에서는 소리가 최대 크기로 들립니다.")]
    public float minDistance = 5.0f; // [추가] 중요! 기본값 1은 너무 작음 -> 5로 증가

    [Tooltip("이 거리 밖에서는 소리가 들리지 않거나 작게 들립니다.")]
    public float maxDistance = 50.0f; // [추가] 소리 전달 최대 거리

    [Tooltip("기본 피치 값 (1.0 = 원래 소리)")]
    public float basePitch = 1.0f;
    [Tooltip("크기가 커질수록 피치를 얼마나 낮출지 (값이 클수록 더 낮아짐)")]
    public float pitchWeight = 0.5f;

    private AudioSource audioSource;
    // ----------------------------

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

        // [추가] 오디오 소스 초기화
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.spatialBlend = 1.0f; // 3D 사운드

        // [수정] 3D 사운드 감쇠 설정 적용
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // 거리에 따라 자연스럽게 줄어듦
        audioSource.minDistance = minDistance; // 이 거리까지는 소리가 100%로 들림
        audioSource.maxDistance = maxDistance; // 이 거리 밖은 소리가 아주 작아짐
        audioSource.volume = volume;           // 설정한 볼륨 적용

        audioSource.playOnAwake = false;

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

        // --- 크기에 따른 피치 조절 및 재생 시작 ---
        if (audioSource != null && movementClip != null)
        {
            audioSource.clip = movementClip;

            // 피치 계산
            float calculatedPitch = basePitch / Mathf.Max(0.5f, transform.localScale.x * pitchWeight);
            audioSource.pitch = Mathf.Clamp(calculatedPitch, 0.3f, 3.0f);

            // [추가] 덩치가 커지면 볼륨도 살짝 더 크게 들리게 하고 싶다면 아래 주석 해제
            // audioSource.volume = volume * (1 + (transform.localScale.x * 0.2f)); 

            audioSource.Play();
        }
        // ----------------------------------------------

        float stopDist = (type == ProjectileType.TerrainPull) ? explosionRadius : detonationDistance;
        Debug.Log($"[CHASER_DEBUG] 추적자 활성화! 임무: {type}. 정지 거리: {stopDist}m");

        Invoke("Explode", selfDestructTime);
    }

    public void Initialize(ProjectileType type)
    {
        Initialize(type, this.explosionRadius);
        Debug.LogWarning($"[CHASER_DEBUG] 구형 Initialize(type)가 호출되었습니다.");
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

    // ... (FindClosestPlayer, ApplyGravityAndSlope, CheckForCliffs, HandleAvoidance, HandleChasing 등은 기존과 동일) ...
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

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

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

            Destroy(effect, 1f);

            if (explosionEffectPrefab.transform.childCount > 1) explosionEffectPrefab.transform.localScale /= scale;
            else explosionEffectPrefab.transform.GetChild(0).localScale /= scale;
            transform.localScale /= scale;
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

    // ... (HandleInstantEffect, SlowModifyTerrainEffect, OnDrawGizmosSelected 동일) ...
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
}