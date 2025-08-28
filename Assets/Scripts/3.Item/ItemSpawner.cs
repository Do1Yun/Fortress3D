using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject itemPrefabs;    // 다양한 아이템 프리팹 배열
    public float spawnInterval = 5f;    // 스폰 주기
    public Vector3 spawnAreaMin;        // 스폰 가능 범위 최소값
    public Vector3 spawnAreaMax;        // 스폰 가능 범위 최대값

    private void Start()
    {
        InvokeRepeating(nameof(SpawnItem), 2f, spawnInterval);
    }

    void SpawnItem()
    {
        // 랜덤 위치
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float z = Random.Range(spawnAreaMin.z, spawnAreaMax.z);
        Vector3 spawnPos = new Vector3(x, 0.5f, z);

        GameObject item = Instantiate(itemPrefabs, spawnPos, Quaternion.identity);

        item.tag = "Item";
    }
}
