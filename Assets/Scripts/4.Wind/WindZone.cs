using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right; // �ٶ� ����
    public float windStrength = 5f; // ���� ũ��

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
}
