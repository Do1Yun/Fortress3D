using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection;
    public float windStrength;

    public Vector3 minPosition = new Vector3(-10, 0, -10);
    public Vector3 maxPosition = new Vector3(10, 0, 10);
    public Vector3 minSize = new Vector3(3, 3, 3);
    public Vector3 maxSize = new Vector3(6, 6, 6);

    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        ChangeZoneRandomly();
    }

    // 특정 상황에서 호출할 메서드
    public void ChangeZoneRandomly()
    {
        // 위치 변경
        transform.position = new Vector3(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y),
            Random.Range(minPosition.z, maxPosition.z)
        );

        // 크기 변경
        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(
                Random.Range(minSize.x, maxSize.x),
                Random.Range(minSize.y, maxSize.y),
                Random.Range(minSize.z, maxSize.z)
            );
        }

        // 방향과 세기 랜덤 변경
        windDirection = Random.onUnitSphere;
        windStrength = Random.Range(2f, 10f);
    }

    private void OnTriggerStay(Collider other)
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
}
