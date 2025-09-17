using UnityEngine;

public class ColorManager : MonoBehaviour
{
    // �ν����� â���� ������ ������ ���͸����� �������ּ���.
    public Material targetMaterial;

    // �ν����� â���� ���� ���͸����� �������ּ���.
    public Material originalMaterial;

    void Start()
    {
        // ���� ���۵� ��, ���� ���͸����� �������� Ÿ�� ���͸����� �ʱ�ȭ�մϴ�.
        if (targetMaterial != null && originalMaterial != null)
        {
            targetMaterial.color = originalMaterial.color;
        }
    }

    // �� �Լ��� FlexibleColorPicker�� onColorChange �̺�Ʈ�� ����� ���Դϴ�.
    public void SetMaterialColor(Color newColor)
    {
        if (targetMaterial != null)
        {
            // ���޹��� �������� ���͸����� ������ �����մϴ�.
            targetMaterial.color = newColor;
        }
    }
}