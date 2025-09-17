using UnityEngine;

public class ColorManager : MonoBehaviour
{
    // 인스펙터 창에서 색상을 변경할 머터리얼을 연결해주세요.
    public Material targetMaterial;

    // 인스펙터 창에서 원본 머터리얼을 연결해주세요.
    public Material originalMaterial;

    void Start()
    {
        // 씬이 시작될 때, 원본 머터리얼의 색상으로 타겟 머터리얼을 초기화합니다.
        if (targetMaterial != null && originalMaterial != null)
        {
            targetMaterial.color = originalMaterial.color;
        }
    }

    // 이 함수는 FlexibleColorPicker의 onColorChange 이벤트에 연결될 것입니다.
    public void SetMaterialColor(Color newColor)
    {
        if (targetMaterial != null)
        {
            // 전달받은 색상으로 머터리얼의 색상을 변경합니다.
            targetMaterial.color = newColor;
        }
    }
}