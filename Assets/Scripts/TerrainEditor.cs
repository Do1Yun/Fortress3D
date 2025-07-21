using UnityEngine;

public class TerrainEditor : MonoBehaviour
{
    public Camera mainCamera;
    public float modificationStrength = 1f; // �ʴ� ���� ����
    public float modificationRadius = 5f;

    void Update()
    {
        // ���� ��ư: �ı� / ������ ��ư: ����
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Chunk chunk = hit.collider.GetComponent<Chunk>();
                if (chunk != null)
                {
                    float modificationAmount = (Input.GetMouseButton(0) ? -1 : 1) * modificationStrength;
                    chunk.ModifyTerrain(hit.point, modificationAmount * Time.deltaTime, modificationRadius);
                }
            }
        }
    }
}