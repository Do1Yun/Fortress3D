using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windStrength = 5f;
    public float maxRandomAngleChange = 30f;

    // ��ź�� ���� ���� ���ϴ� ���, Trajectory ��ũ��Ʈ�� �� ���� �о���� ���� ����
    // ���� OnTriggerStay�� �ʼ��� �ƴմϴ�. �ٸ�, ���������� ��Ȯ�� �ùķ��̼��� ���Ѵٸ� ����� �� �ֽ��ϴ�.
    /* private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(windDirection * windStrength, ForceMode.Force);
            }
        }
    }
    */

    public void SetRandomWindDirection()
    {
        float randomAngle = Random.Range(-maxRandomAngleChange, maxRandomAngleChange);
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        windDirection = rotation * Vector3.right;

        Debug.Log($"�ٶ� ���� ���� ����: {windDirection.normalized}, ����: {windStrength}");
    }

    public void SetWind(Vector3 direction, float strength)
    {
        this.windDirection = direction.normalized;
        this.windStrength = strength;
        Debug.Log($"�ٶ� ���� ����: ����={windDirection}, ����={windStrength}");
    }
}