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
    public float wallClimbSpeed = 5.0f;

    [Header("경사면 설정")]
    public float slopeAdaptSpeed = 10f;
    public float slopeRaycastLength = 1.5f;
    public float forwardRaycastLength = 1.5f;
    public LayerMask groundLayer; // 지면을 감지하기 위한 레이어 마스크 변수

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    [HideInInspector] public float currentStamina;

    private float baseMaxStamina;

    private Image staminaImage;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private float currentSpeed = 0f;

    private bool isWallClimbing = false;
    private Vector3 wallNormal;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        baseMaxStamina = maxStamina;
        currentStamina = maxStamina;
    }

    public void SetUIReferences(Image staminaImg)
    {
        staminaImage = staminaImg;
        UpdateStaminaUI();
    }

    public void ResetStamina()
    {
        maxStamina = baseMaxStamina;
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    public void HandleMovement()
    {
        if (isWallClimbing)
        {
            HandleWallClimbing();
        }
        else
        {
            ApplyHorizontalMovement();
            ApplyGravityAndSlope();
        }
    }

    public void UpdatePhysics()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
        ApplyGravityAndSlope();
    }

    private void ApplyGravityAndSlope()
    {
        RaycastHit hit;
        Vector3 moveDirection = Vector3.zero;
        bool groundFound = false;

        if (Physics.Raycast(transform.position, transform.forward, out hit, forwardRaycastLength, groundLayer))
        {
            // ★[수정] 앞으로 쏘는 레이캐스트에서만 감지된 레이어 이름을 콘솔에 출력합니다.
            Debug.Log("Forward Raycast Detected Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

            if (hit.normal.y < 0.1f && Input.GetAxis("Vertical") > 0.1f && !isWallClimbing && characterController.isGrounded)
            {
                isWallClimbing = true;
                wallNormal = hit.normal;
                transform.rotation = Quaternion.LookRotation(-wallNormal);
                return;
            }

            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * transform.rotation, slopeAdaptSpeed * Time.deltaTime);

            moveDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized * currentSpeed;
            groundFound = true;
        }

        if (!groundFound && Physics.Raycast(transform.position, Vector3.down, out hit, slopeRaycastLength, groundLayer))
        {
            // 아래 방향 레이캐스트의 디버그 로그는 제거되었습니다.
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * transform.rotation, slopeAdaptSpeed * Time.deltaTime);

            moveDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized * currentSpeed;
            groundFound = true;
        }

        if (!groundFound)
        {
            moveDirection = transform.forward * currentSpeed;
        }

        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravityValue * Time.deltaTime;

        characterController.Move((moveDirection + playerVelocity) * Time.deltaTime);
    }

    private void HandleWallClimbing()
    {
        if (Input.GetAxis("Vertical") < -0.1f || characterController.isGrounded)
        {
            isWallClimbing = false;
            return;
        }

        float forwardInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-wallNormal), slopeAdaptSpeed * Time.deltaTime);

        Vector3 wallMove = transform.up * forwardInput * wallClimbSpeed * Time.deltaTime;
        wallMove += wallNormal * 0.1f;

        characterController.Move(wallMove);

        if (Mathf.Abs(forwardInput) > 0.1f || Mathf.Abs(turnInput) > 0.1f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            UpdateStaminaUI();
        }

        if (currentStamina <= 0)
        {
            currentStamina = 0;
            isWallClimbing = false;
        }
    }

    private void ApplyHorizontalMovement()
    {
        float forwardInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        transform.Rotate(0, turnInput * turnSpeed * Time.deltaTime, 0);

        if (currentStamina > 0)
        {
            if (Mathf.Abs(forwardInput) > 0.1f)
            {
                currentSpeed += forwardInput * acceleration * Time.deltaTime;
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
            }

            currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2, maxSpeed);

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