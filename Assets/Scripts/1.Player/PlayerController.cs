using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    // 플레이어의 상태를 나타내는 enum 정의
    public enum PlayerState { SelectingProjectile, Moving, AimingVertical, AimingHorizontal, SettingPower, Waiting, Firing }
    private PlayerState currentState;

    [Header("플레이어 기본 설정")]
    public int playerID;
    public int rerollChances = 3;

    [Header("UI 연결")]
    public Image staminaImage;
    public Image powerImage;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Image timerImage;

    [Header("상태별 시간 제한")]
    public float stageTimeLimit = 5.0f;

    [Header("포탄 선택 UI")]
    public List<Image> projectileSlotImages;
    public TextMeshProUGUI rerollCountText;

    [Header("포탄 데이터")]
    public List<ProjectileData> projectileDatabase;
    private List<ProjectileData> currentSelection = new List<ProjectileData>();
    private ProjectileData selectedProjectile;

    // 분할된 기능 스크립트들에 대한 참조
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
            Debug.LogError("PlayerController에 필요한 기능 스크립트가 모두 할당되지 않았습니다.", this);
            enabled = false;
        }
        if (trajectory == null)
        {
            Debug.LogWarning("Trajectory 컴포넌트를 찾을 수 없습니다.", this);
        }
    }

    void Start()
    {
        mainCameraController = Camera.main.GetComponent<CameraController>();
        if (mainCameraController == null)
        {
            Debug.LogError("씬에 CameraController가 있는 메인 카메라를 찾을 수 없습니다.");
        }

        playerMovement.SetUIReferences(staminaImage);
        playerShooting.SetUIReferences(powerImage, powerText);
    }

    void Update()
    {
        // Waiting, Firing 상태이거나 이 스크립트가 비활성화 상태일 때는 아무것도 하지 않음
        if (currentState == PlayerState.Waiting || currentState == PlayerState.Firing) return;

        // 이동 상태가 아닐 때 타이머 작동
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

        // 상태별 로직 처리
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
        Debug.Log($"Player {playerID}의 턴 시작! [이동 모드]");
    }

    public void EndTurn()
    {
        SetPlayerState(PlayerState.Waiting);
        Debug.Log($"Player {playerID}의 턴 종료!");
    }

    void SetPlayerState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"Player {playerID} 상태 변경: {newState}");

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
            Debug.LogError("Projectile Database가 비어있습니다!");
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
        Debug.Log($"Player {playerID}가 '{selectedProjectile.type}' 포탄을 선택했습니다.");

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