using System.Collections.Generic;
using UnityEngine;

// 이 컴포넌트들이 없으면 자동으로 추가해줍니다.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    // --- 공개 변수 (Public Variables) ---
    [Header("지형 설정")]
    public int chunkSize = 16;      // 한 청크의 크기 (16x16x16)
    public float isoLevel = 0.5f;   // 땅과 공기를 나누는 기준 값
    public float scale = 0.1f;      // 지형의 세밀함 (Perlin Noise 용)
    public Vector3Int chunkPosition; // 월드에서의 청크 위치

    // --- 내부 변수 (Private Variables) ---
    private float[,,] voxelPoints;   // 복셀 데이터 저장 배열
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;

    // Start 대신 Awake를 사용하여 WorldGenerator가 값을 설정한 직후 실행되도록 합니다.
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    // WorldGenerator에 의해 호출되어 지형 생성을 시작합니다.
    public void Initialize()
    {
        GenerateVoxelData();
        CreateMeshData();
    }

    // 초기 복셀 데이터를 생성합니다 (Perlin Noise 사용)
    void GenerateVoxelData()
    {
        voxelPoints = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];
        for (int x = 0; x <= chunkSize; x++)
        {
            for (int y = 0; y <= chunkSize; y++)
            {
                for (int z = 0; z <= chunkSize; z++)
                {
                    float worldX = (chunkPosition.x * chunkSize + x) * scale;
                    float worldY = (chunkPosition.y * chunkSize + y) * scale;
                    float worldZ = (chunkPosition.z * chunkSize + z) * scale;
                    voxelPoints[x, y, z] = Perlin3D(worldX, worldY, worldZ);
                }
            }
        }
    }

    // 3D Perlin Noise 함수
    public static float Perlin3D(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);
        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);
        return (ab + bc + ac + ba + cb + ca) / 6f;
    }

    // 마칭 큐브 알고리즘으로 메쉬 데이터 생성 및 적용
    public void CreateMeshData()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float[] cubeCorners = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingCubesTables.cornerTable[i];
                        cubeCorners[i] = voxelPoints[corner.x, corner.y, corner.z];
                    }
                    March(new Vector3Int(x, y, z), cubeCorners, vertices, triangles);
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshCollider.sharedMesh = mesh;
    }

   
    // 특정 큐브 하나의 메쉬를 계산하고 리스트에 추가
    void March(Vector3Int position, float[] cube, List<Vector3> vertices, List<int> triangles)
    {
        int cubeIndex = 0;
        if (cube[0] < isoLevel) cubeIndex |= 1;
        if (cube[1] < isoLevel) cubeIndex |= 2;
        if (cube[2] < isoLevel) cubeIndex |= 4;
        if (cube[3] < isoLevel) cubeIndex |= 8;
        if (cube[4] < isoLevel) cubeIndex |= 16;
        if (cube[5] < isoLevel) cubeIndex |= 32;
        if (cube[6] < isoLevel) cubeIndex |= 64;
        if (cube[7] < isoLevel) cubeIndex |= 128;

        // triangleTable을 2차원 배열로 직접 접근하도록 수정
        for (int i = 0; MarchingCubesTables.triangleTable[cubeIndex, i] != -1; i += 3)
        {
            // 삼각형의 세 꼭짓점에 해당하는 엣지(모서리) 번호
            int edgeIndex1 = MarchingCubesTables.triangleTable[cubeIndex, i];
            int edgeIndex2 = MarchingCubesTables.triangleTable[cubeIndex, i + 1];
            int edgeIndex3 = MarchingCubesTables.triangleTable[cubeIndex, i + 2];

            // 각 엣지에서 버텍스(정점) 위치 계산
            Vector3 vert1 = InterpolateVertex(MarchingCubesTables.edgeTable[edgeIndex1, 0], MarchingCubesTables.edgeTable[edgeIndex1, 1], cube, position);
            Vector3 vert2 = InterpolateVertex(MarchingCubesTables.edgeTable[edgeIndex2, 0], MarchingCubesTables.edgeTable[edgeIndex2, 1], cube, position);
            Vector3 vert3 = InterpolateVertex(MarchingCubesTables.edgeTable[edgeIndex3, 0], MarchingCubesTables.edgeTable[edgeIndex3, 1], cube, position);

            // 삼각형을 메쉬에 추가
            triangles.Add(vertices.Count);
            vertices.Add(vert1);
            triangles.Add(vertices.Count);
            vertices.Add(vert2);
            triangles.Add(vertices.Count);
            vertices.Add(vert3);
        }
    }
    // 두 꼭짓점 사이의 정확한 버텍스 위치를 보간법으로 계산
    Vector3 InterpolateVertex(int v1Index, int v2Index, float[] cube, Vector3Int position)
    {
        Vector3 corner1 = position + MarchingCubesTables.cornerTable[v1Index];
        Vector3 corner2 = position + MarchingCubesTables.cornerTable[v2Index];
        float val1 = cube[v1Index];
        float val2 = cube[v2Index];
        return Vector3.Lerp(corner1, corner2, (isoLevel - val1) / (val2 - val1));
    }

    // 외부에서 지형을 수정하기 위한 함수
    public void ModifyTerrain(Vector3 worldPos, float modificationAmount, float radius)
    {
        Vector3 localPos = worldPos - transform.position;

        for (int x = 0; x <= chunkSize; x++)
        {
            for (int y = 0; y <= chunkSize; y++)
            {
                for (int z = 0; z <= chunkSize; z++)
                {
                    Vector3 pointPos = new Vector3(x, y, z);
                    float distance = Vector3.Distance(pointPos, localPos);
                    if (distance < radius)
                    {
                        float modification = modificationAmount * (1f - distance / radius);
                        voxelPoints[x, y, z] += modification;
                        voxelPoints[x, y, z] = Mathf.Clamp01(voxelPoints[x, y, z]);
                    }
                }
            }
        }
        CreateMeshData();
    }
}