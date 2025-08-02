using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    // �÷��̾��� ���¸� ��Ÿ���� enum ����
    public enum PlayerState { SelectingProjectile, Moving, AimingVertical, AimingHorizontal, SettingPower, Waiting, Firing }
    private PlayerState currentState;

    [Header("�÷��̾� �⺻ ����")]
    public int playerID;
    public int rerollChances = 3;

    [Header("UI ����")]
    public Image staminaImage;
    public Image powerImage;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Image timerImage;

    [Header("���º� �ð� ����")]
    public float stageTimeLimit = 5.0f;

    [Header("��ź ���� UI")]
    public List<Image> projectileSlotImages;
    public TextMeshProUGUI rerollCountText;

    [Header("��ź ������")]
    public List<ProjectileData> projectileDatabase;
    private List<ProjectileData> currentSelection = new List<ProjectileData>();
    private ProjectileData selectedProjectile;

    // ���ҵ� ��� ��ũ��Ʈ�鿡 ���� ����
    private PlayerMovement playerMovement;
    private PlayerAiming playerAiming;
    private PlayerShooting playerShooting;
    public Trajectory trajectory;
    private CameraController mainCameraController;

    private float currentStageTimer;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAiming = GetComponent<PlayerAiming>();
        playerShooting = GetComponent<PlayerShooting>();
        trajectory = GetComponent<Trajectory>();

        if (playerMovement == null || playerAiming == null || playerShooting == null)
        {
            Debug.LogError("PlayerController�� �ʿ��� ��� ��ũ��Ʈ�� ��� �Ҵ���� �ʾҽ��ϴ�.", this);
            enabled = false;
        }
        if (trajectory == null)
        {
            Debug.LogWarning("Trajectory ������Ʈ�� ã�� �� �����ϴ�.", this);
        }
    }

    void Start()
    {
        mainCameraController = Camera.main.GetComponent<CameraController>();
        if (mainCameraController == null)
        {
            Debug.LogError("���� CameraController�� �ִ� ���� ī�޶� ã�� �� �����ϴ�.");
        }

        playerMovement.SetUIReferences(staminaImage);
        playerShooting.SetUIReferences(powerImage, powerText);
    }

    void Update()
    {
        // Waiting, Firing �����̰ų� �� ��ũ��Ʈ�� ��Ȱ��ȭ ������ ���� �ƹ��͵� ���� ����
        if (currentState == PlayerState.Waiting || currentState == PlayerState.Firing) return;

        // �̵� ���°� �ƴ� �� Ÿ�̸� �۵�
        if (currentState != PlayerState.Moving)
        {
            currentStageTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = $"{currentStageTimer:F1}";
            if (timerImage != null) timerImage.fillAmount = currentStageTimer / stageTimeLimit;

            if (currentStageTimer <= 0)
            {
                TransitionToNextStage(true);
                return;
            }
        }

        // ���º� ���� ó��
        switch (currentState)
        {
            case PlayerState.Moving:
                playerMovement.HandleMovement();
                if (trajectory != null) trajectory.HideTrajectory();
                if (Input.GetKeyDown(KeyCode.Space) || playerMovement.currentStamina <= 0)
                {
                    TransitionToNextStage(false);
                }
                break;
            case PlayerState.SelectingProjectile:
                HandleProjectileSelection();
                break;
            case PlayerState.AimingVertical:
                playerAiming.HandleVerticalAim();
                if (trajectory != null) trajectory.ShowTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.AimingHorizontal:
                playerAiming.HandleHorizontalAim();
                if (trajectory != null) trajectory.ShowTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.SettingPower:
                playerShooting.HandlePowerSetting();
                if (trajectory != null) trajectory.ShowTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(true);
                break;
        }
    }

    public void StartTurn()
    {
        playerMovement.ResetStamina();
        SetPlayerState(PlayerState.Moving);
        Debug.Log($"Player {playerID}�� �� ����! [�̵� ���]");
    }

    public void EndTurn()
    {
        SetPlayerState(PlayerState.Waiting);
        Debug.Log($"Player {playerID}�� �� ����!");
    }

    void SetPlayerState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"Player {playerID} ���� ����: {newState}");

        UpdateUIForState(currentState);

        if (newState != PlayerState.Moving)
        {
            currentStageTimer = stageTimeLimit;
        }

        if (mainCameraController != null)
        {
            mainCameraController.SetTarget(this.transform);
            switch (newState)
            {
                case PlayerState.Moving:
                case PlayerState.SelectingProjectile:
                    mainCameraController.SwitchMode(CameraController.CameraMode.Default);
                    break;
                case PlayerState.AimingVertical:
                    mainCameraController.SwitchMode(CameraController.CameraMode.SideView);
                    break;
                case PlayerState.AimingHorizontal:
                case PlayerState.SettingPower:
                    mainCameraController.SwitchMode(CameraController.CameraMode.TopDownView);
                    break;
                case PlayerState.Firing:
                case PlayerState.Waiting:
                    mainCameraController.SwitchMode(CameraController.CameraMode.Default);
                    break;
            }
        }
    }

    void TransitionToNextStage(bool isTimedOut)
    {
        switch (currentState)
        {
            case PlayerState.Moving:
                GenerateProjectileSelection();
                SetPlayerState(PlayerState.SelectingProjectile);
                break;
            case PlayerState.SelectingProjectile:
                if (isTimedOut)
                {
                    SelectProjectile(0, false);
                }
                SetPlayerState(PlayerState.AimingVertical);
                break;
            case PlayerState.AimingVertical:
                SetPlayerState(PlayerState.AimingHorizontal);
                break;
            case PlayerState.AimingHorizontal:
                playerShooting.ResetPowerGauge();
                SetPlayerState(PlayerState.SettingPower);
                break;
            case PlayerState.SettingPower:
                playerShooting.Fire();
                SetPlayerState(PlayerState.Firing);
                break;
        }
    }

    void HandleProjectileSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectProjectile(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectProjectile(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectProjectile(2);

        if (Input.GetKeyDown(KeyCode.R) && rerollChances > 0)
        {
            rerollChances--;
            GenerateProjectileSelection();
        }
    }

    void GenerateProjectileSelection()
    {
        currentSelection.Clear();
        if (projectileDatabase.Count == 0)
        {
            Debug.LogError("Projectile Database�� ����ֽ��ϴ�!");
            return;
        }
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, projectileDatabase.Count);
            currentSelection.Add(projectileDatabase[randomIndex]);
        }
        UpdateProjectileSelectionUI();
    }

    void SelectProjectile(int index, bool transition = true)
    {
        if (index < 0 || index >= currentSelection.Count) return;

        selectedProjectile = currentSelection[index];
        Debug.Log($"Player {playerID}�� '{selectedProjectile.type}' ��ź�� �����߽��ϴ�.");

        playerShooting.SetProjectile(selectedProjectile.prefab);

        if (transition)
        {
            TransitionToNextStage(false);
        }
    }

    void UpdateProjectileSelectionUI()
    {
        for (int i = 0; i < projectileSlotImages.Count; i++)
        {
            if (i < currentSelection.Count)
            {
                projectileSlotImages[i].gameObject.SetActive(true);
                projectileSlotImages[i].sprite = currentSelection[i].icon;
            }
            else
            {
                projectileSlotImages[i].gameObject.SetActive(false);
            }
        }
        if (rerollCountText != null) rerollCountText.text = $"Reroll: {rerollChances}";
    }

    void UpdateUIForState(PlayerState state)
    {
        bool isMoving = state == PlayerState.Moving;
        bool isSelecting = state == PlayerState.SelectingProjectile;
        bool isAimingOrPower = state == PlayerState.AimingVertical || state == PlayerState.AimingHorizontal || state == PlayerState.SettingPower;

        if (staminaImage != null && staminaImage.transform.parent != null) staminaImage.transform.parent.gameObject.SetActive(isMoving);
        if (timerImage != null && timerImage.transform.parent != null) timerImage.transform.parent.gameObject.SetActive(isAimingOrPower || isSelecting);
        if (powerImage != null && powerImage.transform.parent != null) powerImage.transform.parent.gameObject.SetActive(state == PlayerState.SettingPower);

        foreach (var img in projectileSlotImages)
        {
            if (img != null) img.gameObject.SetActive(isSelecting);
        }
        if (rerollCountText != null) rerollCountText.gameObject.SetActive(isSelecting);

        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = state.ToString();
        }
    }

    public bool IsAimingOrSettingPower()
    {
        return currentState == PlayerState.AimingVertical ||
               currentState == PlayerState.AimingHorizontal ||
               currentState == PlayerState.SettingPower;
    }

    public float GetCurrentLaunchPower()
    {
        return playerShooting.GetCurrentLaunchPower();
    }
}

[System.Serializable]
public class ProjectileData
{
    public ProjectileType type;
    public GameObject prefab;
    public Sprite icon;
}