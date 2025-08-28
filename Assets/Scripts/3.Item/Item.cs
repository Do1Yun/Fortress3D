using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이 스크립트가 부착된 게임 오브젝트에 CharacterController 컴포넌트가 자동으로 추가됩니다.

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

    // 중력 값을 설정합니다.
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
        // 중력 적용
        if (characterController.isGrounded && itemVelocity.y < 0)
        {
            itemVelocity.y = -2f;
        }
        itemVelocity.y += gravityValue * Time.deltaTime;

        // 최종 이동 적용
        characterController.Move(itemVelocity * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Bullet"))
        {
            // Player가 아니라 포탄이면, 포탄 발사자를 가져와서 효과 적용 가능
            GameObject target = other.gameObject;

            // 포탄이면 발사자(Player)를 찾는 예시
            // var bullet = other.GetComponent<Bullet>();
            // if (bullet != null)
            //     target = bullet.shooter; // shooter는 Bullet이 발사자를 참조하는 변수

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