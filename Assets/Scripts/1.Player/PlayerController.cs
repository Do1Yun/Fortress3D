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
    public List<Image> player1ItemSlotImages;
    public List<Image> player2ItemSlotImages;
    private GameManager gameManager;
    public Sprite healthIcon, rangeIcon, turnoffIcon;

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
        // ★★★ 핵심 수정 ★★★
        // 발사 중('Firing')이거나 대기 중('Waiting')일 때는 물리 효과(중력)만 계속 적용하여
        // 탱크가 땅에 붙어 있도록 합니다. 이렇게 하면 발사 후 꺼지는 현상이 사라집니다.
        if (currentState == PlayerState.Firing || currentState == PlayerState.Waiting)
        {
            playerMovement.UpdatePhysics();
            return; // 키 입력 등 다른 로직은 실행하지 않습니다.
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
                if (trajectory != null) trajectory.ShowTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.AimingHorizontal:
                playerMovement.UpdatePhysics();
                playerAiming.HandleHorizontalAim();
                if (trajectory != null) trajectory.ShowTrajectory();
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
        SetPlayerState(PlayerState.Moving);
        Debug.Log($"Player {playerID}의 턴 시작! [이동 모드]");
    }

    public void EndTurn()
    {
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
        ApplyEffect_GameObject(ItemList[index]); // 효과 적용
        ItemList.RemoveAt(index);                      // 사용 후 제거
        UpdateItemSelectionUI();                       // UI 갱신
    }

    public void ApplyEffect_GameObject(ItemType item)
    {
        PlayerMovement playerMovement = gameManager.players_movement[gameManager.currentPlayerIndex];
        PlayerController Player = gameManager.players[gameManager.currentPlayerIndex];
        PlayerController nextPlayer = gameManager.players[(gameManager.currentPlayerIndex + 1) % 2];

        switch (item)
        {
            case ItemType.Health:
                playerMovement.maxStamina *= 2;
                // playerMovement.staminaDrainRate /= 2; 로 할까 고민중
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
        var slotImages = GetCurrentItemSlotImages();

        for (int i = 0; i < slotImages.Count; i++)
        {
            if (i < ItemList.Count)
            {
                slotImages[i].sprite = GetItemIcon(ItemList[i]); // 아이템 아이콘 설정
                slotImages[i].color = Color.white;
                slotImages[i].gameObject.SetActive(true);
            }
            else
            {
                slotImages[i].sprite = null;                       // 아이콘 비우기
                slotImages[i].color = new Color(1, 1, 1, 0);   // 투명 처리
                slotImages[i].gameObject.SetActive(false);
            }
        }
    }

    private List<Image> GetCurrentItemSlotImages()
    {
        if (playerID == 0)
            return player1ItemSlotImages;
        else
            return player2ItemSlotImages;
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

