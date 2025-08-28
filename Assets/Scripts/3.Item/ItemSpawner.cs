using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject itemPrefabs;    // �پ��� ������ ������ �迭
    public float spawnInterval = 5f;    // ���� �ֱ�
    public Vector3 spawnAreaMin;        // ���� ���� ���� �ּҰ�
    public Vector3 spawnAreaMax;        // ���� ���� ���� �ִ밪

    private void Start()
    {
        InvokeRepeating(nameof(SpawnItem), 2f, spawnInterval);
    }

    void SpawnItem()
    {
        // ���� ��ġ
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float z = Random.Range(spawnAreaMin.z, spawnAreaMax.z);
        Vector3 spawnPos = new Vector3(x, 0.5f, z);

        GameObject item = Instantiate(itemPrefabs, spawnPos, Quaternion.identity);

        item.tag = "Item";
    }
}
