using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    // �ٸ� ��ũ��Ʈ���� �� ���带 ���� ������ �� �ֵ��� �̱���(Singleton)���� ����ϴ�.
    public static World Instance { get; private set; }

    [Header("�� ����")]
    public GameObject chunkPrefab;
    public Vector2Int worldSizeInChunks = new Vector2Int(4, 4);

    // ��� ûũ�� ��ǥ�� �Բ� �����Ͽ� ������ ã�� �� �ֵ��� �մϴ�.
    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    void Awake()
    {
        // �̱��� �ν��Ͻ� ����
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

                // ������ ûũ�� Dictionary�� ����մϴ�.
                chunks.Add(chunkPos, newChunk);
            }
        }
    }

    // ������ Ư�� ��ġ�� ���� ������ ��û�ϴ� ���� �Լ�
    public void ModifyTerrain(Vector3 worldPos, float modificationAmount, float radius)
    {
        // ���� �ݰ濡 ������ �޴� ��� ûũ�� ã�Ƽ� ������Ʈ�մϴ�.
        int chunkSize = chunkPrefab.GetComponent<Chunk>().chunkSize;
        int modificationRadiusInChunks = Mathf.CeilToInt(radius / chunkSize);

        // ���� ��ǥ�� ûũ ��ǥ�� ��ȯ
        int centerX = Mathf.FloorToInt(worldPos.x / chunkSize);
        int centerZ = Mathf.FloorToInt(worldPos.z / chunkSize);

        for (int x = centerX - modificationRadiusInChunks; x <= centerX + modificationRadiusInChunks; x++)
        {
            for (int z = centerZ - modificationRadiusInChunks; z <= centerZ + modificationRadiusInChunks; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x, 0, z);
                if (chunks.TryGetValue(chunkPos, out Chunk chunk))
                {
                    // ������ �޴� �� ûũ�� ModifyTerrain �Լ��� ȣ���մϴ�.
                    chunk.ModifyTerrain(worldPos, modificationAmount, radius);
                }
            }
        }
    }
}