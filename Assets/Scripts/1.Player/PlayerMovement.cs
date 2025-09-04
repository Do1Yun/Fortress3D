using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    // ★★★ 추가: 경사를 더 잘 오르기 위해 가속도를 높였습니다. ★★★
    public float acceleration = 1200f;
    public float maxSpeed = 8.0f;
    public float turnSpeed = 10f;
    public float decelerationForce = 10f;
    public float brakeForce = 20f;

    [Header("지형 적응")]
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayer;
    public float slopeAdaptForce = 50f;
    public float stickToGroundForce = 100f;

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    [HideInInspector] public float currentStamina;
    public float basicStamina = 100f;

    private Image staminaImage;
    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isGrounded;
    private Vector3 groundNormal;
    private bool isMovementTurn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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

    public void HandleMovement()
    {
        isMovementTurn = true;
        float forwardInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");
        moveInput = new Vector2(turnInput, forwardInput);

        if (currentStamina > 0 && moveInput.magnitude > 0.1f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            currentStamina = Mathf.Max(0, currentStamina);
        }
        UpdateStaminaUI();
    }

    public void UpdatePhysics()
    {
        isMovementTurn = false;
        moveInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        CheckGrounded();
        if (isGrounded)
        {
            if (isMovementTurn)
            {
                ApplyLocomotion();
                ApplyDeceleration();
            }
            else
            {
                ApplyBrakes();
            }

            AdaptToSlope();
            rb.AddForce(-groundNormal * stickToGroundForce);
        }
    }

    void ApplyLocomotion()
    {
        if (currentStamina <= 0) return;

        if (Mathf.Abs(moveInput.y) > 0.1f)
        {
            if (rb.velocity.magnitude < maxSpeed)
            {
                // ★★★ 핵심 수정: 힘의 방향을 지면과 평행하게 만들어 경사를 오르는 효율을 높입니다. ★★★
                Vector3 moveDirection = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
                Vector3 force = moveDirection * moveInput.y * acceleration;
                rb.AddForce(force, ForceMode.Acceleration);
            }
        }

        Vector3 targetAngularVelocity = transform.up * moveInput.x * turnSpeed;
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, targetAngularVelocity, Time.fixedDeltaTime * 15f);
    }

    void ApplyDeceleration()
    {
        if (isGrounded && moveInput.magnitude < 0.1f)
        {
            Vector3 oppositeForce = -rb.velocity * decelerationForce;
            rb.AddForce(oppositeForce, ForceMode.Acceleration);
        }
    }

    void ApplyBrakes()
    {
        rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * brakeForce);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * brakeForce);
    }

    void CheckGrounded()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer);

        if (isGrounded)
        {
            groundNormal = hit.normal;
        }
        else
        {
            groundNormal = Vector3.up;
        }
    }

    void AdaptToSlope()
    {
        Vector3 torque = Vector3.Cross(transform.up, groundNormal) * slopeAdaptForce;
        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    public void UpdateStaminaUI()
    {
        if (staminaImage != null)
        {
            staminaImage.fillAmount = currentStamina / maxStamina;
        }
    }
}

