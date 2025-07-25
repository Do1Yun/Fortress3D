using UnityEngine;

public class TerrainEditor : MonoBehaviour
{
    public Camera mainCamera;
    public float modificationStrength = 1f;
    public float modificationRadius = 5f;

    void Update()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // ���� ûũ ��� World.Instance�� ������ ��û�մϴ�.
                if (World.Instance != null)
                {
                    float modificationAmount = (Input.GetMouseButton(0) ? -1 : 1) * modificationStrength;
                    World.Instance.ModifyTerrain(hit.point, modificationAmount * Time.deltaTime, modificationRadius);
                }
            }
        }
    }
}