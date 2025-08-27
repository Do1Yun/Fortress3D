using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    // 다른 스크립트에서 이 월드를 쉽게 참조할 수 있도록 싱글턴(Singleton)으로 만듭니다.
    public static World Instance { get; private set; }

    [Header("맵 설정")]
    public GameObject chunkPrefab;
    public Vector2Int worldSizeInChunks = new Vector2Int(4, 4);

    // 모든 청크를 좌표와 함께 저장하여 빠르게 찾을 수 있도록 합니다.
    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    void Awake()
    {
        // 싱글턴 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        GenerateWorld();
    }

    void GenerateWorld()
    {
        if (chunkPrefab == null)
        {
            Debug.LogError("Chunk Prefab이 할당되지 않았습니다!");
            return;
        }

        int chunkSize = chunkPrefab.GetComponent<Chunk>().chunkSize;

        for (int x = 0; x < worldSizeInChunks.x; x++)
        {
            for (int z = 0; z < worldSizeInChunks.y; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x, 0, z);
                Vector3 worldPosition = new Vector3(x * chunkSize, 0, z * chunkSize);

                GameObject newChunkObject = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, this.transform);
                Chunk newChunk = newChunkObject.GetComponent<Chunk>();
                newChunk.chunkPosition = chunkPos;
                newChunk.Initialize();

                // 생성된 청크를 Dictionary에 등록합니다.
                chunks.Add(chunkPos, newChunk);
            }
        }
    }

    // 월드의 특정 위치에 지형 수정을 요청하는 전역 함수
    public void ModifyTerrain(Vector3 worldPos, float modificationAmount, float radius)
    {
        // 수정 반경에 영향을 받는 모든 청크를 찾아서 업데이트합니다.
        int chunkSize = chunkPrefab.GetComponent<Chunk>().chunkSize;
        int modificationRadiusInChunks = Mathf.CeilToInt(radius / chunkSize);

        // 월드 좌표를 청크 좌표로 변환
        int centerX = Mathf.FloorToInt(worldPos.x / chunkSize);
        int centerZ = Mathf.FloorToInt(worldPos.z / chunkSize);

        for (int x = centerX - modificationRadiusInChunks; x <= centerX + modificationRadiusInChunks; x++)
        {
            for (int z = centerZ - modificationRadiusInChunks; z <= centerZ + modificationRadiusInChunks; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x, 0, z);
                if (chunks.TryGetValue(chunkPos, out Chunk chunk))
                {
                    // 영향을 받는 각 청크의 ModifyTerrain 함수를 호출합니다.
                    chunk.ModifyTerrain(worldPos, modificationAmount, radius);
                }
            }
        }
    }
}