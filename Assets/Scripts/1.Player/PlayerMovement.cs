using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float acceleration = 15.0f;
    public float maxSpeed = 10.0f;
    public float turnSpeed = 100.0f;
    public float gravityValue = -9.81f;

    [Header("경사면 설정")]
    public float slopeAdaptSpeed = 10f;      // 차체가 경사면에 맞춰 기울어지는 속도
    public float slopeRaycastLength = 1.5f;  // 경사면을 감지할 광선의 길이

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    [HideInInspector] public float currentStamina;

    private Image staminaImage;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private float currentSpeed = 0f;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentStamina = maxStamina;
    }

    public void SetUIReferences(Image staminaImg)
    {
        staminaImage = staminaImg;
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    public void HandleMovement()
    {
        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        float forwardInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        if (currentStamina > 0)
        {
            transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime, Space.World);
            currentSpeed += forwardInput * acceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2, maxSpeed);

            if (Mathf.Abs(forwardInput) > 0.1f || Mathf.Abs(turnInput) > 0.1f)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                UpdateStaminaUI();
            }
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * 2);
        }

        // --- 수정된 차체 기울기 로직 ---
        RaycastHit hit;
        Quaternion targetRotation;

        // 1. 월드 기준 아래 방향으로 광선을 쏴서 바닥 정보를 가져옴
        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRaycastLength))
        {
            // 바닥이 감지되면, 목표 회전값을 바닥의 기울기에 맞춤
            targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        }
        else
        {
            // 바닥이 없으면(공중), 차체를 수평으로 되돌리는 것을 목표로 함
            // 현재 y축 회전(방향)은 유지하면서 x, z축 기울기만 0으로 만듦
            targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        // 2. Slerp를 사용해 계산된 목표 회전값으로 부드럽게 차체를 회전시킴
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, slopeAdaptSpeed * Time.deltaTime);

        // 이동 및 중력 적용
        Vector3 moveDirection = transform.forward * currentSpeed;
        characterController.Move(moveDirection * Time.deltaTime);

        playerVelocity.y += gravityValue * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    public void UpdateStaminaUI()
    {
        if (staminaImage != null) staminaImage.fillAmount = currentStamina / maxStamina;
    }
}