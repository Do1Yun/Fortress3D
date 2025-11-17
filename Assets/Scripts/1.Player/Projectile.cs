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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
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
        explosionRadius = gameManager.players[gameManager.currentPlayerIndex].ExplosionRange;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet") || hasExploded) return;

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
        // 🔥 수정 핵심: 폭발 이펙트 한 번만 생성 후 자동 삭제
        // ---------------------------------------------
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                explosionPosition,
                Quaternion.identity
            );

            // 파티클 길이를 모르니 3초 기본 삭제 (원하면 조절)
            Destroy(effect, 1f);
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
                    Rigidbody rb = hit.GetComponentInParent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
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
