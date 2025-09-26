using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    [Header("맵 설정")]
    public GameObject chunkPrefab;
    public Vector2Int worldSizeInChunks = new Vector2Int(4, 4);

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
        private List<CaptureZone> captureZones;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
         captureZones = new List<CaptureZone>(FindObjectsOfType<CaptureZone>());

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

                chunks.Add(chunkPos, newChunk);
            }
        }
    }

    public void ModifyTerrain(Vector3 worldPos, float modificationAmount, float radius)
    { foreach (CaptureZone zone in captureZones)
        {
            // 폭발 지점과 거점 중심 사이의 거리를 계산합니다.
            float distanceToZone = Vector3.Distance(worldPos, zone.transform.position);

            // 만약 거리가 거점의 반경보다 작거나 같다면 (즉, 거점 안이라면)
            if (distanceToZone <= zone.captureRadius)
            {
                Debug.Log("거점 보호 지역 안에서는 지형을 변경할 수 없습니다.");
                return; // 함수를 즉시 종료하여 지형 변경을 막습니다.
            }
        }
        int chunkSize = chunkPrefab.GetComponent<Chunk>().chunkSize;
        int modificationRadiusInChunks = Mathf.CeilToInt(radius / chunkSize);

        int centerX = Mathf.FloorToInt(worldPos.x / chunkSize);
        int centerZ = Mathf.FloorToInt(worldPos.z / chunkSize);

        for (int x = centerX - modificationRadiusInChunks; x <= centerX + modificationRadiusInChunks; x++)
        {
            for (int z = centerZ - modificationRadiusInChunks; z <= centerZ + modificationRadiusInChunks; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x, 0, z);
                if (chunks.TryGetValue(chunkPos, out Chunk chunk))
                {
                    // 청크의 ModifyTerrain 메서드 호출
                    chunk.ModifyTerrain(worldPos, modificationAmount, radius);
                }
            }
        }
    }
}