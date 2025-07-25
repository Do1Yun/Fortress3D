using UnityEngine;

public class Projectile : MonoBehaviour
{
    // 인스펙터 창에서 포탄의 생존 시간을 설정할 수 있습니다.
    public float lifeTime = 5.0f;

    // 오브젝트가 생성될 때(Instantiate) 한 번만 호출되는 함수입니다.
    void Start()
    {
        // lifeTime 변수에 지정된 시간(초)이 지나면 이 게임 오브젝트를 파괴합니다.
        Destroy(gameObject, lifeTime);
    }
//기본적으로다가 충돌 없으면 사라지는 총알임.
//이제 여기다가 준상이가 지형삭제 및 생성 함수를 호출하도록 만들면됨
}