using UnityEngine;

public class Projectile : MonoBehaviour
{
    // �ν����� â���� ��ź�� ���� �ð��� ������ �� �ֽ��ϴ�.
    public float lifeTime = 5.0f;

    // ������Ʈ�� ������ ��(Instantiate) �� ���� ȣ��Ǵ� �Լ��Դϴ�.
    void Start()
    {
        // lifeTime ������ ������ �ð�(��)�� ������ �� ���� ������Ʈ�� �ı��մϴ�.
        Destroy(gameObject, lifeTime);
    }
//�⺻�����δٰ� �浹 ������ ������� �Ѿ���.
//���� ����ٰ� �ػ��̰� �������� �� ���� �Լ��� ȣ���ϵ��� ������
}