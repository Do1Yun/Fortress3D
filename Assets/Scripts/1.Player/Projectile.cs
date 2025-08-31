using UnityEngine;

public enum ProjectileType
{
    NormalImpact,
    TerrainDestruction,
    TerrainCreation
}

public class Projectile : MonoBehaviour
{

    [Header("��ź ���� ����")]
    public ProjectileType type;
    public float lifeTime = 5.0f;
    public float explosionRadius = 5.0f;
    public GameObject explosionEffectPrefab;

    [Header("Ÿ�Ժ� ����")]
    public float terrainModificationStrength = 2.0f;
    public float explosionForce = 500f;

    private bool hasExploded = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdate�� ���� ȿ���� �����ϱ⿡ ���� ���� ���Դϴ�.
    void FixedUpdate()
    {
        // WindController�� �����ϰ�, �� ������Ʈ�� �±װ� "Bullet"�� ���� ���� �����մϴ�.
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
        // ��ź�� �������� �ʰ� ������ ���� ����� ��쿡�� ���� �Ѿ���� ó��
        if (!hasExploded && GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.ProjectileFlying)
        {
            GameManager.instance.OnProjectileDestroyed();
        }
    }

    void Explode(Vector3 explosionPosition)
    {
        hasExploded = true;
        Debug.LogFormat("'{0}' ��ź ����! ��ġ: {1}", type, explosionPosition);

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