using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("�߻�ü ����")]
    public float lifeTime = 5.0f; // ��ź�� ���� �ð�
    public float explosionRadius = 5.0f; // ���� �ݰ� (���� �ı� �� ������ �ݰ�)
    public float terrainModificationStrength = 1.0f; // ���� ���� ���� (������ �ı�)
    public GameObject explosionEffectPrefab; // ���� �� ������ ��ƼŬ/����Ʈ ������ (���� ����)

    private bool hasExploded = false; // �ߺ� ���� ���� �÷���

    void Start()
    {
        // lifeTime ������ ������ �ð�(��)�� ������ �� ���� ������Ʈ�� �ı��մϴ�.
        Destroy(gameObject, lifeTime);
    }

    // ��ź�� �ٸ� �ݶ��̴��� ���������� �浹���� �� ȣ��˴ϴ�.
    void OnCollisionEnter(Collision collision)
    {
        // �̹� ���� ó���Ǿ����� �ߺ� ȣ�� ����
        if (hasExploded) return;

        // �浹�� ������ ���� ��ġ�� ����մϴ�.
        // collision.contacts�� �浹 �������� �迭�̸�, ù ��° ������ ����մϴ�.
        Vector3 explosionPosition = collision.contacts[0].point;

        // 1. ������ �浹�߰ų�, �ı� ������ ������Ʈ(��: �ٸ� �÷��̾�)�� �浹�ߴ��� Ȯ��
        // "Terrain" �±״� ���� ������Ʈ�� �Ҵ�Ǿ�� �մϴ�.
        // "Player" �±״� �÷��̾� ������Ʈ�� �Ҵ�Ǿ�� �մϴ�.
        // �ʿ��ϴٸ� "Destructible" �� �߰� �±׸� ����� �� �ֽ��ϴ�.
        if (collision.gameObject.CompareTag("Terrain") || collision.gameObject.CompareTag("Player"))
        {
            Explode(explosionPosition);
        } 
        else // �����̳� �÷��̾ �ƴ� �ٸ� ������Ʈ�� �浹�ص� ���� ó��
        {
            Explode(explosionPosition);
        }
    }

    // ��ź�� ������ ���� �ı��� �� �Ǵ� �ܺο��� Destroy�� �� ȣ��˴ϴ�.
    void OnDestroy()
    {
        // ��ź�� ���� ���� ó������ �ʾҰ�, ������ ���� ���̸�,
        // GameManager �ν��Ͻ��� ������ ��쿡�� �� ���� ��ȣ�� �����ϴ�.
        // �̴� ��ź�� �� ������ ���ư��ų�, ������ ���ؼ� ������� ��쿡 ���� �Ѿ���� �ϱ� �����Դϴ�.
        if (!hasExploded && GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.ProjectileFlying)
        {
            Debug.Log("�߻�ü ���� ���� �Ǵ� �ܺ� �ı�. �� ��ȯ�� ��û�մϴ�.");
            GameManager.instance.OnProjectileDestroyed();
        }
    }

    // ���� ���� �� ���� ����, �� ��ȯ ������ ó���ϴ� �Լ�
    void Explode(Vector3 explosionPosition)
    {
        // �ߺ� ���� ���� �÷��� ����
        hasExploded = true;
        Debug.LogFormat("��ź ����! ��ġ: {0}", explosionPosition);

        // 1. ���� �ı�/���� ���� ȣ��
        if (World.Instance != null)
        {
            // terrainModificationStrength ���� ����� ��� ������ �ھƿ�����, ������ ��� �ı��մϴ�.
            // ���⼭�� �ı��� ���� ������ �����մϴ�.
            World.Instance.ModifyTerrain(explosionPosition, -terrainModificationStrength, explosionRadius);
        }
        else
        {
            Debug.LogWarning("World.Instance�� ã�� �� �����ϴ�. ���� ������ �۵����� �ʽ��ϴ�.");
        }

        // 2. ���� �ð� ȿ�� (��ƼŬ, ���� ��) ����
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
            // TODO: ������ ��� �� �߰�
        }

        // 3. �ֺ� ������Ʈ(�÷��̾� ��)�� �������� �� ���ϱ� (���� ����, ��Ʈ������ �ַ� ���� �ı�)
        // Physics.OverlapSphere�� ����Ͽ� ���� �ݰ� ���� ��� �ݶ��̴��� ã�� �� �ֽ��ϴ�.
        // Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);
        // foreach (Collider hitCollider in colliders)
        // {
        //     // �÷��̾�� �������� �ְų� �������� ���� ���ϴ� ����
        //     // ��: Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
        //     // if (rb != null) rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
        //     // PlayerController player = hitCollider.GetComponent<PlayerController>();
        //     // if (player != null) player.TakeDamage(damageAmount);
        // }

        // 4. GameManager���� ��ź�� �ı��Ǿ����� �˸��� �� ���Ḧ ��û
        if (GameManager.instance != null)
        {
            GameManager.instance.OnProjectileDestroyed();
        }
        else
        {
            Debug.LogWarning("GameManager.instance�� ã�� �� �����ϴ�. �� ��ȯ�� �۵����� �ʽ��ϴ�.");
        }

        // 5. �ڽ�(��ź)�� �ı��Ͽ� ������ �����մϴ�.
        Destroy(gameObject);
    }
}