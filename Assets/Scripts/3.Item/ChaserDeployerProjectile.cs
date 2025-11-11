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
    private float lifeTime = 15.0f;
    private float rotationSmoothSpeed = 10f;
    private Rigidbody rb;

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
        rb = GetComponent<Rigidbody>();

        // ▼▼▼ [수정됨] Destroy 대신 Invoke 사용 ▼▼▼
        // lifeTime초 후에 'HatchAtCurrentPosition' 함수를 실행하도록 예약합니다.
        Invoke(nameof(HatchAtCurrentPosition), lifeTime);
        // ▲▲▲ [여기까지 수정] ▲▲▲
    }

    void FixedUpdate()
    {
        if (isLanded) return; // [추가됨] 착지 후에는 물리 이동 중지

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

    void OnCollisionEnter(Collision collision)
    {
        if (isLanded) return;
        if (collision.gameObject.GetComponent<PlayerController>() != null) return;

        // ▼▼▼ [수정됨] 부화 로직을 별도 함수로 분리 ▼▼▼

        // 1. (중요) lifeTime 후에 부화하는 Invoke 예약을 취소합니다.
        //    (그렇지 않으면 땅에 부딪혀 부화한 뒤, lifeTime이 다 됐을 때 공중에서 또 부화하려 합니다)
        CancelInvoke(nameof(HatchAtCurrentPosition));

        // 2. 충돌 지점에서 부화 로직 실행
        Vector3 spawnPosition = collision.contacts[0].point;
        DeployChaser(spawnPosition, "충돌 지점");
        // ▲▲▲ [여기까지 수정] ▲▲▲
    }

    // ▼▼▼ [추가됨] lifeTime 만료 시 호출될 함수 ▼▼▼
    /// <summary>
    /// Invoke에 의해 lifeTime초 후에 호출됩니다.
    /// (충돌하지 않고 시간이 다 됐을 때)
    /// </summary>
    void HatchAtCurrentPosition()
    {
        // isLanded는 DeployChaser 내부에서 체크하므로 여기선 필요 없습니다.
        DeployChaser(transform.position, "라이프타임 만료");
    }
    // ▲▲▲ [여기까지 추가] ▲▲▲


    // ▼▼▼ [추가됨] 공통 부화 로직 ▼▼▼
    /// <summary>
    /// 지정된 위치에 Chaser 유닛을 생성하고 이 "알" 오브젝트를 파괴합니다.
    /// </summary>
    /// <param name="spawnPosition">유닛이 생성될 위치</param>
    /// <param name="debugReason">부화 사유 (로그 출력용)</param>
    void DeployChaser(Vector3 spawnPosition, string debugReason)
    {
        // 이미 부화했으면 중복 실행 방지
        if (isLanded) return;
        isLanded = true;

        // (참고) OnCollisionEnter에서 이미 CancelInvoke를 했지만,
        // HatchAtCurrentPosition으로 실행될 경우를 대비해 여기서도 호출해주는 것이 안전합니다.
        CancelInvoke();

        Debug.Log($"추적탄 착지! ({debugReason}) 페이로드를 전개합니다.");

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
                // ▼▼▼ [수정됨] 인스펙터 값이 아닌, 전달받은 타입을 ChasingObject에게 전달 ▼▼▼
                chaserScript.Initialize(payloadTypeToPass);
                // ▲▲▲ [여기까지 수정] ▲▲▲
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

        // "알" 자신을 파괴
        Destroy(gameObject);
    }
    // ▲▲▲ [여기까지 추가] ▲▲▲
}