using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이 스크립트가 부착된 게임 오브젝트에 CharacterController 컴포넌트가 자동으로 추가됩니다.

public enum ItemType
{
    Health,
    Range,
    TurnOff
}

[RequireComponent(typeof(CharacterController))]
public class Item : MonoBehaviour
{
    public ItemType itemtype;
    public Sprite itemIcon;

    // 중력 값을 설정합니다.
    public float gravityValue = -9.81f;

    private GameManager gameManager;
    private CharacterController characterController;
    private MeshRenderer meshRenderer;
    private Vector3 itemVelocity;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");
    }

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        characterController = GetComponent<CharacterController>();

        itemtype = (ItemType)Random.Range(0, System.Enum.GetValues(typeof(ItemType)).Length);
        switch (itemtype)
        {
            case ItemType.Health: meshRenderer.material.color = Color.red; break;
            case ItemType.Range: meshRenderer.material.color = Color.green; break;
            case ItemType.TurnOff: meshRenderer.material.color = Color.blue; break;
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
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player.ItemList.Count < player.maxItemCount)
            {
                // 아이템 추가
                player.ItemList.Add(itemtype);

                // UI 갱신 호출
                player.UpdateItemSelectionUI();

                Destroy(gameObject);
            }
            else
            {
               Debug.Log("아이템 슬롯이 가득 찼습니다!");
            }
        }
        else if (other.CompareTag("Bullet"))
        {
            PlayerController player = gameManager.players[gameManager.currentPlayerIndex];

            if (player.ItemList.Count < player.maxItemCount)
            {
                // 아이템 추가
                player.ItemList.Add(itemtype);

                // UI 갱신 호출
                player.UpdateItemSelectionUI();

                Destroy(gameObject);
            }
            else
            {
               Debug.Log("아이템 슬롯이 가득 찼습니다!");
            }
        }
    }

    // PlayerController.cs 에 코드 옮김
    // public void ApplyEffect_GameObject(ItemType item)
    // {
    //     PlayerMovement playerMovement = gameManager.players_movement[gameManager.currentPlayerIndex];
    //     PlayerController Player = gameManager.players[gameManager.currentPlayerIndex];
    //     PlayerController nextPlayer = gameManager.players[(gameManager.currentPlayerIndex + 1) % 2];

    //     switch (item)
    //     {
    //         case ItemType.Health:
    //             playerMovement.maxStamina *= 2;
    //             // playerMovement.staminaDrainRate /= 2; 로 할까 고민중
    //             break;

    //         case ItemType.Range:
    //             Player.ExplosionRange *= 2;
    //             break;

    //         case ItemType.TurnOff:
    //             nextPlayer.trajectory.isPainted = false;
    //             break;
    //     }
    // }
}