using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }

    [Header("�� ����")]
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
            Debug.LogError("Chunk Prefab�� �Ҵ���� �ʾҽ��ϴ�!");
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
            // ���� ������ ���� �߽� ������ �Ÿ��� ����մϴ�.
            float distanceToZone = Vector3.Distance(worldPos, zone.transform.position);

            // ���� �Ÿ��� ������ �ݰ溸�� �۰ų� ���ٸ� (��, ���� ���̶��)
            if (distanceToZone <= zone.captureRadius)
            {
                Debug.Log("���� ��ȣ ���� �ȿ����� ������ ������ �� �����ϴ�.");
                return; // �Լ��� ��� �����Ͽ� ���� ������ �����ϴ�.
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
                    // ûũ�� ModifyTerrain �޼��� ȣ��
                    chunk.ModifyTerrain(worldPos, modificationAmount, radius);
                }
            }
        }
    }
}