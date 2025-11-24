using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Health,
    Range,
    TurnOff,
    Chasing
}

// CharacterController 대신 Rigidbody를 필수 컴포넌트로 지정합니다.
[RequireComponent(typeof(Rigidbody))]
public class Item : MonoBehaviour
{
    public ItemType itemtype;
    public Sprite itemIcon;

    // Rigidbody가 중력을 처리하므로 gravityValue와 itemVelocity 변수는 필요 없습니다.

    private GameManager gameManager;
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    [Header("오디오 설정")]
    [Tooltip("아이템이 생성될 때 재생할 중계 멘트")]
    public AudioClip itemCommentary;
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");
    }

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        // Rigidbody 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();

        itemtype = (ItemType)Random.Range(0, System.Enum.GetValues(typeof(ItemType)).Length);

        // 스크립트 시작 시 물리 설정 (Inspector에서 설정해도 되지만 안전을 위해 코드 추가)
        rb.useGravity = true; // 중력 켜기
        // 아이템이 땅에 떨어졌을 때 데굴데굴 굴러가거나 넘어지지 않게 하려면 아래 주석을 해제하세요.
        // rb.constraints = RigidbodyConstraints.FreezeRotation; 
    }

    void Update()
    {
        // Rigidbody를 사용하면 물리 엔진이 자동으로 중력과 충돌을 계산하므로
        // 여기서 이동 관련 코드를 작성할 필요가 없습니다.
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            // PlayerController가 있는지 확인 (안전 장치)
            if (player != null)
            {
                ProcessItem(player);
            }
        }
        else if (other.CompareTag("Bullet"))
        {
            // Bullet에 맞았을 때는 현재 턴인 플레이어에게 아이템 지급
            if (gameManager != null && gameManager.players.Count > 0)
            {
                PlayerController player = gameManager.players[gameManager.currentPlayerIndex];
                ProcessItem(player);
            }
        }
    }

    // 아이템 획득 로직이 중복되어 함수로 분리했습니다.
    private void ProcessItem(PlayerController player)
    {
        if (player.ItemList.Count < player.maxItemCount)
        {
            if (Random.value <= 0.33f) // 확률 (현재 100%로 설정됨)
            {
                if (gameManager != null && gameManager.announcerAudioSource != null && itemCommentary != null)
                {
                    // 기존 멘트가 있다면 끊고, 아이템 멘트를 즉시 재생
                    gameManager.announcerAudioSource.Stop();
                    gameManager.announcerAudioSource.PlayOneShot(itemCommentary);
                    Debug.Log("아이템 획득 멘트 재생!");
                }
            }
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