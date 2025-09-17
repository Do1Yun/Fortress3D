using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject itemPrefab;       // 아이템 프리팹
    public Chunk chunk;                 // 아이템을 생성할 Chunk
    public float spawnInterval = 5f;
    public int x_min_index = 0;
    public int x_max_index = 0;
    public int z_min_index = 0;
    public int z_max_index = 0;

    public GameManager gameManager;
    private float timer;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");
    }

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
        if (chunk == null || itemPrefab == null || gameManager.isMGTime()) return;  // 우당탕탕 일때 아이템 안떨구도록 수정

        int x = Random.Range(x_min_index, x_max_index);
        int y = 50;
        int z = Random.Range(z_min_index, z_max_index);

        Vector3 spawnPos = new Vector3(
            x + chunk.transform.position.x,
            y + chunk.transform.position.y,
            z + chunk.transform.position.z
        );

        GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
        
    }
}

