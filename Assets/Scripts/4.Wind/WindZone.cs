using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windStrength = 5f;

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
//using System;
//using System.Collections;
//using UnityEngine;

//public class WindZone : MonoBehaviour
//{
//    public Vector3 windDirection = Random.onUnitSphere;
//    public float windStrength = Random.Range(2f, 10f);

//    public Vector3 minPosition = new Vector3(-10, 0, -10);
//    public Vector3 maxPosition = new Vector3(10, 0, 10);
//    public Vector3 minSize = new Vector3(3, 3, 3);
//    public Vector3 maxSize = new Vector3(6, 6, 6);

//    public float changeInterval = 5f;

//    private BoxCollider boxCollider;

//    private void Start()
//    {
//        boxCollider = GetComponent<BoxCollider>();
//    }

//    public void ChangeZoneRandomly() // GameMangager의 Update함수에 조건+함수호출로 사용
//    {
//         // 위치 랜덤
//         transform.position = new Vector3(
//             Random.Range(minPosition.x, maxPosition.x),
//             Random.Range(minPosition.y, maxPosition.y),
//             Random.Range(minPosition.z, maxPosition.z)
//         );

//         // 크기 랜덤
//         if (boxCollider != null)
//         {
//             boxCollider.size = new Vector3(
//                 Random.Range(minSize.x, maxSize.x),
//                 Random.Range(minSize.y, maxSize.y),
//                 Random.Range(minSize.z, maxSize.z)
//             );
//         }

//         windDirection = Random.onUnitSphere;
//         windStrength = Random.Range(2f, 10f);

//    }

//    private void OnTriggerStay(Collider other)
//    {
//        if (other.CompareTag("Projectile"))
//        {
//            Rigidbody rb = other.GetComponent<Rigidbody>();
//            if (rb != null)
//            {
//                rb.AddForce(windDirection * windStrength, ForceMode.Force);
//            }
//        }
//    }
//}
