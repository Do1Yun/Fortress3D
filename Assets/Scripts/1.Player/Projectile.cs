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

    [Header("��ź ���� ����")]
    public ProjectileType type;
    public float lifeTime = 5.0f;
    public float explosionRadius;
    public GameObject explosionEffectPrefab;

    [Header("Ÿ�Ժ� ����")]
    public float terrainModificationStrength = 2.0f;
    public float explosionForce = 500f;
    public float playerKnockbackForce = 20f; // <-- �÷��̾� �˹� ���� ���� �߰�! (�⺻�� 20)

    private bool hasExploded = false;
    private Rigidbody rb;
    private GameManager gameManager;

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
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager�� ã�� �� �����ϴ�!");

        // ��ź�� �����ǰ� lifeTime�� �Ŀ� �ڵ����� Explode �Լ��� ȣ���մϴ�.
        Invoke("Explode", lifeTime);
    }

    // isTrigger�� �ƴ� Collider�� �浹���� �� ȣ��˴ϴ�.
    void OnCollisionEnter(Collision collision)
    {
        // �̹� �����ߴٸ� �ƹ��͵� ���� �ʽ��ϴ�.
        if (hasExploded) return;

        // ��� �����ϰ�, �̺�Ʈ�� ������ ��, ������Ʈ�� �ı��մϴ�.
        Explode(collision.contacts[0].point);
    }

    // [����] Invoke ȣ���� ���� �Ű������� ���� Explode �Լ��� �߰��߽��ϴ�.
    private void Explode()
    {
        Explode(this.transform.position); // ���� ��ġ�� ���� �������� ���
    }

    private void Explode(Vector3 explosionPosition)
    {
        if (hasExploded) return;
        hasExploded = true;

        Debug.LogFormat("'{0}' ��ź ����! ��ġ: {1}", type, explosionPosition);

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
        }

        Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);

        switch (type)
        {
            case ProjectileType.NormalImpact:
                // (������ ����)
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
                // (������ ����)
                if (World.Instance != null)
                {
                    World.Instance.ModifyTerrain(explosionPosition, -terrainModificationStrength, explosionRadius);
                }
                break;

            case ProjectileType.TerrainCreation:
                // (������ ����)
                if (World.Instance != null)
                {
                    World.Instance.ModifyTerrain(explosionPosition, terrainModificationStrength, explosionRadius);
                }
                break;

            // ���� [������ �κ�] ����
            case ProjectileType.TerrainPush:
                // 1. ���� �ı� ���� ����!
                // 2. �ֺ� �÷��̾� �о��
                foreach (Collider hit in colliders)
                {
                    PlayerMovement player = hit.GetComponentInParent<PlayerMovement>();
                    if (player != null)
                    {
                        Vector3 direction = player.transform.position - explosionPosition;
                        // ���� ���� playerKnockbackForce ���� ���
                        player.ApplyKnockback(direction, playerKnockbackForce);
                    }
                }
                break;

            case ProjectileType.TerrainPull:
                // 1. ���� ���� ���� ����!
                // 2. �ֺ� �÷��̾� �������
                foreach (Collider hit in colliders)
                {
                    PlayerMovement player = hit.GetComponentInParent<PlayerMovement>();
                    if (player != null)
                    {
                        Vector3 direction = explosionPosition - player.transform.position;
                        // ���� ���� playerKnockbackForce ���� ���
                        player.ApplyKnockback(direction, playerKnockbackForce);
                    }
                }
                break;
                // ���� [������� ����] ����
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnProjectileDestroyed();
        }
        Destroy(gameObject);
    }
}