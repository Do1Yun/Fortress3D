// WindZone.cs
using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right; // �ٶ� ����
    public float windStrength = 5f; // ���� ũ��
    public float maxRandomAngleChange = 30f; // ���� �ٶ� ���� ��ȭ �ִ� ����

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Projectile")) // ��ź�� ����
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(windDirection * windStrength, ForceMode.Force);
            }
        }
    }

    // GameManager���� ȣ���� �� �ִ� ���� �ٶ� ���� ���� �޼��� (���� ����)
    public void SetRandomWindDirection()
    {
        // Y�� �������� �����ϰ� ȸ�� (���� �ٶ�)
        float randomAngle = Random.Range(-maxRandomAngleChange, maxRandomAngleChange);
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        windDirection = rotation * Vector3.right; // �ʱ� windDirection�� Vector3.right��� ����
        
        Debug.Log($"�ٶ� ������ �������� ����Ǿ����ϴ�: {windDirection.normalized}, ����: {windStrength}");
    }

    // Ư�� ����� ������ �ٶ��� �����ϴ� �޼��� (���� ����)
    public void SetWind(Vector3 direction, float strength)
    {
        this.windDirection = direction.normalized;
        this.windStrength = strength;
        Debug.Log($"�ٶ��� �����Ǿ����ϴ�: ����={windDirection}, ����={windStrength}");
    }
}