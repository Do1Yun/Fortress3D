// WindZone.cs
using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right; // 바람 방향
    public float windStrength = 5f; // 힘의 크기
    public float maxRandomAngleChange = 30f; // 랜덤 바람 방향 변화 최대 각도

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Projectile")) // 포탄만 적용
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(windDirection * windStrength, ForceMode.Force);
            }
        }
    }

    // GameManager에서 호출할 수 있는 랜덤 바람 방향 설정 메서드 (선택 사항)
    public void SetRandomWindDirection()
    {
        // Y축 기준으로 랜덤하게 회전 (수평 바람)
        float randomAngle = Random.Range(-maxRandomAngleChange, maxRandomAngleChange);
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        windDirection = rotation * Vector3.right; // 초기 windDirection이 Vector3.right라고 가정
        
        Debug.Log($"바람 방향이 랜덤으로 변경되었습니다: {windDirection.normalized}, 강도: {windStrength}");
    }

    // 특정 방향과 강도로 바람을 설정하는 메서드 (선택 사항)
    public void SetWind(Vector3 direction, float strength)
    {
        this.windDirection = direction.normalized;
        this.windStrength = strength;
        Debug.Log($"바람이 설정되었습니다: 방향={windDirection}, 강도={windStrength}");
    }
}