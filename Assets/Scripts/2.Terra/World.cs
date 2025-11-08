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
    private List<SpawnZone> spawnZones;

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

        // 보호 구역은 Awake에서 찾아두는 것이 좋습니다.
        captureZones = new List<CaptureZone>(FindObjectsOfType<CaptureZone>());
        spawnZones = new List<SpawnZone>(FindObjectsOfType<SpawnZone>());
    }

    void Start()
    {
        
       
        if (Application.isPlaying)
        {
            GenerateWorld();
        }
      
    }

    [ContextMenu("Generate World (Editor)")]
    public void GenerateWorld()
    {
        // 1. 맵 생성 전, 기존 맵이 있다면 삭제합니다.
        ClearWorld();

        // 2. (중요) 에디터에서 실행 시 보호 구역 리스트를 다시 찾아옵니다.
        captureZones = new List<CaptureZone>(FindObjectsOfType<CaptureZone>());
        spawnZones = new List<SpawnZone>(FindObjectsOfType<SpawnZone>());

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

    // 인스펙터의 컨텍스트 메뉴(점 3개)에 "Clear World (Editor)" 옵션을 추가합니다.
    [ContextMenu("Clear World (Editor)")]
    public void ClearWorld()
    {
        chunks.Clear();

        // 자식 오브젝트(청크들)를 파괴합니다.
        // 에디터 모드에서는 DestroyImmediate를 사용해야 안전합니다.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }


    public void ModifyTerrain(Vector3 worldPos, float modificationAmount, float radius)
    {
        foreach (SpawnZone zone in spawnZones)
        {
            if (zone.zoneCollider.bounds.Contains(worldPos))
            {
                Vector3 closestPoint = zone.zoneCollider.ClosestPoint(worldPos);
                if (Vector3.Distance(closestPoint, worldPos) < 0.001f)
                {
                    Debug.Log("스폰 지점 보호 지역 안에서는 지형을 변경할 수 없습니다.");
                    return;
                }
            }
        }
        foreach (CaptureZone zone in captureZones)
        {
            float distanceToZone = Vector3.Distance(worldPos, zone.transform.position);
            if (distanceToZone <= zone.captureRadius)
            {
                Debug.Log("거점 보호 지역 안에서는 지형을 변경할 수 없습니다.");
                return;
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
                    chunk.ModifyTerrain(worldPos, modificationAmount, radius);
                }
            }
        }
    }
}