using UnityEngine;
using UnityEngine.UI; // UI ������Ʈ ������ ���� �߰�
using TMPro;
public class PlayerController : MonoBehaviour

{
    // �÷��̾��� ���¸� ��Ÿ���� enum ����
    public enum PlayerState { Moving, AimingVertical, AimingHorizontal, SettingPower, Waiting, Firing }
    private PlayerState currentState;

    [Header("�÷��̾� �⺻ ����")]
    public int playerID;

    [Header("UI ���� (���� ����: UI Manager�� �и� ����)")]
    // ��� UI ������ �� ��ũ��Ʈ���� �����ϰų� PlayerUIUpdater�� �ű� �� �ֽ��ϴ�.
    public Image staminaImage;
    public Image powerImage;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Image timerImage;

    [Header("���º� �ð� ����")]
    public float stageTimeLimit = 5.0f; // �� ����/�Ŀ� ���� �ܰ��� �ð� ����

    // ���ҵ� ��� ��ũ��Ʈ�鿡 ���� ����
    private PlayerMovement playerMovement;
    private PlayerAiming playerAiming;
    private PlayerShooting playerShooting;
    // private PlayerUIUpdater playerUIUpdater; // UI ������Ʈ�� ���� ��ũ��Ʈ�� �и��� ���

    private float currentStageTimer; // ���� �ܰ��� ���� �ð�

    // ī�޶� ��Ʈ�ѷ� ����
    private CameraController mainCameraController;

    void Awake()
    {
        // �ʿ��� ������Ʈ ���� ��������
        playerMovement = GetComponent<PlayerMovement>();
        playerAiming = GetComponent<PlayerAiming>();
        playerShooting = GetComponent<PlayerShooting>();
        // playerUIUpdater = GetComponent<PlayerUIUpdater>(); // UI Updater�� �и��ߴٸ�

        // �� ��� ��ũ��Ʈ�� �ùٸ��� �Ҵ�Ǿ����� Ȯ��
        if (playerMovement == null || playerAiming == null || playerShooting == null)
        {
            Debug.LogError("PlayerController�� �ʿ��� ��� ��ũ��Ʈ(Movement, Aiming, Shooting)�� ��� �Ҵ���� �ʾҽ��ϴ�.", this);
            enabled = false; // ��ũ��Ʈ ��Ȱ��ȭ
        }
    }

    void Start()
    {
        // ī�޶� ��Ʈ�ѷ��� ���� �ϳ��� �����Ѵٰ� ����
        mainCameraController = Camera.main.GetComponent<CameraController>();
        if (mainCameraController == null)
        {
            Debug.LogError("���� CameraController�� �ִ� ���� ī�޶� ã�� �� �����ϴ�. ī�޶� ����� ���ѵ� �� �ֽ��ϴ�.");
        }

        // �ʱ� UI ���� (�� ��� ��ũ��Ʈ�� UI ������ ����)
        // �� �κ��� PlayerUIUpdater�� ����� GameManager �̺�Ʈ�� �����Ͽ� ó���ϴ� ���� �� �����ϴ�.
        // ����� ���ǻ� ���⼭ �����մϴ�.
        playerMovement.SetUIReferences(staminaImage);
        playerShooting.SetUIReferences(powerImage, powerText);

        // GameManager�� �� ����/���� �̺�Ʈ�� ���� (PlayerController�� ���� �޾� ó��)
        // GameManager�� ���� ��� �÷��̾�� StartTurn/EndTurn�� ���� ȣ���մϴ�.
    }

    void Update()
    {
        // ��� ���̰ų� �߻� ���� ���� �Է� �� ���� ��ȯ�� ó������ �ʽ��ϴ�.
        if (currentState == PlayerState.Waiting || currentState == PlayerState.Firing) return;

        // UI Ÿ�̸� ������Ʈ (�̵� ��� ����)
        if (currentState != PlayerState.Moving)
        {
            currentStageTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = $"{currentStageTimer:F1}";
            if (timerImage != null) timerImage.fillAmount = currentStageTimer / stageTimeLimit;

            // Ÿ�̸Ӱ� 0�� �Ǹ� ���� �ܰ�� �ڵ� ��ȯ (�߻� �ܰ�� �ڵ� �߻�)
            if (currentStageTimer <= 0)
            {
                TransitionToNextStage(true); // �ð��� �� �� ���
                return; // �̹� ��ȯ�����Ƿ� ���� �������� ������ Update ������ ��ŵ
            }
        }

        // ���� ���¿� ���� ��� ��ũ��Ʈ�� �޼��� ȣ��
        switch (currentState)
        {
            case PlayerState.Moving:
                playerMovement.HandleMovement();
                if (Input.GetKeyDown(KeyCode.Space) || playerMovement.currentStamina <= 0) // ���¹̳� �� �� �ڵ� ��ȯ
                {
                    TransitionToNextStage(false);
                }
                break;
            case PlayerState.AimingVertical:
                playerAiming.HandleVerticalAim();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.AimingHorizontal:
                playerAiming.HandleHorizontalAim();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.SettingPower:
                playerShooting.HandlePowerSetting();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(true);
                break;
        }
    }

    // ���� ���۵� �� GameManager�κ��� ȣ��˴ϴ�.
    public void StartTurn()
    {
        playerMovement.ResetStamina(); // ���¹̳� �ʱ�ȭ
        SetPlayerState(PlayerState.Moving); // �� ���� �� �̵� ���� ����
        Debug.Log($"Player {playerID}�� �� ����! (���� ����: {currentState})");
    }

    // ���� ����� �� GameManager�κ��� ȣ��˴ϴ�.
    public void EndTurn()
    {
        SetPlayerState(PlayerState.Waiting); // �� ���� �� ��� ���� ����
        Debug.Log($"Player {playerID}�� �� ����! (���� ����: {currentState})");
    }

    // �÷��̾��� ���� ���¸� �����ϰ�, �׿� ���� UI �� ī�޶� ������Ʈ�մϴ�.
    void SetPlayerState(PlayerState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"Player {playerID} ���� ����: {newState}");

        UpdateUIForState(currentState); // UI ������Ʈ (PlayerUIUpdater�� �и��� ��)

        // ���� ��ȯ �� �ʱ�ȭ �۾�
        currentStageTimer = stageTimeLimit; // Ÿ�̸� �ʱ�ȭ

        // ī�޶� ��� ��ȯ (CameraController�� PlayerController�� �и��Ǿ� �����Ƿ�)
        if (mainCameraController != null)
        {
            if (newState == PlayerState.Moving)
            {
                mainCameraController.SetTarget(this.transform); // �÷��̾� ��ü�� ����ٴϵ��� ����
                mainCameraController.ToggleAimMode(false); // �⺻ ī�޶� ���
            }
            else if (newState == PlayerState.AimingVertical ||
                     newState == PlayerState.AimingHorizontal ||
                     newState == PlayerState.SettingPower)
            {
                mainCameraController.SetTarget(this.transform); // ������ �÷��̾ ���󰡵�
                mainCameraController.ToggleAimMode(true, playerAiming.turretPivot); // ���� ��� �� ��ž�� �ٶ󺸵��� ����
            }
            else if (newState == PlayerState.Firing)
            {
                // ��ź �߻� �� ī�޶�� GameManager���� Projectile�� ���󰡵��� ������ ����
                // ���⼭�� ��� �⺻ ���� ��ȯ (�Ǵ� �ƿ� �������� ����)
                mainCameraController.ToggleAimMode(false);
            }
        }
    }

    // �� ������������ �����̽��ٸ� �����ų� �ð��� �� �Ǿ��� �� ���� ���������� �Ѿ�� ����
    void TransitionToNextStage(bool isTimedOut)
    {
        switch (currentState)
        {
            case PlayerState.Moving:
                SetPlayerState(PlayerState.AimingVertical);
                break;
            case PlayerState.AimingVertical:
                SetPlayerState(PlayerState.AimingHorizontal);
                break;
            case PlayerState.AimingHorizontal:
                SetPlayerState(PlayerState.SettingPower);
                playerShooting.ResetPowerGauge(); // �Ŀ� ������ �ʱ�ȭ
                break;
            case PlayerState.SettingPower:
                // �Ŀ� ������ �������Ƿ� �߻�
                playerShooting.Fire(); // Fire �Լ��� ���� PlayerShooting�� �ֽ��ϴ�.
                SetPlayerState(PlayerState.Firing); // <-- �߿�! �߻� �� ��� Firing ���·� ����
                break;
        }
    }

    // UI ��ҵ��� Ȱ��ȭ/��Ȱ��ȭ�� �����ϴ� �Լ� (�ӽ�, PlayerUIUpdater�� �и� ����)
    void UpdateUIForState(PlayerState state)
    {
        // ��� UI ��Ҹ� �⺻������ ����
        if (staminaImage != null) staminaImage.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (timerImage != null) timerImage.gameObject.SetActive(false);
        if (powerImage != null) powerImage.gameObject.SetActive(false);
        if (powerText != null) powerText.gameObject.SetActive(false);

        // �� ���¿� ���� �ʿ��� UI�� Ȱ��ȭ
        switch (state)
        {
            case PlayerState.Moving:
                if (staminaImage != null)
                {
                    staminaImage.gameObject.SetActive(true);
                    playerMovement.UpdateStaminaUI(); // PlayerMovement�� UI ������Ʈ �Լ� ȣ��
                }
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "move"; }
                break;
            case PlayerState.AimingVertical:
                if (timerImage != null) timerImage.gameObject.SetActive(true);
                if (timerText != null) timerText.gameObject.SetActive(true);
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "vertical"; }
                break;
            case PlayerState.AimingHorizontal:
                if (timerImage != null) timerImage.gameObject.SetActive(true);
                if (timerText != null) timerText.gameObject.SetActive(true);
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "horizon"; }
                break;
            case PlayerState.SettingPower:
                if (timerImage != null) timerImage.gameObject.SetActive(true);
                if (timerText != null) timerText.gameObject.SetActive(true);
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "power"; }
                if (powerImage != null) powerImage.gameObject.SetActive(true);
                if (powerText != null) powerText.gameObject.SetActive(true);
                playerShooting.UpdatePowerUI(); // PlayerShooting�� UI ������Ʈ �Լ� ȣ��
                break;
            case PlayerState.Waiting:
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "waiting."; }
                break;
            case PlayerState.Firing:
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "shoot!"; }
                break;
        }
    }

    // Trajectory ��ũ��Ʈ���� ȣ���� �� �ֵ��� ���� ���� �������� Ȯ���ϴ� �Լ�
    public bool IsAimingOrSettingPower()
    {
        return currentState == PlayerState.AimingVertical ||
               currentState == PlayerState.AimingHorizontal ||
               currentState == PlayerState.SettingPower;
    }

    // Trajectory ��ũ��Ʈ���� ���� �߻� �Ŀ��� ������ �� �ֵ��� �ϴ� �Լ�
    public float GetCurrentLaunchPower()
    {
        // ���� �Ŀ� ���� �����ʹ� PlayerShooting ��ũ��Ʈ�� �ֽ��ϴ�.
        return playerShooting.GetCurrentLaunchPower();
    }
}