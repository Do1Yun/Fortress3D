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

        RaycastHit hit;
        Quaternion targetRotation;
        float speedModifier = 1.0f;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRaycastLength))
        {
            targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

            // --- 오르막길 저항 로직 (단순화) ---
            // 경사면이고, 앞으로 가려하고, '오르막' 방향일 때만 속도 저하
            if (hit.normal.y < 0.99f && forwardInput > 0.1f && Vector3.Dot(transform.forward, Vector3.up) > 0)
            {
                float steepness = 1.0f - hit.normal.y;
                speedModifier = Mathf.Max(1.0f - steepness * slopeResistance * 5f, 0.1f);
            }
            // 내리막길이나 평지에서는 speedModifier가 기본값 1.0f를 유지합니다.
        }
        else
        {
            targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        if (currentStamina > 0)
        {
            transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime, Space.World);

            if (Mathf.Abs(forwardInput) > 0.1f)
            {
                currentSpeed += forwardInput * acceleration * speedModifier * Time.deltaTime;
                currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2, maxSpeed * speedModifier);
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
            }

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