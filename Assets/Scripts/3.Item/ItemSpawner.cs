using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject itemPrefab;       // ������ ������
    public Chunk chunk;                 // �������� ������ Chunk
    public float spawnInterval = 5f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnItem();
            timer = 0f;
        }
    }

    void SpawnItem()
    {
        if (chunk == null || itemPrefab == null) return;

        int x = Random.Range(0, chunk.chunkSize);
        int y = 50;
        int z = Random.Range(0, chunk.chunkSize);

        Vector3 spawnPos = new Vector3(
            x + chunk.transform.position.x,
            y + chunk.transform.position.y,
            z + chunk.transform.position.z
        );

        GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
        
    }
}

