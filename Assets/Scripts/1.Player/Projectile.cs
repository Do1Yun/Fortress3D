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
    public float pushPullRangeMultiplier = 1.5f;
    [Header("오디오 설정")]
    [Tooltip("착탄(폭발) 시 재생할 효과음")]

    public AudioClip explosionSound;
    [Range(0f, 1f)]
    public float explosionVolume = 1.0f;

    public AudioClip TerrainPushCommentary;     // 두 번째 멘트 파일
    public AudioClip TerrainPullCommentary;

    private bool hasExploded = false;
    private Rigidbody rb;
    private GameManager gameManager;

    private Vector3 lastVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            lastVelocity = rb.velocity;
        }

        if (WindController.instance != null && gameObject.CompareTag("Bullet"))
        {
            Vector3 windForce = WindController.instance.CurrentWindDirection *
                                WindController.instance.CurrentWindStrength;
            rb.AddForce(windForce, ForceMode.Force);
        }

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

        if (gameManager.players != null && gameManager.players.Count > 0)
        {
            explosionRadius = gameManager.players[gameManager.currentPlayerIndex].ExplosionRange;
            if (type == ProjectileType.TerrainPush || type == ProjectileType.TerrainPull)
            {
                explosionRadius *= pushPullRangeMultiplier;
            }
            if (explosionEffectPrefab != null)
            {
                if (explosionEffectPrefab.transform.childCount > 1) explosionEffectPrefab.transform.localScale *= explosionRadius / gameManager.players[gameManager.currentPlayerIndex].BasicExplosionRange;
                else explosionEffectPrefab.transform.GetChild(0).localScale *= explosionRadius / gameManager.players[gameManager.currentPlayerIndex].BasicExplosionRange;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet") || hasExploded) return;

        // Environment 레이어 반사 로직 (기존 유지)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            Vector3 normal = collision.contacts[0].normal;
            Vector3 reflectDir = Vector3.Reflect(lastVelocity, normal);
            rb.velocity = reflectDir.normalized * lastVelocity.magnitude;
            transform.rotation = Quaternion.LookRotation(rb.velocity) * Quaternion.Euler(90f, 0f, 0f);
            return;
        }

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

        if (gameManager != null && gameManager.sfxAudioSource != null && explosionSound != null)
        {
            gameManager.sfxAudioSource.PlayOneShot(explosionSound, explosionVolume);
        }

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                explosionPosition,
                Quaternion.identity
            );
            Destroy(effect, 1f);

            if (explosionEffectPrefab.transform.childCount > 1) explosionEffectPrefab.transform.localScale /= explosionRadius / gameManager.players[gameManager.currentPlayerIndex].BasicExplosionRange;
            else explosionEffectPrefab.transform.GetChild(0).localScale /= explosionRadius / gameManager.players[gameManager.currentPlayerIndex].BasicExplosionRange;
        }

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
                        if (Random.value <= 0.33f)
                        {
                            if (gameManager != null && gameManager.announcerAudioSource != null && TerrainPushCommentary != null)
                            {
                                gameManager.announcerAudioSource.Stop();
                                gameManager.announcerAudioSource.PlayOneShot(TerrainPushCommentary);
                            }
                        }
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
                        if (Random.value <= 0.33f)
                        {
                            if (gameManager != null && gameManager.announcerAudioSource != null && TerrainPullCommentary != null)
                            {
                                gameManager.announcerAudioSource.Stop();
                                gameManager.announcerAudioSource.PlayOneShot(TerrainPullCommentary);
                            }
                        }
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