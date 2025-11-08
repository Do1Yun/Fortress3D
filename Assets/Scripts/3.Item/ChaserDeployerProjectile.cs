using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ChaserDeployerProjectile : MonoBehaviour
{
    [Header("페이로드 설정")]
    [Tooltip("땅에 착지했을 때 생성할 ChasingObject 프리팹")]
    public GameObject chaserUnitPrefab;

    // ▼▼▼ [수정됨] 인스펙터에서 설정하는 대신, 코드로 타입을 전달받음 ▼▼▼
    private ProjectileType payloadTypeToPass; // ChasingObject에게 전달할 타입
    // ▲▲▲ [여기까지 수정] ▲▲▲

    private bool isLanded = false;
    private float lifeTime = 15.0f;

    // ▼▼▼ [추가됨] PlayerShooting이 이 함수를 호출하여 탄 타입을 주입 ▼▼▼
    /// <summary>
    /// PlayerShooting 스크립트가 이 "알" 포탄을 발사할 때 호출합니다.
    /// </summary>
    public void Initialize(ProjectileType type)
    {
        this.payloadTypeToPass = type;
        Debug.Log($"[DEPLOYER_DEBUG] '알' 포탄이 임무를 받음: {type}");
    }
    // ▲▲▲ [여기까지 추가] ▲▲▲

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isLanded) return;
        if (collision.gameObject.GetComponent<PlayerController>() != null) return;

        isLanded = true;
        CancelInvoke();

        Debug.Log("추적탄 착지! 페이로드를 전개합니다.");

        Vector3 spawnPosition = collision.contacts[0].point;
        spawnPosition.y += 0.5f;

        if (chaserUnitPrefab != null)
        {
            GameObject chaserGO = Instantiate(chaserUnitPrefab, spawnPosition, Quaternion.identity);

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

        Destroy(gameObject);
    }
}