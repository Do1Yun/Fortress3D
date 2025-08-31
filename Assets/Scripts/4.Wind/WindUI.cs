using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindUI : MonoBehaviour
{
    [Header("UI ��� ����")]
    [Tooltip("�ٶ� ���⸦ ǥ���� TextMeshPro UI")]
    public TextMeshProUGUI windStrengthText;

    [Tooltip("�ٶ� ������ ����ų ȭ��ǥ �̹����� RectTransform")]
    public RectTransform windArrow;

    void Update()
    {
        if (WindController.instance == null) return;

        // WindController���� ���� �ٶ� ������ �����ɴϴ�.
        float strength = WindController.instance.CurrentWindStrength;
        Vector3 direction = WindController.instance.CurrentWindDirection;

        // �ؽ�Ʈ ������Ʈ (��: "�ٶ�: 5.4")
        if (windStrengthText != null)
        {
            windStrengthText.text = $"�ٶ�: {strength:F1}";
        }

        // ȭ��ǥ ���� ������Ʈ
        if (windArrow != null)
        {
            // 3D ������ 2D UI�� �°� ��ȯ�Ͽ� ȭ��ǥ�� ȸ����ŵ�ϴ�.
            // �ٶ��� x, z ������ ����Ͽ� ������ ����մϴ�.
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            windArrow.rotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}