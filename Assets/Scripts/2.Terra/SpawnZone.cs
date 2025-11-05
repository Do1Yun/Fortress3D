using UnityEngine;

// 이 스크립트가 추가된 게임 오브젝트는 반드시 Collider 컴포넌트를 가져야 함
[RequireComponent(typeof(Collider))]
public class SpawnZone : MonoBehaviour
{
    // 보호 영역을 감지하기 위한 콜라이더
    public Collider zoneCollider { get; private set; }

    void Awake()
    {
        // 스크립트가 시작될 때 자신의 Collider 컴포넌트를 찾아 저장
        zoneCollider = GetComponent<Collider>();

        // 중요: 스폰 지점 콜라이더가 Raycast에 감지되지 않도록 설정
        // (지형 편집 Raycast에 방해가 되지 않도록)
        // 만약 이 콜라이더를 다른 물리적 용도로도 사용한다면 이 줄을 제거하세요.
        zoneCollider.isTrigger = true;
    }

    // 씬(Scene) 뷰에서 영역을 쉽게 볼 수 있도록 기즈모(Gizmo)를 그립니다.
    void OnDrawGizmos()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider>();
        }

        // 기즈모 색상 설정 (반투명한 파란색)
        Gizmos.color = new Color(0.0f, 0.5f, 1.0f, 0.4f);

        // 콜라이더 타입에 따라 적절한 기즈모를 그립니다.
        if (zoneCollider is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (zoneCollider is SphereCollider sphere)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
        // 참고: MeshCollider는 복잡해서 기즈모 그리기를 생략 (필요시 추가 가능)
    }
}