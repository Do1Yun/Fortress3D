using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))] // [추가] 오디오 소스 컴포넌트 필수 지정
public class PlayerMovement : MonoBehaviour
{
    private GameManager gameManager;

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
    public LayerMask groundLayer;

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float currentStamina;

    [Header("오디오 설정")]
    public AudioClip RespawnCommentary;

    // --- [추가] 이동 사운드 관련 변수 ---
    [Space(10)]
    public AudioClip engineSoundClip;   // 엔진 루프 사운드 (우웅~ 하는 소리)
    public float minPitch = 0.9f;       // 정지 상태일 때 피치
    public float maxPitch = 1.5f;       // 최고 속도일 때 피치
    public float pitchChangeSpeed = 2f; // 피치가 변하는 부드러움 정도
    private AudioSource movementAudioSource; // 내 탱크의 오디오 소스
    // -------------------------------

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

    [Header("리스폰/충돌 트리거")]
    public Transform respawnPoint;
    public Collider respawnTrigger;
    public float respawnLift = 0.08f;
    public bool copyRespawnRotation = false;

    public void ApplyKnockback(Vector3 direction, float force)
    {
        direction.y = 0;
        knockbackVelocity = direction.normalized * force;
    }

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");
        characterController = GetComponent<CharacterController>();

        // [추가] 오디오 소스 가져오기 및 초기화
        movementAudioSource = GetComponent<AudioSource>();
        if (movementAudioSource == null)
        {
            // 만약 없으면 코드에서 추가
            movementAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // [추가] 오디오 설정 적용
        movementAudioSource.clip = engineSoundClip;
        movementAudioSource.loop = true; // 엔진 소리는 계속 돌아야 함
        movementAudioSource.playOnAwake = true;
        movementAudioSource.spatialBlend = 1.0f; // 3D 사운드
        movementAudioSource.Play(); // 소리 재생 시작

        baseMaxStamina = maxStamina;
        currentStamina = maxStamina;
    }

    // [추가] 매 프레임마다 소리 조절을 위해 Update 추가
    void Update()
    {
        // 입력 및 이동 처리 함수가 외부에서 불리는 구조라면 이 Update는 필요 없을 수 있으나,
        // 보통 소리 갱신은 매 프레임 이루어져야 부드럽습니다.
        HandleEngineAudio();
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

    // [추가] 엔진 사운드 처리 함수
    private void HandleEngineAudio()
    {
        if (movementAudioSource == null || engineSoundClip == null) return;

        // 현재 속도가 0이면 정지(minPitch), 최고속도면 maxPitch로 목표 설정
        float targetPitch = minPitch;

        // 속도 비율 계산 (절댓값 사용)
        float speedRatio = Mathf.Abs(currentSpeed) / maxSpeed;

        // 벽을 타고 있거나 움직이는 중이면 피치를 높임
        if (isWallClimbing || Mathf.Abs(currentSpeed) > 0.1f)
        {
            targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
        }

        // 현재 피치를 목표 피치로 부드럽게 변경
        movementAudioSource.pitch = Mathf.Lerp(movementAudioSource.pitch, targetPitch, pitchChangeSpeed * Time.deltaTime);
    }

    public void UpdatePhysics()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * Time.deltaTime);
        ApplyGravityAndSlope();
    }

    // ... (이하 기존 코드와 동일: ApplyGravityAndSlope, HandleWallClimbing, ApplyHorizontalMovement 등) ...

    private void ApplyGravityAndSlope()
    {
        RaycastHit hit;
        Vector3 moveDirection = Vector3.zero;
        bool groundFound = false;

        if (Physics.Raycast(transform.position, transform.forward, out hit, forwardRaycastLength, groundLayer))
        {
            // Debug.Log 삭제 혹은 유지
            if (hit.normal.y < 0.1f && Input.GetAxis("Vertical") > 0.1f && !isWallClimbing && characterController.isGrounded)
            {
                isWallClimbing = true;
                wallNormal = hit.normal;
                transform.rotation = Quaternion.LookRotation(-wallNormal);
                return;
            }
        }

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

        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravityValue * Time.deltaTime;

        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);

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

    // ... (나머지 UI 및 리스폰 함수 동일) ...
    public void UpdateStaminaUI()
    {
        if (staminaImage != null)
        {
            staminaImage.fillAmount = currentStamina / maxStamina;
        }
    }

    public void Respawn()
    {
        if (Random.value <= 1.0f)
        {
            if (gameManager != null && gameManager.announcerAudioSource != null && RespawnCommentary != null)
            {
                gameManager.announcerAudioSource.Stop();
                gameManager.announcerAudioSource.PlayOneShot(RespawnCommentary);
            }
        }
        if (respawnPoint == null)
        {
            Debug.LogWarning("[Respawn] respawnPoint가 비어있습니다.");
            return;
        }

        Vector3 targetPos = respawnPoint.position + Vector3.up * Mathf.Max(0f, respawnLift);
        Quaternion targetRot = copyRespawnRotation ? respawnPoint.rotation : transform.rotation;

        bool prevEnabled = characterController.enabled;
        characterController.enabled = false;

        playerVelocity = Vector3.zero;
        knockbackVelocity = Vector3.zero;
        currentSpeed = 0f;
        isWallClimbing = false;

        transform.SetPositionAndRotation(targetPos, targetRot);

        characterController.enabled = prevEnabled;

        Debug.Log($"[Respawn] {name} → {respawnPoint.name} @ {targetPos}");
    }

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