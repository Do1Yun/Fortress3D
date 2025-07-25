using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    [Header("지형 설정")]
    public int chunkSize = 16;
    public float isoLevel = 0.5f;
    public Vector3Int chunkPosition;

    private float[,,] voxelPoints;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    public void Initialize()
    {
        GenerateVoxelData();
        CreateMeshData();
    }

    void GenerateVoxelData()
    {
        // 만들려는 평지의 높이
        int groundHeight = 8;
        voxelPoints = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];

        for (int x = 0; x <= chunkSize; x++)
        {
            for (int y = 0; y <= chunkSize; y++)
            {
                for (int z = 0; z <= chunkSize; z++)
                {
                    // 월드 좌표 기준의 높이
                    float worldY = chunkPosition.y * chunkSize + y;

                    // 높이를 기준으로 밀도를 계산 (groundHeight 아래는 모두 1에 가까운 값)
                    float density = groundHeight - worldY;

                    // 값을 0과 1 사이로 제한하여 완벽한 평면과 그 아래를 채웁니다.
                    voxelPoints[x, y, z] = Mathf.Clamp01(density);
                }
            }
        }
    }

    public void CreateMeshData()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

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
                    March(new Vector3Int(x, y, z), cubeCorners, vertices, triangles, normals);
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        meshCollider.sharedMesh = mesh;
    }

    void March(Vector3Int position, float[] cube, List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        int cubeIndex = 0;
        if (cube[0] > isoLevel) cubeIndex |= 1;
        if (cube[1] > isoLevel) cubeIndex |= 2;
        if (cube[2] > isoLevel) cubeIndex |= 4;
        if (cube[3] > isoLevel) cubeIndex |= 8;
        if (cube[4] > isoLevel) cubeIndex |= 16;
        if (cube[5] > isoLevel) cubeIndex |= 32;
        if (cube[6] > isoLevel) cubeIndex |= 64;
        if (cube[7] > isoLevel) cubeIndex |= 128;

        for (int i = 0; MarchingCubesTables.triangleTable[cubeIndex, i] != -1; i += 3)
        {
            int edgeA = MarchingCubesTables.triangleTable[cubeIndex, i];
            int edgeB = MarchingCubesTables.triangleTable[cubeIndex, i + 1];
            int edgeC = MarchingCubesTables.triangleTable[cubeIndex, i + 2];

            Vector3 vertA, normalA;
            InterpolateVertexAndNormal(edgeA, cube, position, out vertA, out normalA);

            Vector3 vertB, normalB;
            InterpolateVertexAndNormal(edgeB, cube, position, out vertB, out normalB);

            Vector3 vertC, normalC;
            InterpolateVertexAndNormal(edgeC, cube, position, out vertC, out normalC);

            triangles.Add(vertices.Count);
            vertices.Add(vertA);
            normals.Add(normalA);

            triangles.Add(vertices.Count);
            vertices.Add(vertB);
            normals.Add(normalB);

            triangles.Add(vertices.Count);
            vertices.Add(vertC);
            normals.Add(normalC);
        }
    }

    Vector3 CalculateNormal(Vector3Int point)
    {
        float density_x1 = (point.x + 1 <= chunkSize) ? voxelPoints[point.x + 1, point.y, point.z] : voxelPoints[point.x, point.y, point.z];
        float density_x0 = (point.x - 1 >= 0) ? voxelPoints[point.x - 1, point.y, point.z] : voxelPoints[point.x, point.y, point.z];

        float density_y1 = (point.y + 1 <= chunkSize) ? voxelPoints[point.x, point.y + 1, point.z] : voxelPoints[point.x, point.y, point.z];
        float density_y0 = (point.y - 1 >= 0) ? voxelPoints[point.x, point.y - 1, point.z] : voxelPoints[point.x, point.y, point.z];

        float density_z1 = (point.z + 1 <= chunkSize) ? voxelPoints[point.x, point.y, point.z + 1] : voxelPoints[point.x, point.y, point.z];
        float density_z0 = (point.z - 1 >= 0) ? voxelPoints[point.x, point.y, point.z - 1] : voxelPoints[point.x, point.y, point.z];

        float dx = density_x1 - density_x0;
        float dy = density_y1 - density_y0;
        float dz = density_z1 - density_z0;

        Vector3 normal = new Vector3(-dx, -dy, -dz);

        // [추가] 법선 벡터의 길이가 0에 가까우면 (오류 발생 가능성이 있으면) 기본 값(위쪽)을 반환합니다.
        if (normal.sqrMagnitude < 0.0001f)
        {
            return Vector3.up;
        }

        return normal.normalized;
    }

    void InterpolateVertexAndNormal(int edgeIndex, float[] cube, Vector3Int position, out Vector3 vert, out Vector3 normal)
    {
        int v0_idx = MarchingCubesTables.edgeTable[edgeIndex, 0];
        int v1_idx = MarchingCubesTables.edgeTable[edgeIndex, 1];

        Vector3Int p0 = position + MarchingCubesTables.cornerTable[v0_idx];
        Vector3Int p1 = position + MarchingCubesTables.cornerTable[v1_idx];

        float d0 = cube[v0_idx];
        float d1 = cube[v1_idx];

        float t = (isoLevel - d0) / (d1 - d0);

        if (float.IsNaN(t) || float.IsInfinity(t))
        {
            t = 0.5f;
        }

        vert = Vector3.Lerp(p0, p1, t);

        Vector3 n0 = CalculateNormal(p0);
        Vector3 n1 = CalculateNormal(p1);
        normal = Vector3.Lerp(n0, n1, t).normalized;
    }

    public void ModifyTerrain(Vector3 worldPos, float modificationAmount, float radius)
    {
        Vector3 localPos = worldPos - transform.position;
        bool needsUpdate = false;

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
                        float oldValue = voxelPoints[x, y, z];
                        float modification = modificationAmount * (1f - distance / radius);
                        voxelPoints[x, y, z] = Mathf.Clamp01(voxelPoints[x, y, z] + modification);

                        if (oldValue != voxelPoints[x, y, z])
                        {
                            needsUpdate = true;
                        }
                    }
                }
            }
        }
        if (needsUpdate)
        {
            CreateMeshData();
        }
    }
    
}

