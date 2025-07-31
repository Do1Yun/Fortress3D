using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("발사체 설정")]
    public float lifeTime = 5.0f; // 포탄의 생존 시간
    public float explosionRadius = 5.0f; // 폭발 반경 (지형 파괴 및 데미지 반경)
    public float terrainModificationStrength = 1.0f; // 지형 수정 강도 (음수면 파괴)
    public GameObject explosionEffectPrefab; // 폭발 시 생성될 파티클/이펙트 프리팹 (선택 사항)

    private bool hasExploded = false; // 중복 폭발 방지 플래그

    void Start()
    {
        // lifeTime 변수에 지정된 시간(초)이 지나면 이 게임 오브젝트를 파괴합니다.
        Destroy(gameObject, lifeTime);
    }

    // 포탄이 다른 콜라이더와 물리적으로 충돌했을 때 호출됩니다.
    void OnCollisionEnter(Collision collision)
    {
        // 이미 폭발 처리되었으면 중복 호출 방지
        if (hasExploded) return;

        // 충돌한 지점을 폭발 위치로 사용합니다.
        // collision.contacts는 충돌 지점들의 배열이며, 첫 번째 지점을 사용합니다.
        Vector3 explosionPosition = collision.contacts[0].point;

        // 1. 지형과 충돌했거나, 파괴 가능한 오브젝트(예: 다른 플레이어)와 충돌했는지 확인
        // "Terrain" 태그는 지형 오브젝트에 할당되어야 합니다.
        // "Player" 태그는 플레이어 오브젝트에 할당되어야 합니다.
        // 필요하다면 "Destructible" 등 추가 태그를 사용할 수 있습니다.
        if (collision.gameObject.CompareTag("Terrain") || collision.gameObject.CompareTag("Player"))
        {
            Explode(explosionPosition);
        } 
        else // 지형이나 플레이어가 아닌 다른 오브젝트와 충돌해도 폭발 처리
        {
            Explode(explosionPosition);
        }
    }

    // 포탄이 수명이 다해 파괴될 때 또는 외부에서 Destroy될 때 호출됩니다.
    void OnDestroy()
    {
        // 포탄이 아직 폭발 처리되지 않았고, 게임이 진행 중이며,
        // GameManager 인스턴스가 존재할 경우에만 턴 종료 신호를 보냅니다.
        // 이는 포탄이 맵 밖으로 날아가거나, 수명이 다해서 사라지는 경우에 턴이 넘어가도록 하기 위함입니다.
        if (!hasExploded && GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.ProjectileFlying)
        {
            Debug.Log("발사체 수명 만료 또는 외부 파괴. 턴 전환을 요청합니다.");
            GameManager.instance.OnProjectileDestroyed();
        }
    }

    // 실제 폭발 및 지형 변형, 턴 전환 로직을 처리하는 함수
    void Explode(Vector3 explosionPosition)
    {
        // 중복 폭발 방지 플래그 설정
        hasExploded = true;
        Debug.LogFormat("포탄 폭발! 위치: {0}", explosionPosition);

        // 1. 지형 파괴/변형 로직 호출
        if (World.Instance != null)
        {
            // terrainModificationStrength 값은 양수일 경우 지형을 솟아오르게, 음수일 경우 파괴합니다.
            // 여기서는 파괴를 위해 음수로 전달합니다.
            World.Instance.ModifyTerrain(explosionPosition, -terrainModificationStrength, explosionRadius);
        }
        else
        {
            Debug.LogWarning("World.Instance를 찾을 수 없습니다. 지형 변형이 작동하지 않습니다.");
        }

        // 2. 폭발 시각 효과 (파티클, 사운드 등) 생성
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
            // TODO: 폭발음 재생 등 추가
        }

        // 3. 주변 오브젝트(플레이어 등)에 물리적인 힘 가하기 (선택 사항, 포트리스는 주로 지형 파괴)
        // Physics.OverlapSphere를 사용하여 폭발 반경 내의 모든 콜라이더를 찾을 수 있습니다.
        // Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius);
        // foreach (Collider hitCollider in colliders)
        // {
        //     // 플레이어에게 데미지를 주거나 물리적인 힘을 가하는 로직
        //     // 예: Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
        //     // if (rb != null) rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
        //     // PlayerController player = hitCollider.GetComponent<PlayerController>();
        //     // if (player != null) player.TakeDamage(damageAmount);
        // }

        // 4. GameManager에게 포탄이 파괴되었음을 알리고 턴 종료를 요청
        if (GameManager.instance != null)
        {
            GameManager.instance.OnProjectileDestroyed();
        }
        else
        {
            Debug.LogWarning("GameManager.instance를 찾을 수 없습니다. 턴 전환이 작동하지 않습니다.");
        }

        // 5. 자신(포탄)을 파괴하여 씬에서 제거합니다.
        Destroy(gameObject);
    }
}