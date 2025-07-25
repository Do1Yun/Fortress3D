using UnityEngine;

public class CameraController : MonoBehaviour
{
    // ����ٴ� ��ǥ (�÷��̾� ��ũ)
    private Transform target;
    // ���� �� �ٶ� Ÿ�� (��ž)
    private Transform aimTarget;

    [Header("�⺻ ����")]
    public Vector3 offset = new Vector3(0, 10f, -8f);
    public float smoothSpeed = 5f;

    [Header("���� ��� ����")]
    public Vector3 aimOffset = new Vector3(2f, 7f, -3f); // ���� �� �� ����� ��ġ
    public float aimSmoothSpeed = 10f; // ���� �� �� ���� ȸ�� �ӵ�

    private bool isAimingMode = false;

    // LateUpdate�� ��� Update �Լ��� ȣ��� �Ŀ� ����˴ϴ�.
    // �÷��̾ ������ '��'�� ī�޶� ���󰡾� ���̳� ������ �����Ƿ�
    // ī�޶� �̵��� LateUpdate���� ó���ϴ� ���� �����ϴ�.
    void LateUpdate()
    {
        // Ÿ���� �������� �ʾҴٸ� �ƹ��͵� ���� ����
        if (target == null)
        {
            return;
        }

        // ���� ��忡 �´� �����°� �ӵ��� ����
        Vector3 currentOffset = isAimingMode ? aimOffset : offset;
        float currentSpeed = isAimingMode ? aimSmoothSpeed : smoothSpeed;

        // 1. ��ġ �̵�
        Vector3 desiredPosition = target.position + currentOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, currentSpeed * Time.deltaTime);

        // 2. ȸ�� ��� ����
        if (isAimingMode && aimTarget != null)
        {
            // ���� ���: ī�޶��� ������ ��ž�� �������� �ε巴�� ��ġ��Ŵ
            Quaternion desiredRotation = Quaternion.LookRotation(aimTarget.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, currentSpeed * Time.deltaTime);
        }
        else
        {
            // �⺻ ���: �÷��̾��� ��ü�� �ٶ�
            transform.LookAt(target);
        }
    }

    // ��� ��ȯ�� ���� �Լ�
    public void ToggleAimMode(bool isAiming, Transform newAimTarget = null)
    {
        isAimingMode = isAiming;
        aimTarget = newAimTarget;
    }

    // GameManager�� ȣ���� �Լ�: ����ٴ� Ÿ���� �����մϴ�.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}