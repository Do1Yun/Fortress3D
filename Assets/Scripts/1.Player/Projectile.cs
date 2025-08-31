using UnityEngine;

public enum ProjectileType
{
    NormalImpact,
    TerrainDestruction,
    TerrainCreation
}

public class Projectile : MonoBehaviour
{

    [Header("포탄 공통 설정")]
    public ProjectileType type;
    public float lifeTime = 5.0f;
    public float explosionRadius = 5.0f;
    public GameObject explosionEffectPrefab;

    [Header("타입별 설정")]
    public float terrainModificationStrength = 2.0f;
    public float explosionForce = 500f;

    private bool hasExploded = false;
    private Rigidbody rb;

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
    }
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        Vector3 explosionPosition = collision.contacts[0].point;
        Explode(explosionPosition);
    }

    void OnDestroy()
    {
        // 포탄이 폭발하지 않고 수명이 다해 사라진 경우에도 턴이 넘어가도록 처리
        if (!hasExploded && GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.ProjectileFlying)
        {
            GameManager.instance.OnProjectileDestroyed();
        }
    }

    void Explode(Vector3 explosionPosition)
    {
        hasExploded = true;
        Debug.LogFormat("'{0}' 포탄 폭발! 위치: {1}", type, explosionPosition);

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
        }

        switch (type)
        {
            case ProjectileType.NormalImpact:
                Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);
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
                    World.Instance.ModifyTerrain(explosionPosition, -terrainModificationStrength, explosionRadius);
                }
                break;

            case ProjectileType.TerrainCreation:
                if (World.Instance != null)
                {
                    World.Instance.ModifyTerrain(explosionPosition, terrainModificationStrength, explosionRadius);
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