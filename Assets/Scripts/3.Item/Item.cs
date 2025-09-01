using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �� ��ũ��Ʈ�� ������ ���� ������Ʈ�� CharacterController ������Ʈ�� �ڵ����� �߰��˴ϴ�.

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

    // �߷� ���� �����մϴ�.
    public float gravityValue = -9.81f;

    private GameManager gameManager;
    private CharacterController characterController;
    private MeshRenderer meshRenderer;
    private Vector3 itemVelocity;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager�� ã�� �� �����ϴ�!");
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
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        PlayerController Player = gameManager.players[gameManager.currentPlayerIndex];
        PlayerController nextPlayer = gameManager.players[(gameManager.currentPlayerIndex + 1) % 2];

        switch (itemtype)
        {
            case ItemType.Health:
                playerMovement.maxStamina *= 2;
                break;

            case ItemType.Range:
                Player.ExplosionRange *= 2;
                break;

            case ItemType.TurnOff:
                nextPlayer.trajectory.isPainted = false;
                break;
        }
    }
}