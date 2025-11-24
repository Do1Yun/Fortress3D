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
    [Header("오디오 설정")]
    [Tooltip("아이템이 생성될 때 재생할 중계 멘트")]
    public AudioClip itemSpawnCommentary;
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
        if (Random.value <= 0.33f) // 확률 (현재 100%로 설정됨)
        {
            if (gameManager != null && gameManager.announcerAudioSource != null && itemSpawnCommentary != null)
            {
                // 기존 멘트가 있다면 끊고, 아이템 멘트를 즉시 재생
                gameManager.announcerAudioSource.Stop();
                gameManager.announcerAudioSource.PlayOneShot(itemSpawnCommentary);
                Debug.Log("아이템 생성 멘트 재생!");
            }
        }

    }
}

