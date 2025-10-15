using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UITweener와 함께 사용할 상태별 UI 설정 클래스입니다.
[System.Serializable]
public class UIStateSettings
{
    public PlayerController.PlayerState state;
    // 이 상태일 때 활성화할 UITweener 컴포넌트들의 리스트입니다.
    public List<UITweener> activeTweeners;
}

public class PlayerController : MonoBehaviour
{
    public enum PlayerState { SelectingProjectile, Moving, AimingVertical, AimingHorizontal, SettingPower, Waiting, Firing, MakingGround }
    public PlayerState currentState;

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

    // ▼▼▼ [UITweener 시스템 복원] ▼▼▼
    [Header("상태별 UI 설정")]
    [Tooltip("상태에 따라 제어할 모든 UI 트위너들을 여기에 등록합니다.")]
    public List<UITweener> allManagedTweeners;
    [Tooltip("각 PlayerState 별로 활성화할 UI 트위너들을 지정합니다.")]
    public List<UIStateSettings> uiStateSettings;

    [Header("UI 사운드 설정")]
    public AudioSource uiAudioSource;
    public AudioClip uiSlideInSound;
    public AudioClip uiSlideOutSound;
    // ▲▲▲ [여기까지 복원] ▲▲▲

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

    [Header("우당탕탕 데이터")]
    public float MakingGroundTime = 10.0f;
    public bool isMakingGround = false;
    public Camera mainCamera;

    private float currentStageTimer = 5.0f;

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

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        UpdateItemSelectionUI();
    }

    // ★★★ [수정됨] Update 함수 전체를 switch 문으로 재구성하여 안정성 확보 ★★★
    void Update()
    {
        switch (currentState)
        {
            case PlayerState.Waiting:
            case PlayerState.Firing:
                playerMovement.UpdatePhysics();
                break;

            case PlayerState.MakingGround:
                currentStageTimer -= Time.deltaTime;
                if (timerText != null) timerText.text = $"{currentStageTimer:F1}";
                if (timerImage != null) timerImage.fillAmount = currentStageTimer / MakingGroundTime;

                if (currentStageTimer <= 0f)
                {
                    Debug.Log($"Player {playerID} 우당탕탕 종료");
                    isMakingGround = false;
                    SetPlayerState(PlayerState.Waiting);
                }
                else
                {
                    HandleModifyKeys();
                }
                break;

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
            case PlayerState.AimingVertical:
            case PlayerState.AimingHorizontal:
            case PlayerState.SettingPower:
                // 타이머 처리
                currentStageTimer -= Time.deltaTime;
                if (timerText != null) timerText.text = $"{currentStageTimer:F1} / {stageTimeLimit:F1}";
                if (timerImage != null) timerImage.fillAmount = currentStageTimer / stageTimeLimit;

                // 타임아웃 또는 입력에 따른 로직 처리
                if (currentStageTimer <= 0f)
                {
                    TransitionToNextStage(true);
                }
                else
                {
                    HandleActiveTurnInput();
                }
                break;
        }
    }

    // 활성 턴 상태(탄 선택, 조준, 파워)의 입력을 처리하는 함수
    private void HandleActiveTurnInput()
    {
        playerMovement.UpdatePhysics(); // 이동은 하지 않지만 물리 효과(중력 등)는 계속 적용

        switch (currentState)
        {
            case PlayerState.SelectingProjectile:
                HandleProjectileSelection();
                break;
            case PlayerState.AimingVertical:
                playerAiming.HandleVerticalAim();
                if (trajectory != null) trajectory.ShowFixedTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.AimingHorizontal:
                playerAiming.HandleHorizontalAim();
                if (trajectory != null) trajectory.ShowFixedTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.SettingPower:
                playerShooting.HandlePowerSetting();
                if (trajectory != null) trajectory.ShowTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(true);
                break;
        }
    }

    // 플레이어의 상태를 변경하고, 그에 맞는 UI를 표시하는 함수
    void SetPlayerState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"Player {playerID} 상태 변경: {newState}");

        UpdateUIForState(currentState); // 상태가 변경될 때마다 UI 업데이트 호출
    }

    // 턴 시작 시 호출되는 함수
    public void StartTurn()
    {
        playerMovement.ResetStamina();
        selectedProjectile = null;

        if (playerShooting != null)
        {
            playerShooting.SetProjectile(null);
        }

        currentStageTimer = stageTimeLimit;
        SetPlayerState(PlayerState.Moving);
        Debug.Log($"======== PLAYER {playerID} TURN START (타이머: {currentStageTimer}) ========");
    }

    // 턴 종료 시 호출되는 함수
    public void EndTurn()
    {
        if (trajectory != null) trajectory.HideTrajectory();
        SetPlayerState(PlayerState.Waiting);
        Debug.Log($"Player {playerID}의 턴 종료!");
    }

    // 다음 게임 단계로 전환하는 함수
    void TransitionToNextStage(bool isTimedOut)
    {
        currentStageTimer = stageTimeLimit; // 다음 단계를 위해 타이머 초기화

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

    // 탄 선택 입력 처리
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

    // 3개의 무작위 탄 생성
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

        if (currentSelection.Count >= 3)
        {
            Debug.Log($"Player {playerID} Projectile Selection Generated: [1] {currentSelection[0].type}, [2] {currentSelection[1].type}, [3] {currentSelection[2].type}");
        }

        UpdateProjectileSelectionUI();
    }

    // 탄 선택 확정
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

    // 탄 선택 UI 아이콘 업데이트
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
        if (rerollCountText != null) rerollCountText.text = $"{rerollChances}";
    }

    // ▼▼▼ [UITweener 시스템 복원 및 수정] ▼▼▼
    void UpdateUIForState(PlayerState state)
    {
        // 상태 텍스트는 항상 업데이트
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            switch (state)
            {
                case PlayerState.Moving:
                    statusText.text = "이동 모드";
                    staminaImage.gameObject.SetActive(true);
                    powerText.gameObject.SetActive(false);
                    powerImage.gameObject.SetActive(false);
                    timerImage.gameObject.SetActive(false);
                    timerText.gameObject.SetActive(false);
                    break;
                case PlayerState.SelectingProjectile:
                    statusText.text = "탄 선택 모드";
                    staminaImage.gameObject.SetActive(false);
                    powerText.gameObject.SetActive(true);
                    powerImage.gameObject.SetActive(true);
                    timerImage.gameObject.SetActive(true);
                    timerText.gameObject.SetActive(true);
                    break;
                case PlayerState.AimingVertical: statusText.text = "수직 조준"; break;
                case PlayerState.AimingHorizontal: statusText.text = "수평 조준"; break;
                case PlayerState.SettingPower: statusText.text = "파워 조절"; break;
                case PlayerState.Waiting:
                case PlayerState.Firing: statusText.text = "대기중..."; break;
                case PlayerState.MakingGround: statusText.text = "우당탕탕!"; break;
                default: statusText.text = state.ToString(); break;
            }
        }

        // 현재 상태에 맞는 UI 설정을 인스펙터 리스트에서 찾음
        UIStateSettings currentSettings = uiStateSettings.Find(s => s.state == state);

        // 보여줘야 할 트위너 리스트를 결정 (설정이 없으면 빈 리스트)
        List<UITweener> tweenersToShow = (currentSettings != null) ? currentSettings.activeTweeners : new List<UITweener>();

        // 관리하는 모든 트위너를 순회
        if (allManagedTweeners != null)
        {
            foreach (var tweener in allManagedTweeners)
            {
                if (tweener == null) continue;

                // 보여줘야 할 리스트에 포함되어 있으면 Show, 아니면 Hide 호출
                if (tweenersToShow.Contains(tweener))
                {
                    tweener.Show(uiAudioSource, uiSlideInSound);
                }
                else
                {
                    tweener.Hide(uiAudioSource, uiSlideOutSound);
                }
            }
        }
    }
    // ▲▲▲ [여기까지 복원 및 수정] ▲▲▲

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

    // 아이템 단축키 처리
    private void HandleItemHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) UseItem(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) UseItem(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) UseItem(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) UseItem(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) UseItem(4);
    }

    // 아이템 사용
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

    // 아이템 효과 적용
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

    // 아이템 슬롯 UI 업데이트
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

    // 아이템 타입에 맞는 아이콘 반환
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

    // '우당탕탕' 중 마우스 입력 처리
    private void HandleModifyKeys()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 groundPoint = hit.point;
                Vector3 SpawnPosition = new Vector3(groundPoint.x, groundPoint.y + 11, groundPoint.z);
                Instantiate(projectileDatabase[2].prefab, SpawnPosition, Quaternion.identity);
                Debug.Log("지형 생성 포탄 생성");
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 groundPoint = hit.point;
                Vector3 SpawnPosition = new Vector3(groundPoint.x, groundPoint.y + 11, groundPoint.z);
                Instantiate(projectileDatabase[1].prefab, SpawnPosition, Quaternion.identity);
                Debug.Log("지형 파괴 포탄 생성");
            }
        }
    }

    // '우당탕탕' 시작
    public void MakeGround()
    {
        if (!isMakingGround)
        {
            isMakingGround = true;
            currentStageTimer = MakingGroundTime;
            Debug.Log($"Player {playerID} 우당탕탕 시작 (지속 시간: {currentStageTimer}초)");
            SetPlayerState(PlayerState.MakingGround);
        }
    }
}

// 포탄 정보를 담는 클래스
[System.Serializable]
public class ProjectileData
{
    public ProjectileType type;
    public GameObject prefab;
    public Sprite icon;
}

