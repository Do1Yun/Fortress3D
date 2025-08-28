using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �� ��ũ��Ʈ�� ������ ���� ������Ʈ�� CharacterController ������Ʈ�� �ڵ����� �߰��˴ϴ�.

public enum ItemType
{
    Health,
    Speed,
    Attack
}

[RequireComponent(typeof(CharacterController))]
public class Item : MonoBehaviour
{
    public ItemType itemtype;

    // �߷� ���� �����մϴ�.
    public float gravityValue = -9.81f;

    private CharacterController characterController;
    private MeshRenderer meshRenderer;
    private Vector3 itemVelocity;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        characterController = GetComponent<CharacterController>();

        itemtype = (ItemType)Random.Range(0, System.Enum.GetValues(typeof(ItemType)).Length);
        switch (itemtype)
        {
            case ItemType.Health: meshRenderer.material.color = Color.red; break;
            case ItemType.Speed: meshRenderer.material.color = Color.green; break;
            case ItemType.Attack: meshRenderer.material.color = Color.blue; break;
        }
    }

    void Update()
    {
        // �߷� ����
        if (characterController.isGrounded && itemVelocity.y < 0)
        {
            itemVelocity.y = -2f;
        }
        itemVelocity.y += gravityValue * Time.deltaTime;

        // ���� �̵� ����
        characterController.Move(itemVelocity * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Bullet"))
        {
            // Player�� �ƴ϶� ��ź�̸�, ��ź �߻��ڸ� �����ͼ� ȿ�� ���� ����
            GameObject target = other.gameObject;

            // ��ź�̸� �߻���(Player)�� ã�� ����
            // var bullet = other.GetComponent<Bullet>();
            // if (bullet != null)
            //     target = bullet.shooter; // shooter�� Bullet�� �߻��ڸ� �����ϴ� ����

            ApplyEffect(target);
            Destroy(gameObject);
        }
    }

    public void ApplyEffect(GameObject player)
    {
        // TankController tank = player.GetComponent<TankController>();

        // switch (itemType)
        // {
        //     case ItemType.Health:
        //         tank.hp = Mathf.Min(tank.maxHp, tank.hp + effectValue);
        //         break;

        //     case ItemType.Speed:
        //         tank.moveSpeed += effectValue;
        //         break;

        //     case ItemType.Attack:
        //         tank.attackPower += effectValue;
        //         break;
        // }
    }
}