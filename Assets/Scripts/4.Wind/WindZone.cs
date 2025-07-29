using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right; // 바람 방향
    public float windStrength = 5f; // 힘의 크기

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
}
