using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("?��?�� ?��?��")]
    public float acceleration = 15.0f;
    public float maxSpeed = 10.0f;
    public float turnSpeed = 100.0f;
    public float gravityValue = -9.81f;
    public float deceleration = 10f;

    [Header("경사�? ?��?��")]
    public float slopeAdaptSpeed = 10f;
    public float slopeRaycastLength = 1.5f;
    public float slopeResistance = 0.5f;
    public float resistanceAdaptSpeed = 5f;

    [Header("?��?��미너 ?��?��")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float basicStamina = 100f;
    [HideInInspector] public float currentStamina;

    private Image staminaImage;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private float currentSpeed = 0f;
    private float currentSpeedModifier = 1.0f;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentStamina = maxStamina;
    }

    public void SetUIReferences(Image staminaImg)
    {
        staminaImage = staminaImg;
        UpdateStaminaUI();
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    /// <summary>
    /// ?��?�� ?��?�� ?���?: ?��보드 ?��?��, 중력, 경사�? ?��?��?�� 모두 처리?��?��?��.
    /// </summary>
    public void HandleMovement()
    {
        ApplyHorizontalMovement();
        ApplyGravityAndSlope();
    }

    /// <summary>
    /// ?��?�� ?��?�� ?��?�� ?�� ?���?: 중력�? 경사�? ?��?���? 처리?��?�� ?��?��리�?? �??��?��?��.
    /// </summary>
    public void UpdatePhysics()
    {
        // ?��?���? ?��?��?�� 0?���? 줄입?��?��.
        currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
        ApplyGravityAndSlope();
    }

    private void ApplyGravityAndSlope()
    {
        // 중력 ?��?��
        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravityValue * Time.deltaTime;

        // 경사�? ?��?��
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRaycastLength))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * transform.rotation, slopeAdaptSpeed * Time.deltaTime);
        }

        // 최종 ?��?�� ?��?��
        Vector3 moveDirection = transform.forward * currentSpeed;
        characterController.Move((moveDirection + playerVelocity) * Time.deltaTime);
    }

    private void ApplyHorizontalMovement()
    {
        float forwardInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        transform.Rotate(0, turnInput * turnSpeed * Time.deltaTime, 0);

        if (currentStamina > 0)
        {
            RaycastHit hit;
            float slopeModifier = 1.0f;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 1.5f))
            {
                float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
                if (slopeAngle > characterController.slopeLimit && Vector3.Dot(transform.forward, Vector3.up) < 0)
                {
                    slopeModifier = Mathf.Clamp(1.0f - (slopeAngle / 90f) * slopeResistance, 0.1f, 1.0f);
                }
            }
            currentSpeedModifier = Mathf.Lerp(currentSpeedModifier, slopeModifier, resistanceAdaptSpeed * Time.deltaTime);

            if (Mathf.Abs(forwardInput) > 0.1f)
            {
                currentSpeed += forwardInput * acceleration * currentSpeedModifier * Time.deltaTime;
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
            }

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
            currentStamina = 0;
            currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
        }
    }

    public void UpdateStaminaUI()
    {
        if (staminaImage != null)
        {
            staminaImage.fillAmount = currentStamina / maxStamina;
        }
    }
}