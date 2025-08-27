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

    // Rigidbody 참조를 위한 변수
    private Rigidbody rb;

    private float currentStageTimer;

   void Awake()
{
    // ================================================================== //
    // ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼ 기존 Awake() 함수를 이걸로 교체해주세요 ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼ //
    // ================================================================== //

    Debug.Log("--- PlayerController Awake() 진단 시작 ---");

    playerMovement = GetComponent<PlayerMovement>();
    if (playerMovement == null)
    {
        Debug.LogError("진단 결과: PlayerMovement 스크립트를 찾지 못했습니다!");
    }
    else
    {
        Debug.Log("진단 결과: PlayerMovement 스크립트 찾기 성공.");
    }

    playerAiming = GetComponent<PlayerAiming>();
    if (playerAiming == null)
    {
        Debug.LogError("진단 결과: PlayerAiming 스크립트를 찾지 못했습니다!");
    }
    else
    {
        Debug.Log("진단 결과: PlayerAiming 스크립트 찾기 성공.");
    }

    playerShooting = GetComponent<PlayerShooting>();
    if (playerShooting == null)
    {
        Debug.LogError("진단 결과: PlayerShooting 스크립트를 찾지 못했습니다!");
    }
    else
    {
        Debug.Log("진단 결과: PlayerShooting 스크립트 찾기 성공.");
    }

    trajectory = GetComponent<Trajectory>();
    if (trajectory == null)
    {
        Debug.LogWarning("진단 결과: Trajectory 컴포넌트를 찾을 수 없습니다.");
    }
    else
    {
        Debug.Log("진단 결과: Trajectory 컴포넌트 찾기 성공.");
    }
    
    Debug.Log("--- PlayerController Awake() 진단 종료 ---");

    // ================================================================== //
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲ //
    // ================================================================== //
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
        if (currentState == PlayerState.Waiting || currentState == PlayerState.Firing) return;

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
                playerMovement.StopMovement(); // 이동을 멈추고 물리 상태를 결정하도록 요청
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

    /// <summary>
    /// 외부(World.cs)에서 호출할 함수.
    /// 자신의 발밑 지형을 확인하고, 땅이 없으면 물리 상태를 갱신하여 떨어지도록 만듭니다.
    /// </summary>
    public void CheckForGround()
    {
        // 이미 물리 엔진의 제어를 받고 있다면(움직이는 중 등) 굳이 확인할 필요가 없습니다.
        if (rb != null && !rb.isKinematic)
        {
            return;
        }

        // 아주 짧은 레이캐스트를 아래로 쏴서 발밑에 땅이 있는지 확인합니다.
        // 이 때 PlayerMovement에 있는 groundLayer 정보를 가져와야 하므로, PlayerMovement에 IsGrounded 함수를 만들어 호출합니다.
        if (playerMovement != null && !playerMovement.IsGrounded())
        {
            Debug.Log($"Player {playerID}의 발밑에 땅이 없어 isKinematic을 해제합니다.");

            // 땅이 없다면, isKinematic을 false로 만들어 중력의 영향을 받게 합니다.
            rb.isKinematic = false;
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