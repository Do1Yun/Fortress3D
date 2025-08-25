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
    public float deceleration = 10f;

    [Header("경사면 설정")]
    public float slopeAdaptSpeed = 10f;
    public float slopeRaycastLength = 1.5f;
    [Tooltip("오르막길에서 속도가 얼마나 감소할지 결정합니다.")]
    public float slopeResistance = 0.5f;
    [Tooltip("경사면 저항이 얼마나 부드럽게 적용될지 결정합니다.")]
    public float resistanceAdaptSpeed = 5f; // 저항값 전환 속도

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    [HideInInspector] public float currentStamina;

    private Image staminaImage;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private float currentSpeed = 0f;
    private float currentSpeedModifier = 1.0f; // 부드럽게 변하는 현재 저항값

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

        RaycastHit hit;
        Quaternion targetRotation;
        float targetSpeedModifier = 1.0f; // 이번 프레임의 목표 저항값

        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRaycastLength))
        {
            targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

            if (hit.normal.y < 0.99f && Mathf.Abs(forwardInput) > 0.1f)
            {
                float verticalDirection = Vector3.Dot(transform.forward, Vector3.up);
                float steepness = 1.0f - hit.normal.y;
                bool isUphill = (forwardInput > 0 && verticalDirection > 0) || (forwardInput < 0 && verticalDirection < 0);

                if (isUphill)
                {
                    targetSpeedModifier = Mathf.Max(1.0f - steepness * slopeResistance * 5f, 0.1f);
                }
            }
        }
        else
        {
            targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        // --- 핵심: 저항값을 부드럽게 변경 ---
        currentSpeedModifier = Mathf.Lerp(currentSpeedModifier, targetSpeedModifier, resistanceAdaptSpeed * Time.deltaTime);

        if (currentStamina > 0)
        {
            transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime, Space.World);

            if (Mathf.Abs(forwardInput) > 0.1f)
            {
                // 부드러워진 저항값을 사용
                currentSpeed += forwardInput * acceleration * currentSpeedModifier * Time.deltaTime;
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
            }

            // 부드러워진 저항값을 사용
            float currentMaxSpeed = maxSpeed * currentSpeedModifier;
            float currentMinSpeed = -maxSpeed / 2 * currentSpeedModifier;
            currentSpeed = Mathf.Clamp(currentSpeed, currentMinSpeed, currentMaxSpeed);

            if (Mathf.Abs(forwardInput) > 0.1f || Mathf.Abs(turnInput) > 0.1f)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                UpdateStaminaUI();
            }
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, slopeAdaptSpeed * Time.deltaTime);

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