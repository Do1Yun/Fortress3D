using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get; private set; }
    private GameManager gameManager; // 변수 선언

    [Header("맵 설정")]
    public GameObject chunkPrefab;
    public Vector2Int worldSizeInChunks = new Vector2Int(4, 4);

    [Header("지형 감지 레이어")]
    public LayerMask terrainLayer; // ★ 인스펙터에서 Environment 레이어 할당 필수!

    [Header("오디오 설정")]
    public AudioClip TerrainDestructionCommentary; // 파괴 멘트
    public AudioClip TerrainCreationCommentary;    // 생성 멘트

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
        gameManager = GameManager.instance;
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
     

        if (Application.isPlaying)
        {
            GenerateWorld();
        }
    }

    [ContextMenu("Generate World (Editor)")]
    public void GenerateWorld()
    {
        ClearWorld();

        captureZones = new List<CaptureZone>(FindObjectsOfType<CaptureZone>());
        spawnZones = new List<SpawnZone>(FindObjectsOfType<SpawnZone>());

        if (chunkPrefab == null)
        {
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

    [ContextMenu("Clear World (Editor)")]
    public void ClearWorld()
    {
        chunks.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }


    public void ModifyTerrain(Vector3 worldPos, float modificationAmount, float radius)
    {
        // 1. 스폰 보호 구역 체크
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

        if (GameManager.instance.dangtang == false)
        {
            // 2. 허공 체크 및 멘트 재생
            if (modificationAmount > 0) // 생성
            {
                if (Physics.CheckSphere(worldPos, radius, terrainLayer))
                {
                    PlayTerrainAudio(TerrainCreationCommentary);
                }
            }
            else // 파괴
            {
                // ★ 허공인지 체크 (반경 내에 Environment 레이어가 없으면 파괴 안 함 & 소리 안 냄)
                if (Physics.CheckSphere(worldPos, radius, terrainLayer))
                {
                    PlayTerrainAudio(TerrainDestructionCommentary);
                }
                else
                {
                    // 허공이면 함수 종료 (아무 일도 안 일어남)
                    return;
                }
            }
        }

        // 3. 실제 지형 수정 로직
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

    // 중복 코드를 줄이기 위한 내부 함수
    void PlayTerrainAudio(AudioClip clip)
    {
        // 확률 체크 (100%)
        if (Random.value <= 0.33f)
        {
            if (gameManager != null && gameManager.announcerAudioSource != null && clip != null)
            {
                gameManager.announcerAudioSource.Stop();
                gameManager.announcerAudioSource.PlayOneShot(clip);
            }
        }
    }
}