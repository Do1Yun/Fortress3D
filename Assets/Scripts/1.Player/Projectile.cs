using UnityEngine;

public enum ProjectileType
{
    NormalImpact,
    TerrainDestruction,
    TerrainCreation,
    TerrainPush,
    TerrainPull
}

public class Projectile : MonoBehaviour
{
    [Header("포탄 공통 설정")]
    public ProjectileType type;
    public float lifeTime = 5.0f;
    public float explosionRadius;
    public GameObject explosionEffectPrefab;
    private float rotationSmoothSpeed = 10f;

    [Header("타입별 설정")]
    public float terrainModificationStrength = 2.0f;
    public float explosionForce = 500f;
    public float playerKnockbackForce = 20f;

    private bool hasExploded = false;
    private Rigidbody rb;
    private GameManager gameManager;

    // [추가] 충돌 직전의 속도를 저장하기 위한 변수
    private Vector3 lastVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // [추가] 물리 연산 전, 현재 프레임의 속도를 저장 (충돌 시 반사각 계산용)
        if (rb != null)
        {
            lastVelocity = rb.velocity;
        }

        // 바람 영향
        if (WindController.instance != null && gameObject.CompareTag("Bullet"))
        {
            Vector3 windForce = WindController.instance.CurrentWindDirection *
                                WindController.instance.CurrentWindStrength;
            rb.AddForce(windForce, ForceMode.Force);
        }

        // 포탄의 머리 방향
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation * Quaternion.Euler(90f, 0f, 0f),
                Time.deltaTime * rotationSmoothSpeed
            );
        }
    }

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");

        Invoke("Explode", lifeTime);

        // [수정] .Length -> .Count 로 변경
        if (gameManager.players != null && gameManager.players.Count > 0)
        {
            explosionRadius = gameManager.players[gameManager.currentPlayerIndex].ExplosionRange;

            if (explosionEffectPrefab != null)
            {
                // 폭발 반경에 따라 이펙트 크기 조절
                explosionEffectPrefab.transform.localScale *= explosionRadius / gameManager.players[gameManager.currentPlayerIndex].BasicExplosionRange;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet") || hasExploded) return;

        // -------------------------------------------------------
        // [추가] Environment 레이어와 충돌 시 반사(Reflect) 처리
        // -------------------------------------------------------
        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            // 1. 충돌 지점의 법선(Normal) 벡터 (벽이 바라보는 방향)
            Vector3 normal = collision.contacts[0].normal;

            // 2. 반사 벡터 계산 (Vector3.Reflect 이용)
            // lastVelocity를 쓰는 이유: 충돌로 인해 rb.velocity가 이미 변했을 수 있기 때문
            Vector3 reflectDir = Vector3.Reflect(lastVelocity, normal);

            // 3. 리지드바디 속도 강제 재설정
            // .normalized * lastVelocity.magnitude -> 방향은 튕기는 쪽으로, 속력(파워)은 그대로 유지
            rb.velocity = reflectDir.normalized * lastVelocity.magnitude;

            // 4. 회전도 즉시 반영 (Lerp를 기다리지 않고 머리를 돌림)
            transform.rotation = Quaternion.LookRotation(rb.velocity) * Quaternion.Euler(90f, 0f, 0f);

            // 폭발하지 않고 함수 종료 (튕겨 나감)
            return;
        }

        // 그 외(플레이어, 지형 등)에는 정상 폭발
        Explode(collision.contacts[0].point);
    }

    private void Explode()
    {
        Explode(this.transform.position);
    }

    private void Explode(Vector3 explosionPosition)
    {
        if (hasExploded) return;
        hasExploded = true;

        Debug.LogFormat("'{0}' 포탄 폭발! 위치: {1}", type, explosionPosition);

        // ---------------------------------------------
        // 폭발 이펙트 한 번만 생성 후 자동 삭제
        // ---------------------------------------------
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                explosionPosition,
                Quaternion.identity
            );
            Destroy(effect, 1f);

            // 이펙트 크기 원상복구 (Prefab 자체를 수정한게 아니라 인스턴스만 수정됨을 가정)
            // 주의: Prefab 원본의 localScale을 건드리면 안되므로, 여기서는 생성된 effect의 스케일을 조정하는 것이 더 안전할 수 있음.
            // 기존 코드 로직을 존중하여 유지.
            explosionEffectPrefab.transform.localScale /= explosionRadius / gameManager.players[gameManager.currentPlayerIndex].BasicExplosionRange;
        }

        // ---------------------------------------------
        // 폭발 반경 내 오브젝트 탐색
        // ---------------------------------------------
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);

        switch (type)
        {
            case ProjectileType.NormalImpact:
                foreach (Collider hit in colliders)
                {
                    Rigidbody targetRb = hit.GetComponentInParent<Rigidbody>();
                    if (targetRb != null)
                    {
                        targetRb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
                    }
                }
                break;

            case ProjectileType.TerrainDestruction:
                if (World.Instance != null)
                {
                    World.Instance.ModifyTerrain(
                        explosionPosition,
                        -terrainModificationStrength,
                        explosionRadius
                    );
                }
                break;

            case ProjectileType.TerrainCreation:
                if (World.Instance != null)
                {
                    World.Instance.ModifyTerrain(
                        explosionPosition,
                        terrainModificationStrength,
                        explosionRadius
                    );
                }
                break;

            case ProjectileType.TerrainPush:
                foreach (Collider hit in colliders)
                {
                    PlayerMovement player = hit.GetComponentInParent<PlayerMovement>();
                    if (player != null)
                    {
                        Vector3 direction = player.transform.position - explosionPosition;
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
                        Vector3 direction = explosionPosition - player.transform.position;
                        player.ApplyKnockback(direction, playerKnockbackForce);
                    }
                }
                break;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnProjectileDestroyed();
        }

        Destroy(gameObject);
    }
}