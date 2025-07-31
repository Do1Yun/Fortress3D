using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windStrength = 5f;
    public float maxRandomAngleChange = 30f;

    // 포탄에 직접 힘을 가하는 대신, Trajectory 스크립트가 이 값을 읽어가도록 설계 변경
    // 따라서 OnTriggerStay는 필수가 아닙니다. 다만, 물리적으로 정확한 시뮬레이션을 원한다면 사용할 수 있습니다.
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

        Debug.Log($"바람 방향 랜덤 변경: {windDirection.normalized}, 강도: {windStrength}");
    }

    public void SetWind(Vector3 direction, float strength)
    {
        this.windDirection = direction.normalized;
        this.windStrength = strength;
        Debug.Log($"바람 설정 변경: 방향={windDirection}, 강도={windStrength}");
    }
}