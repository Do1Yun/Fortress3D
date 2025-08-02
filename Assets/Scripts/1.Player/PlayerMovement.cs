using UnityEngine;
using UnityEngine.UI; // Image 사용을 위해 추가

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 10.0f;
    public float gravityValue = -9.81f; // 중력 값

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    [HideInInspector] public float currentStamina; // 외부에서 읽기만 하도록 HideInInspector
    
    // UI 참조 (PlayerController에서 SetUIReferences를 통해 전달받음)
    private Image staminaImage;

    private CharacterController characterController;
    private Vector3 playerVelocity; // 플레이어의 현재 속도

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentStamina = maxStamina; // 초기 스태미나 설정
    }

    // PlayerController에서 UI 참조를 받아오기 위한 함수
    public void SetUIReferences(Image staminaImg)
    {
        staminaImage = staminaImg;
    }

    // 스태미나를 초기화하는 함수 (PlayerController의 StartTurn에서 호출)
    public void ResetStamina()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    // 이동 처리 로직
    public void HandleMovement()
    {
        // CharacterController가 지면에 닿아있고 하강 중이라면 Y 속도를 0으로 초기화
        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // 입력 값 가져오기
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 플레이어 기준으로 이동 방향 계산
        Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        moveDirection.y = 0; // Y축 이동은 중력으로만 처리
        moveDirection.Normalize(); // 대각선 이동 속도 보정

        // 이동 조건: 입력이 있고 스태미나가 남아있는 경우
        if (moveDirection.magnitude > 0.1f && currentStamina > 0)
        {
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            currentStamina -= staminaDrainRate * Time.deltaTime;
            UpdateStaminaUI();
            
            // 이동 방향으로 플레이어 회전 (부드럽게)
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection, Vector3.up), rotationSpeed * Time.deltaTime);
            }
        }
        
        // 항상 중력 적용
        playerVelocity.y += gravityValue * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    // 스태미나 UI 업데이트 함수
    public void UpdateStaminaUI()
    {
        if (staminaImage != null) staminaImage.fillAmount = currentStamina / maxStamina;
    }
}