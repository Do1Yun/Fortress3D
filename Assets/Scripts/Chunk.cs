using System.Collections.Generic;
using UnityEngine;

// �� ������Ʈ���� ������ �ڵ����� �߰����ݴϴ�.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    // --- ���� ���� (Public Variables) ---
    [Header("���� ����")]
    public int chunkSize = 16;      // �� ûũ�� ũ�� (16x16x16)
    public float isoLevel = 0.5f;   // ���� ���⸦ ������ ���� ��
    public float scale = 0.1f;      // ������ ������ (Perlin Noise ��)
    public Vector3Int chunkPosition; // ���忡���� ûũ ��ġ

    // --- ���� ���� (Private Variables) ---
    private float[,,] voxelPoints;   // ���� ������ ���� �迭
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;

    // Start ��� Awake�� ����Ͽ� WorldGenerator�� ���� ������ ���� ����ǵ��� �մϴ�.
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    // WorldGenerator�� ���� ȣ��Ǿ� ���� ������ �����մϴ�.
    public void Initialize()
    {
        GenerateVoxelData();
        CreateMeshData();
    }

    // �ʱ� ���� �����͸� �����մϴ� (Perlin Noise ���)
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

    // 3D Perlin Noise �Լ�
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

    // ��Ī ť�� �˰������� �޽� ������ ���� �� ����
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

   
    // Ư�� ť�� �ϳ��� �޽��� ����ϰ� ����Ʈ�� �߰�
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

        // triangleTable�� 2���� �迭�� ���� �����ϵ��� ����
        for (int i = 0; MarchingCubesTables.triangleTable[cubeIndex, i] != -1; i += 3)
        {
            // �ﰢ���� �� �������� �ش��ϴ� ����(�𼭸�) ��ȣ
            int edgeIndex1 = MarchingCubesTables.triangleTable[cubeIndex, i];
            int edgeIndex2 = MarchingCubesTables.triangleTable[cubeIndex, i + 1];
            int edgeIndex3 = MarchingCubesTables.triangleTable[cubeIndex, i + 2];

            // �� �������� ���ؽ�(����) ��ġ ���
            Vector3 vert1 = InterpolateVertex(MarchingCubesTables.edgeTable[edgeIndex1, 0], MarchingCubesTables.edgeTable[edgeIndex1, 1], cube, position);
            Vector3 vert2 = InterpolateVertex(MarchingCubesTables.edgeTable[edgeIndex2, 0], MarchingCubesTables.edgeTable[edgeIndex2, 1], cube, position);
            Vector3 vert3 = InterpolateVertex(MarchingCubesTables.edgeTable[edgeIndex3, 0], MarchingCubesTables.edgeTable[edgeIndex3, 1], cube, position);

            // �ﰢ���� �޽��� �߰�
            triangles.Add(vertices.Count);
            vertices.Add(vert1);
            triangles.Add(vertices.Count);
            vertices.Add(vert2);
            triangles.Add(vertices.Count);
            vertices.Add(vert3);
        }
    }
    // �� ������ ������ ��Ȯ�� ���ؽ� ��ġ�� ���������� ���
    Vector3 InterpolateVertex(int v1Index, int v2Index, float[] cube, Vector3Int position)
    {
        Vector3 corner1 = position + MarchingCubesTables.cornerTable[v1Index];
        Vector3 corner2 = position + MarchingCubesTables.cornerTable[v2Index];
        float val1 = cube[v1Index];
        float val2 = cube[v2Index];
        return Vector3.Lerp(corner1, corner2, (isoLevel - val1) / (val2 - val1));
    }

    // �ܺο��� ������ �����ϱ� ���� �Լ�
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