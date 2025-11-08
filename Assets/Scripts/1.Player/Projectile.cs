// Projectile.cs
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
    private float rotationSmoothSpeed = 10f; // <-- 포탄 머리 방향 보간 속도

    [Header("타입별 설정")]
    public float terrainModificationStrength = 2.0f;
    public float explosionForce = 500f;
    public float playerKnockbackForce = 20f; // <-- 플레이어 넉백 전용 변수 추가! (기본값 20)

    private bool hasExploded = false;
    private Rigidbody rb;
    private GameManager gameManager;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdate는 물리 효과를 적용하기에 가장 좋은 곳입니다.
    void FixedUpdate()
    {
        // WindController가 존재하고, 이 오브젝트의 태그가 "Bullet"일 때만 힘을 적용합니다.
        if (WindController.instance != null && gameObject.CompareTag("Bullet"))
        {
            Vector3 windForce = WindController.instance.CurrentWindDirection * WindController.instance.CurrentWindStrength;
            rb.AddForce(windForce, ForceMode.Force);
        }
        // ▼▼▼▼▼▼▼▼▼▼▼▼ 포탄 머리방향 설정
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
        {
            // 목표 회전값 = 현재 속도 방향
            Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation * Quaternion.Euler(90f, 0f, 0f), Time.deltaTime * rotationSmoothSpeed);
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲
    }
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");

        // 포탄이 생성되고 lifeTime초 후에 자동으로 Explode 함수를 호출합니다.
        Invoke("Explode", lifeTime);
    }

    // isTrigger가 아닌 Collider와 충돌했을 때 호출됩니다.
    void OnCollisionEnter(Collision collision)
    {
        // 이미 폭발했다면 아무것도 하지 않습니다.
        if (collision.gameObject.CompareTag("Bullet") || hasExploded) return;

        // 즉시 폭발하고, 이벤트를 전파한 후, 오브젝트를 파괴합니다.
        Explode(collision.contacts[0].point);
    }

    // [수정] Invoke 호출을 위해 매개변수가 없는 Explode 함수를 추가했습니다.
    private void Explode()
    {
        Explode(this.transform.position); // 현재 위치를 폭발 지점으로 사용
    }

    private void Explode(Vector3 explosionPosition)
    {
        if (hasExploded) return;
        hasExploded = true;

        Debug.LogFormat("'{0}' 포탄 폭발! 위치: {1}", type, explosionPosition);

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
        }

        Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);

        switch (type)
        {
            case ProjectileType.NormalImpact:
                // (기존과 동일)
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
                // (기존과 동일)
                if (World.Instance != null)
                {
                    World.Instance.ModifyTerrain(explosionPosition, -terrainModificationStrength, explosionRadius);
                }
                break;

            case ProjectileType.TerrainCreation:
                // (기존과 동일)
                if (World.Instance != null)
                {
                    World.Instance.ModifyTerrain(explosionPosition, terrainModificationStrength, explosionRadius);
                }
                break;

            // ▼▼▼ [수정된 부분] ▼▼▼
            case ProjectileType.TerrainPush:
                // 1. 지형 파괴 로직 제거!
                // 2. 주변 플레이어 밀어내기
                foreach (Collider hit in colliders)
                {
                    PlayerMovement player = hit.GetComponentInParent<PlayerMovement>();
                    if (player != null)
                    {
                        Vector3 direction = player.transform.position - explosionPosition;
                        // 새로 만든 playerKnockbackForce 변수 사용
                        player.ApplyKnockback(direction, playerKnockbackForce);
                    }
                }
                break;

            case ProjectileType.TerrainPull:
                // 1. 지형 생성 로직 제거!
                // 2. 주변 플레이어 끌어당기기
                foreach (Collider hit in colliders)
                {
                    PlayerMovement player = hit.GetComponentInParent<PlayerMovement>();
                    if (player != null)
                    {
                        Vector3 direction = explosionPosition - player.transform.position;
                        // 새로 만든 playerKnockbackForce 변수 사용
                        player.ApplyKnockback(direction, playerKnockbackForce);
                    }
                }
                break;
                // ▲▲▲ [여기까지 수정] ▲▲▲
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnProjectileDestroyed();
        }
        Destroy(gameObject);
    }
}