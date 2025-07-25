using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private enum PlayerState { Moving, AimingVertical, AimingHorizontal, SettingPower, Waiting }
    private PlayerState currentState;

    [Header("플레이어 설정")]
    public int playerID;
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 10.0f;

    [Header("스테미너 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    private float currentStamina;
    public Image staminaImage;

    [Header("조준 관련")]
    public Transform turretPivot;
    public Transform cannonBarrel;
    public Transform firePoint;
    public float verticalAimSpeed = 20f;
    public float horizontalAimSpeed = 30f;
    public float maxAimAngle = 45.0f;
    public float minAimAngle = -20.0f;

    [Header("발사 관련")]
    public GameObject projectilePrefab;
    public float minLaunchPower = 10f;
    public float maxLaunchPower = 50f;
    public float powerGaugeSpeed = 30f;
    public Image powerImage;
    public TextMeshProUGUI powerText;

    [Header("UI 설정")]
    public float stageTimeLimit = 5.0f;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Image timerImage;

    // --- 내부 상태 변수 ---
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private float gravityValue = -9.81f;
    private float currentVerticalAngle = 0.0f;
    private float currentLaunchPower;
    private bool isPowerIncreasing = true;
    private float currentStageTimer;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void StartTurn()
    {
        currentStamina = maxStamina;
        currentState = PlayerState.Moving;
        UpdateUIForState(currentState);
        Debug.Log("Player " + playerID + "의 턴 시작! [이동 모드]");
    }

    public void EndTurn()
    {
        currentState = PlayerState.Waiting;
        UpdateUIForState(currentState);
    }

    void Update()
    {
        if (currentState == PlayerState.Waiting) return;

        switch (currentState)
        {
            case PlayerState.Moving:
                HandleMovement();
                break;
            case PlayerState.AimingVertical:
                HandleStage(HandleVerticalAim, PlayerState.AimingHorizontal);
                break;
            case PlayerState.AimingHorizontal:
                HandleStage(HandleHorizontalAim, PlayerState.SettingPower);
                break;
            case PlayerState.SettingPower:
                HandleStage(HandlePowerSetting, PlayerState.Waiting, true);
                break;
        }
    }

    void HandleMovement()
    {
        Transform camTransform = Camera.main.transform;
        if (characterController.isGrounded && playerVelocity.y < 0) playerVelocity.y = 0f;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 moveDirection = (camTransform.forward * v) + (camTransform.right * h);
        moveDirection.y = 0;
        moveDirection.Normalize();

        if (moveDirection.magnitude > 0.1f && currentStamina > 0)
        {
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (staminaImage != null) staminaImage.fillAmount = currentStamina / maxStamina;
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection, Vector3.up), rotationSpeed * Time.deltaTime);
            }
        }
        playerVelocity.y += gravityValue * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);

        if (currentStamina <= 0)
        {
            EnterFiringMode(PlayerState.AimingVertical);
        }
    }

    void EnterFiringMode(PlayerState nextState)
    {
        currentState = nextState;
        currentStageTimer = stageTimeLimit;
        UpdateUIForState(currentState);
        Debug.LogFormat("발사 모드 진입: [{0}]", nextState);

        if (nextState == PlayerState.SettingPower)
        {
            currentLaunchPower = minLaunchPower;
            isPowerIncreasing = true;
        }
    }

    void HandleStage(System.Action stageAction, PlayerState nextState, bool isFinalStage = false)
    {
        currentStageTimer -= Time.deltaTime;
        if (timerText != null) timerText.text = $"{currentStageTimer:F1}";
        if (timerImage != null) timerImage.fillAmount = currentStageTimer / stageTimeLimit;

        stageAction();

        if (currentStageTimer <= 0 || Input.GetKeyDown(KeyCode.Space))
        {
            if (isFinalStage) Fire(currentLaunchPower);
            else EnterFiringMode(nextState);
        }
    }

    void HandleVerticalAim()
    {
        if (Input.GetKey(KeyCode.I)) currentVerticalAngle -= verticalAimSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.K)) currentVerticalAngle += verticalAimSpeed * Time.deltaTime;
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minAimAngle, maxAimAngle);
        cannonBarrel.localEulerAngles = new Vector3(currentVerticalAngle, 0, 0);
    }

    void HandleHorizontalAim()
    {
        if (Input.GetKey(KeyCode.J)) turretPivot.Rotate(Vector3.up, -horizontalAimSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.L)) turretPivot.Rotate(Vector3.up, horizontalAimSpeed * Time.deltaTime);
    }

    void HandlePowerSetting()
    {
        if (isPowerIncreasing)
        {
            currentLaunchPower += powerGaugeSpeed * Time.deltaTime;
            if (currentLaunchPower >= maxLaunchPower) isPowerIncreasing = false;
        }
        else
        {
            currentLaunchPower -= powerGaugeSpeed * Time.deltaTime;
            if (currentLaunchPower <= minLaunchPower) isPowerIncreasing = true;
        }

        if (powerImage != null) powerImage.fillAmount = (currentLaunchPower - minLaunchPower) / (maxLaunchPower - minLaunchPower);
        if (powerText != null) powerText.text = $"Power: {currentLaunchPower:F0}";
    }

    void Fire(float finalLaunchPower)
    {
        EndTurn(); 

        if (projectilePrefab == null || firePoint == null) return;
        Debug.LogFormat("Player {0} 발사! (파워: {1})", playerID, finalLaunchPower);
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(firePoint.forward * finalLaunchPower, ForceMode.Impulse);
        GameManager.instance.SwitchToNextTurn();
    }
    
    void UpdateUIForState(PlayerState state)
    {
        if (staminaImage != null) staminaImage.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (timerImage != null) timerImage.gameObject.SetActive(false);
        if (powerImage != null) powerImage.gameObject.SetActive(false);
        if (powerText != null) powerText.gameObject.SetActive(false);

        switch (state)
        {
            case PlayerState.Moving:
                if (staminaImage != null)
                {
                    staminaImage.gameObject.SetActive(true);
                    staminaImage.fillAmount = currentStamina / maxStamina;
                }
                break;
            case PlayerState.AimingVertical:
                if (timerImage != null) timerImage.gameObject.SetActive(true);
                if (timerText != null) timerText.gameObject.SetActive(true);
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "Vertical Aim";
                }
                break;
            case PlayerState.AimingHorizontal:
                if (timerImage != null) timerImage.gameObject.SetActive(true);
                if (timerText != null) timerText.gameObject.SetActive(true);
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "Horizontal Aim";
                }
                break;
            case PlayerState.SettingPower:
                if (timerImage != null) timerImage.gameObject.SetActive(true);
                if (timerText != null) timerText.gameObject.SetActive(true);
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "Set Power";
                }
                if (powerImage != null) powerImage.gameObject.SetActive(true);
                if (powerText != null) powerText.gameObject.SetActive(true);
                break;
        }
    }
}