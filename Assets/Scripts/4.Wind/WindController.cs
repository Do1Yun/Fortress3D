using UnityEngine;

public class WindController : MonoBehaviour
{
    public static WindController instance;

    [Header("�ٶ� ���� ����")]
    [Tooltip("�ٶ��� �ּ� �����Դϴ�.")]
    public float minWindStrength = 1f;
    [Tooltip("�ٶ��� �ִ� �����Դϴ�.")]
    public float maxWindStrength = 8f;

    [Header("�ٶ� ���� ����")]
    [Tooltip("���� ���� �ٶ��� ������ �����մϴ�. (0 = ���� �ٶ���, 1 = ���� 3D ����)")]
    [Range(0f, 1f)]
    public float verticalWindFactor = 0.2f; // <-- �� ������ �߰��Ǿ����ϴ�!

    public Vector3 CurrentWindDirection { get; private set; }
    public float CurrentWindStrength { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ChangeWind();
    }

    public void ChangeWind()
    {
        // 1. �ϴ� ���� 3D ���� ������ �����մϴ�.
        Vector3 randomDirection = Random.onUnitSphere;

        // 2. Y��(����) ������ ���� verticalWindFactor ����ŭ �ٿ��ݴϴ�.
        randomDirection.y *= verticalWindFactor;

        // 3. ���� ������ ��ü ���̸� �ٽ� 1�� ����ȭ�Ͽ� ������ �������� ����ϴ�.
        CurrentWindDirection = randomDirection.normalized;

        CurrentWindStrength = Random.Range(minWindStrength, maxWindStrength);

        Debug.Log($" �� ����! ���ο� �ٶ� �߻�! ����: {CurrentWindDirection}, ����: {CurrentWindStrength:F1}");
    }
}