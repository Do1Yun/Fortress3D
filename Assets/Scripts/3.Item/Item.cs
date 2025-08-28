using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Health,
    Speed,
    Attack
}

public class Item : MonoBehaviour
{
    public ItemType itemtype;

    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject);
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
