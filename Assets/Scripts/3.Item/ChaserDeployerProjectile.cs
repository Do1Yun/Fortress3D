using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ChaserDeployerProjectile : MonoBehaviour
{
    [Header("페이로드 설정")]
    [Tooltip("땅에 착지했을 때 생성할 ChasingObject 프리팹")]
    public GameObject chaserUnitPrefab;

    private ProjectileType payloadTypeToPass; // ChasingObject에게 전달할 타입
    private bool isLanded = false;
    private float lifeTime = 5.0f;
    private float rotationSmoothSpeed = 10f;
    private Rigidbody rb;
    [Header("오디오 설정")]
    public AudioClip ChaserStrartCommentary;
    public GameManager gameManager;

    // [추가] 충돌 직전의 속도를 저장하기 위한 변수 (Projectile.cs와 동일)
    private Vector3 lastVelocity;

    /// <summary>
    /// PlayerShooting 스크립트가 이 "알" 포탄을 발사할 때 호출합니다.
    /// </summary>
    public void Initialize(ProjectileType type)
    {
        this.payloadTypeToPass = type;
        Debug.Log($"[DEPLOYER_DEBUG] '알' 포탄이 임무를 받음: {type}");
    }

    void Start()
    {
        gameManager = GameManager.instance;
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        rb = GetComponent<Rigidbody>();
       
        // lifeTime초 후에 'HatchAtCurrentPosition' 함수를 실행하도록 예약합니다.
        Invoke(nameof(HatchAtCurrentPosition), lifeTime);
    }

    void FixedUpdate()
    {
        if (isLanded) return;

        // [추가] 물리 연산 전, 현재 프레임의 속도를 저장 (충돌 시 반사각 계산용)
        if (rb != null)
        {
            lastVelocity = rb.velocity;
        }

        // WindController가 존재하고, 이 오브젝트의 태그가 "Bullet"일 때만 힘을 적용합니다.
        if (WindController.instance != null && gameObject.CompareTag("Bullet"))
        {
            Vector3 windForce = WindController.instance.CurrentWindDirection * WindController.instance.CurrentWindStrength;
            rb.AddForce(windForce, ForceMode.Force);
        }

        // 포탄 머리방향 설정 (Projectile.cs와 동일한 90도 보정 적용)
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rb.velocity.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation * Quaternion.Euler(90f, 0f, 0f), Time.deltaTime * rotationSmoothSpeed);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isLanded) return;
        // 플레이어와 충돌은 무시 (발사자 본인 충돌 방지 등)
        if (collision.gameObject.GetComponent<PlayerController>() != null) return;

        // -------------------------------------------------------
        // [수정] Projectile.cs의 Environment 반사 로직 적용
        // -------------------------------------------------------
        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            // 1. 충돌 지점의 법선(Normal) 벡터
            Vector3 normal = collision.contacts[0].normal;

            // 2. 반사 벡터 계산 (Vector3.Reflect 이용)
            // lastVelocity를 사용하여 물리 엔진 감속 전 속도 기준으로 계산
            Vector3 reflectDir = Vector3.Reflect(lastVelocity, normal);

            // 3. 리지드바디 속도 강제 재설정 (속력 유지)
            rb.velocity = reflectDir.normalized * lastVelocity.magnitude;

            // 4. 회전 즉시 반영 (90도 보정 포함)
            transform.rotation = Quaternion.LookRotation(rb.velocity) * Quaternion.Euler(90f, 0f, 0f);

            // *중요*: Environment에서는 터지지 않고 튕겨 나가므로 함수 종료
            return;
        }

        // Environment가 아닌 다른 물체(몬스터 등)에 부딪혔을 때만 즉시 부화

        // 1. lifeTime 예약 취소
        CancelInvoke(nameof(HatchAtCurrentPosition));

        // 2. 충돌 지점에서 부화 로직 실행
        Vector3 spawnPosition = collision.contacts[0].point;
        DeployChaser(spawnPosition, "충돌 지점");
    }

    /// <summary>
    /// Invoke에 의해 lifeTime초 후에 호출됩니다.
    /// (Environment 사이를 튕기다가 시간이 다 됐을 때)
    /// </summary>
    void HatchAtCurrentPosition()
    {
        DeployChaser(transform.position, "라이프타임 만료");
    }

    /// <summary>
    /// 지정된 위치에 Chaser 유닛을 생성하고 이 "알" 오브젝트를 파괴합니다.
    /// </summary>
    void DeployChaser(Vector3 spawnPosition, string debugReason)
    {
        if (isLanded) return;
        isLanded = true;

        CancelInvoke();

        Debug.Log($"추적탄 착지! ({debugReason}) 페이로드를 전개합니다.");
        if (Random.value <= 0.2f) // 확률 (현재 100%로 설정됨)
        {
            if (gameManager != null && gameManager.announcerAudioSource != null && ChaserStrartCommentary != null)
            {
                // 기존 멘트가 있다면 끊고, 아이템 멘트를 즉시 재생
                gameManager.announcerAudioSource.Stop();
                gameManager.announcerAudioSource.PlayOneShot(ChaserStrartCommentary);

            }
        }
        // Y축 보정
        spawnPosition.y += 0.5f;

        if (chaserUnitPrefab != null)
        {
            GameObject chaserGO = Instantiate(chaserUnitPrefab, spawnPosition, Quaternion.identity);
            if (ProjectileFollowCamera.instance != null)
            {
                ProjectileFollowCamera.instance.SetTarget(chaserGO.transform);
            }
            ChasingObject chaserScript = chaserGO.GetComponent<ChasingObject>();
            if (chaserScript != null)
            {
                chaserScript.Initialize(payloadTypeToPass);
            }
            else
            {
                Debug.LogError("chaserUnitPrefab에 ChasingObject 스크립트가 없습니다!");
                if (GameManager.instance != null) GameManager.instance.OnProjectileDestroyed();
            }
        }
        else
        {
            Debug.LogError("chaserUnitPrefab이 할당되지 않았습니다!");
            if (GameManager.instance != null) GameManager.instance.OnProjectileDestroyed();
        }

        Destroy(gameObject);
    }
}