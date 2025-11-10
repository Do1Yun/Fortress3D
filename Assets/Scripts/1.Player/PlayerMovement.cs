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
    public float speedMultiplier = 1;

    [Header("경사면 설정")]
    public float slopeAdaptSpeed = 10f;
    public float slopeRaycastLength = 1.5f;
    public float forwardRaycastLength = 1.5f;
    public LayerMask groundLayer; // 지면/벽 감지 레이어

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float currentStamina;

    private float baseMaxStamina;
    private Image staminaImage;
    private CharacterController characterController;

    // 이동 상태
    private Vector3 playerVelocity;
    private float currentSpeed = 0f;
    private bool isWallClimbing = false;
    private Vector3 wallNormal;

    // --- 넉백 관련 ---
    private Vector3 knockbackVelocity = Vector3.zero;
    private float knockbackDamping = 5.0f;

    // ===================== 리스폰/충돌 트리거 설정 =====================
    [Header("리스폰/충돌 트리거")]
    public Transform respawnPoint;
    public Collider respawnTrigger;
    public float respawnLift = 0.08f;
    public bool copyRespawnRotation = false;
    // ================================================================

    // 플레이어가 외부 힘을 받을 수 있도록 하는 함수
    public void ApplyKnockback(Vector3 direction, float force)
    {
        // y축으로는 밀려나지 않도록 방향을 수평으로 고정합니다.
        direction.y = 0;
        knockbackVelocity = direction.normalized * force;
    }

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

    public void ResetSpeed()
    {
        speedMultiplier = 1;
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

        // 전방 벽 감지
        if (Physics.Raycast(transform.position, transform.forward, out hit, forwardRaycastLength, groundLayer))
        {
            Debug.Log("Forward Raycast Detected Object: " + hit.collider.name);
            if (hit.normal.y < 0.1f && Input.GetAxis("Vertical") > 0.1f && !isWallClimbing && characterController.isGrounded)
            {
                isWallClimbing = true;
                wallNormal = hit.normal;
                transform.rotation = Quaternion.LookRotation(-wallNormal);
                return;
            }
        }

        // 바닥 경사 감지
        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRaycastLength, groundLayer))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * transform.rotation, slopeAdaptSpeed * Time.deltaTime);

            moveDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized * currentSpeed;
            groundFound = true;
        }

        if (!groundFound)
        {
            moveDirection = transform.forward * currentSpeed;
        }

        // 중력
        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravityValue * Time.deltaTime;

        // 넉백 감쇠
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);

        // 최종 이동
        characterController.Move((moveDirection + playerVelocity + knockbackVelocity) * Time.deltaTime * speedMultiplier);
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

    // ===================== 리스폰 =====================
    public void Respawn()
    {
        if (respawnPoint == null)
        {
            Debug.LogWarning("[Respawn] respawnPoint가 비어있습니다.");
            return;
        }

        Vector3 targetPos = respawnPoint.position + Vector3.up * Mathf.Max(0f, respawnLift);
        Quaternion targetRot = copyRespawnRotation ? respawnPoint.rotation : transform.rotation;

        bool prevEnabled = characterController.enabled;
        characterController.enabled = false;

        // 내부 상태 초기화
        playerVelocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;
        currentSpeed = 0f;
        isWallClimbing = false;

        transform.SetPositionAndRotation(targetPos, targetRot);

        characterController.enabled = prevEnabled;

        Debug.Log($"[Respawn] {name} → {respawnPoint.name} @ {targetPos}");
    }

    // ===================== 충돌 → 리스폰 트리거 =====================

    private void OnTriggerEnter(Collider other)
    {
        if (respawnTrigger != null && other == respawnTrigger)
        {
            Respawn();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (respawnTrigger != null && hit.collider == respawnTrigger)
        {
            Respawn();
        }
    }
}
