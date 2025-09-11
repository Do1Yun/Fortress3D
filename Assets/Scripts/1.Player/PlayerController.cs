// Scripts.zip/1.Player/PlayerController.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState { SelectingProjectile, Moving, AimingVertical, AimingHorizontal, SettingPower, Waiting, Firing }
    private PlayerState currentState;

    [Header("플레이어 기본 설정")]
    public int playerID;
    public int rerollChances = 3;

    [Header("UI 연결 (이 플레이어 전용)")]
    public Image staminaImage;
    public Image powerImage;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Image timerImage;
    public List<Image> projectileSlotImages;
    public TextMeshProUGUI rerollCountText;
    public List<Image> itemSlotImages;

    [Header("상태별 시간 제한")]
    public float stageTimeLimit = 5.0f;

    [Header("포탄 데이터")]
    public List<ProjectileData> projectileDatabase;
    public float BasicExplosionRange = 5.0f;
    public float ExplosionRange = 5.0f;
    private List<ProjectileData> currentSelection = new List<ProjectileData>();
    private ProjectileData selectedProjectile;

    private PlayerMovement playerMovement;
    private PlayerAiming playerAiming;
    private PlayerShooting playerShooting;
    public Trajectory trajectory;

    [Header("아이템 데이터")]
    public List<ItemType> ItemList = new List<ItemType>();
    public int maxItemCount = 5;
    private GameManager gameManager;
    public Sprite healthIcon, rangeIcon, turnoffIcon;

    [Header("점령 데이터")]
    public bool isInCaptureZone = false;

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
        playerMovement.SetUIReferences(staminaImage);
        playerShooting.SetUIReferences(powerImage, powerText);

        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");
        UpdateItemSelectionUI();
    }

    void Update()
    {
        if (currentState == PlayerState.Firing || currentState == PlayerState.Waiting)
        {
            playerMovement.UpdatePhysics();
            return;
        }

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
                HandleItemHotkeys();
                if (trajectory != null) trajectory.HideTrajectory();
                if (Input.GetKeyDown(KeyCode.Space) || playerMovement.currentStamina <= 0)
                {
                    TransitionToNextStage(false);
                }
                break;
            case PlayerState.SelectingProjectile:
                playerMovement.UpdatePhysics();
                HandleProjectileSelection();
                break;
            case PlayerState.AimingVertical:
                playerMovement.UpdatePhysics();
                playerAiming.HandleVerticalAim();
                if (trajectory != null) trajectory.ShowFixedTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.AimingHorizontal:
                playerMovement.UpdatePhysics();
                playerAiming.HandleHorizontalAim();
                if (trajectory != null) trajectory.ShowFixedTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.SettingPower:
                playerMovement.UpdatePhysics();
                playerShooting.HandlePowerSetting();
                if (trajectory != null) trajectory.ShowTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(true);
                break;
        }
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
    }

    public void StartTurn()
    {
        playerMovement.ResetStamina();
        selectedProjectile = null;

        if (playerShooting != null)
        {
            playerShooting.SetProjectile(null);
        }

        SetPlayerState(PlayerState.Moving);
        Debug.Log($"======== PLAYER {playerID} TURN START ========");
    }

    public void EndTurn()
    {
        if (projectileSlotImages != null)
        {
            foreach (var img in projectileSlotImages)
            {
                if (img != null) img.gameObject.SetActive(false);
            }
        }
        if (rerollCountText != null)
        {
            rerollCountText.gameObject.SetActive(false);
        }

        if (trajectory != null) trajectory.HideTrajectory();
        SetPlayerState(PlayerState.Waiting);
        Debug.Log($"Player {playerID}의 턴 종료!");
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
                if (selectedProjectile == null)
                {
                    Debug.Log("포탄을 선택하지 않아 첫 번째 포탄으로 자동 선택됩니다.");
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
            selectedProjectile = null;
            playerShooting.SetProjectile(null);
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

        // ★★★ [수정] 겹치지 않게 섞는 방식 대신, 3번의 완전한 무작위 선택으로 변경합니다. (중복 허용) ★★★
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, projectileDatabase.Count);
            currentSelection.Add(projectileDatabase[randomIndex]);
        }

        if (currentSelection.Count >= 3)
        {
            Debug.Log($"Player {playerID} Projectile Selection Generated: [1] {currentSelection[0].type}, [2] {currentSelection[1].type}, [3] {currentSelection[2].type}");
        }

        UpdateProjectileSelectionUI();
    }

    void SelectProjectile(int index, bool transition = true)
    {
        if (index < 0 || index >= currentSelection.Count) return;

        selectedProjectile = currentSelection[index];

        string prefabName = (selectedProjectile.prefab != null) ? selectedProjectile.prefab.name : "NULL";
        Debug.Log($"[ACTION] Player {playerID} selected index {index} -> Projectile: {selectedProjectile.type}, Setting Prefab: {prefabName}");

        playerShooting.SetProjectile(selectedProjectile.prefab);

        if (transition)
        {
            TransitionToNextStage(false);
        }
    }

    void UpdateProjectileSelectionUI()
    {
        if (projectileSlotImages == null) return;

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

        if (projectileSlotImages != null)
        {
            foreach (var img in projectileSlotImages)
            {
                if (img != null) img.gameObject.SetActive(isSelecting);
            }
        }
        if (rerollCountText != null)
        {
            rerollCountText.gameObject.SetActive(isSelecting);
        }

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

    // 아이템 함수

    private void HandleItemHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) UseItem(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) UseItem(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) UseItem(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) UseItem(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) UseItem(4);
    }

    public void UseItem(int index)
    {
        if (index < 0 || index >= ItemList.Count)
        {
            Debug.Log("해당 슬롯에 아이템이 없습니다.");
            return;
        }

        Debug.Log($"아이템 사용: {ItemList[index]}");
        ApplyEffect_GameObject(ItemList[index]);
        ItemList.RemoveAt(index);
        UpdateItemSelectionUI();
    }

    public void ApplyEffect_GameObject(ItemType item)
    {
        PlayerMovement playerMovement = gameManager.players_movement[playerID];
        PlayerController Player = gameManager.players[playerID];
        PlayerController nextPlayer = gameManager.players[(playerID + 1) % 2];

        switch (item)
        {
            case ItemType.Health:
                playerMovement.maxStamina *= 2;
                break;

            case ItemType.Range:
                Player.ExplosionRange *= 2;
                break;

            case ItemType.TurnOff:
                nextPlayer.trajectory.isPainted = false;
                break;
        }
    }

    public void UpdateItemSelectionUI()
    {
        if (itemSlotImages == null) return;

        for (int i = 0; i < itemSlotImages.Count; i++)
        {
            if (i < ItemList.Count)
            {
                itemSlotImages[i].sprite = GetItemIcon(ItemList[i]);
                itemSlotImages[i].color = Color.white;
                itemSlotImages[i].gameObject.SetActive(true);
            }
            else
            {
                itemSlotImages[i].sprite = null;
                itemSlotImages[i].color = new Color(1, 1, 1, 0);
                itemSlotImages[i].gameObject.SetActive(false);
            }
        }
    }

    private Sprite GetItemIcon(ItemType type)
    {
        switch (type)
        {
            case ItemType.Health: return healthIcon;
            case ItemType.Range: return rangeIcon;
            case ItemType.TurnOff: return turnoffIcon;
            default: return null;
        }
    }
}

[System.Serializable]
public class ProjectileData
{
    public ProjectileType type;
    public GameObject prefab;
    public Sprite icon;
}

