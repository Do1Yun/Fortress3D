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
         captureZones = new List<CaptureZone>(FindObjectsOfType<CaptureZone>());
        spawnZones = new List<SpawnZone>(FindObjectsOfType<SpawnZone>());

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
    {
        foreach (SpawnZone zone in spawnZones)
        {
            // 1. AABB(축 정렬 경계 상자)로 1차 고속 검사
            //    수정 지점(worldPos)이 일단 콜라이더의 바운더리 박스 안에 있는지 확인
            if (zone.zoneCollider.bounds.Contains(worldPos))
            {
                // 2. 1차 통과 시, 더 정밀한 2차 검사
                //    Collider.ClosestPoint()는 콜라이더 표면(또는 내부)에서 worldPos와 가장 가까운 점을 반환
                Vector3 closestPoint = zone.zoneCollider.ClosestPoint(worldPos);

                // 만약 가장 가까운 점과 worldPos 사이의 거리가 매우 가깝다면 (거의 0)
                // worldPos는 콜라이더 내부에 있는 것으로 간주합니다.
                if (Vector3.Distance(closestPoint, worldPos) < 0.001f)
                {
                    Debug.Log("스폰 지점 보호 지역 안에서는 지형을 변경할 수 없습니다.");
                    return; // 함수를 즉시 종료하여 지형 변경을 막습니다.
                }
            }
        }
        foreach (CaptureZone zone in captureZones)
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