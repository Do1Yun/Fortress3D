using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UIStateSettings
{
    public PlayerController.PlayerState state;
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

    [Header("상태별 UI 설정")]
    public List<UITweener> allManagedTweeners;
    public List<UIStateSettings> uiStateSettings;

    [Header("중계 멘트 설정")]
    public AudioClip moveStartCommentary1;
    public AudioClip moveStartCommentary2;
    public AudioClip staminaDepletedCommentary1;
    public AudioClip staminaDepletedCommentary2;
    public AudioClip choiceCommentary;
    public AudioClip NotchoiceCommentary;
    public AudioClip AimingVerticalCommentary;
    public AudioClip AimingHorizontalCommentary;

    [Header("아이템 사용 멘트 (4종류)")]
    public AudioClip commentItemstamina;
    public AudioClip commentItem2x;
    public AudioClip commentItemTurnOff;
    public AudioClip commentItemChasing;

    // ★ [추가] 효과음(SFX) 설정
    [Header("효과음 설정 (SFX)")]
    [Tooltip("아이템 사용 시 재생할 소리")]
    public AudioClip itemUseSFX;
    [Tooltip("포탄 선택(변경) 시 재생할 소리")]
    public AudioClip projectileSelectSFX;
    [Tooltip("포탄 발사 시 재생할 소리")]
    public AudioClip fireSFX;

    private bool hasPlayedStaminaCommentary = false;
    private Coroutine activeCommentaryCoroutine;

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
    public Sprite healthIcon, rangeIcon, turnoffIcon, chasingIcon;
    private bool using_chasingItem = false;
    public GameObject BuffEffectPrefab;
    public GameObject DebuffEffectPrefab;

    [Header("점령 데이터")]
    public bool isInCaptureZone = false;

    [Header("우당탕탕 데이터")]
    public float MakingGroundTime = 10.0f;
    public bool isMakingGround = false;
    public Camera mainCamera;

    [Header("특수탄 설정")]
    public KeyCode chaserModeKey = KeyCode.M;
    [HideInInspector] public bool isNextShotChaser = false;

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
        BuffEffectPrefab.SetActive(false);
        DebuffEffectPrefab.SetActive(false);
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

    void Update()
    {
        if (playerMovement.speedMultiplier > 1f)
        {
            BuffEffectPrefab.SetActive(true);
        }
        else
        {
            BuffEffectPrefab.SetActive(false);
        }

        if (trajectory.isPainted)
        {
            DebuffEffectPrefab.SetActive(false);
        }
        else
        {
            DebuffEffectPrefab.SetActive(true);
        }

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

                if (playerMovement.currentStamina <= 0)
                {
                    if (!hasPlayedStaminaCommentary)
                    {
                        hasPlayedStaminaCommentary = true;

                        Debug.Log($"[LOG] Player {playerID}: 기동력 소진 상황 발생!");

                        if (Random.value <= 1.0f)
                        {
                            TriggerCommentary(staminaDepletedCommentary1, staminaDepletedCommentary2);
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.Space) || playerMovement.currentStamina <= 0)
                {
                    TransitionToNextStage(false);
                }
                break;

            case PlayerState.SelectingProjectile:
            case PlayerState.AimingVertical:
            case PlayerState.AimingHorizontal:
            case PlayerState.SettingPower:
                currentStageTimer -= Time.deltaTime;
                if (timerText != null) timerText.text = $"{currentStageTimer:F1} / {stageTimeLimit:F1}";
                if (timerImage != null) timerImage.fillAmount = currentStageTimer / stageTimeLimit;

                if (currentStageTimer <= 0f)
                {
                    TransitionToNextStage(true);
                }
                else
                {
                    if (using_chasingItem)
                    {
                        isNextShotChaser = true;
                        ToggleChaserMode();
                    }
                    HandleActiveTurnInput();
                }
                break;
        }
    }

    private void HandleActiveTurnInput()
    {
        playerMovement.UpdatePhysics();

        switch (currentState)
        {
            case PlayerState.SelectingProjectile:
                HandleProjectileSelection();
                break;
            case PlayerState.AimingVertical:
                playerAiming.HandleVerticalAim();
                if (!trajectory.isPainted) trajectory.HideTrajectory();
                else if (trajectory != null) trajectory.ShowFixedTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.AimingHorizontal:
                playerAiming.HandleHorizontalAim();
                if (!trajectory.isPainted) trajectory.HideTrajectory();
                else if (trajectory != null) trajectory.ShowFixedTrajectory();
                if (Input.GetKeyDown(KeyCode.Space)) TransitionToNextStage(false);
                break;
            case PlayerState.SettingPower:
                playerShooting.HandlePowerSetting();
                if (!trajectory.isPainted) trajectory.HideTrajectory();
                else if (trajectory != null) trajectory.ShowTrajectory();
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
    }

    public void StartTurn()
    {
        playerMovement.ResetStamina();
        playerMovement.ResetSpeed();
        using_chasingItem = false;
        selectedProjectile = null;
        isNextShotChaser = false;

        hasPlayedStaminaCommentary = false;

        if (playerShooting != null)
        {
            playerShooting.SetProjectile(null);
        }

        currentStageTimer = stageTimeLimit;
        SetPlayerState(PlayerState.Moving);
        Debug.Log($"======== PLAYER {playerID} TURN START (타이머: {currentStageTimer}) ========");

        Debug.Log($"[LOG] Player {playerID}: 이동 시작 상황 발생!");

        if (Random.value <= 0.2f)
        {
            if (GameManager.instance.coment == false)
            {
                TriggerCommentary(moveStartCommentary1, moveStartCommentary2);
            }
            else
            {
                GameManager.instance.coment = false;
            }
        }
    }

    public void TriggerCommentary(AudioClip clip1, AudioClip clip2)
    {
        if (activeCommentaryCoroutine != null)
        {
            StopCoroutine(activeCommentaryCoroutine);
        }

        if (GameManager.instance != null && GameManager.instance.announcerAudioSource != null)
        {
            GameManager.instance.announcerAudioSource.Stop();
        }

        activeCommentaryCoroutine = StartCoroutine(PlayCommentarySequence(clip1, clip2));
    }

    IEnumerator PlayCommentarySequence(AudioClip clip1, AudioClip clip2)
    {
        AudioSource announcer = null;
        if (GameManager.instance != null)
        {
            announcer = GameManager.instance.announcerAudioSource;
        }

        if (announcer == null) yield break;

        if (clip1 != null)
        {
            announcer.PlayOneShot(clip1);
            yield return new WaitForSecondsRealtime(clip1.length);
        }

        if (clip2 != null)
        {
            announcer.PlayOneShot(clip2);
            yield return new WaitForSecondsRealtime(clip2.length);
        }

        activeCommentaryCoroutine = null;
    }

    public void EndTurn()
    {
        if (trajectory != null) trajectory.HideTrajectory();

        if (activeCommentaryCoroutine != null)
        {
            StopCoroutine(activeCommentaryCoroutine);
            activeCommentaryCoroutine = null;
        }

        SetPlayerState(PlayerState.Waiting);
        Debug.Log($"Player {playerID}의 턴 종료!");
    }

    void TransitionToNextStage(bool isTimedOut)
    {
        currentStageTimer = stageTimeLimit;

        switch (currentState)
        {
            case PlayerState.Moving:
                GenerateProjectileSelection();
                if (Random.value <= 0.2f)
                {
                    if (gameManager != null && gameManager.announcerAudioSource != null && choiceCommentary != null)
                    {
                        gameManager.announcerAudioSource.Stop();
                        gameManager.announcerAudioSource.PlayOneShot(choiceCommentary);
                    }
                }
                SetPlayerState(PlayerState.SelectingProjectile);
                break;
            case PlayerState.SelectingProjectile:
                if (selectedProjectile == null)
                {
                    Debug.Log("포탄을 선택하지 않아 첫 번째 포탄으로 자동 선택됩니다.");
                    if (Random.value <= 1.0f)
                    {
                        if (gameManager != null && gameManager.announcerAudioSource != null && NotchoiceCommentary != null)
                        {
                            gameManager.announcerAudioSource.Stop();
                            gameManager.announcerAudioSource.PlayOneShot(NotchoiceCommentary);
                        }
                    }
                    SelectProjectile(0, false);
                }
                SetPlayerState(PlayerState.AimingVertical);
                GameManager.instance.dangtang = false;
                if (Random.value <= 0.2f)
                {
                    if (gameManager != null && gameManager.announcerAudioSource != null && AimingVerticalCommentary != null)
                    {
                        gameManager.announcerAudioSource.Stop();
                        gameManager.announcerAudioSource.PlayOneShot(AimingVerticalCommentary);
                    }
                }
                break;
            case PlayerState.AimingVertical:
                SetPlayerState(PlayerState.AimingHorizontal);
                if (Random.value <= 0.2f)
                {
                    if (gameManager != null && gameManager.announcerAudioSource != null && AimingHorizontalCommentary != null)
                    {
                        gameManager.announcerAudioSource.Stop();
                        gameManager.announcerAudioSource.PlayOneShot(AimingHorizontalCommentary);
                    }
                }
                break;
            case PlayerState.AimingHorizontal:
                playerShooting.ResetPowerGauge();
                SetPlayerState(PlayerState.SettingPower);
                break;
            case PlayerState.SettingPower:
                // ★ [추가] 발사 효과음 재생
                if (gameManager != null && gameManager.sfxAudioSource != null && fireSFX != null)
                {
                    gameManager.sfxAudioSource.PlayOneShot(fireSFX);
                }

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

        // ★ [추가] 포탄 선택 효과음 재생
        if (gameManager != null && gameManager.sfxAudioSource != null && projectileSelectSFX != null)
        {
            gameManager.sfxAudioSource.PlayOneShot(projectileSelectSFX);
        }

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
        if (rerollCountText != null) rerollCountText.text = $"{rerollChances}";
    }

    void UpdateUIForState(PlayerState state)
    {
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

        UIStateSettings currentSettings = uiStateSettings.Find(s => s.state == state);
        List<UITweener> tweenersToShow = (currentSettings != null) ? currentSettings.activeTweeners : new List<UITweener>();

        if (allManagedTweeners != null)
        {
            foreach (var tweener in allManagedTweeners)
            {
                if (tweener == null) continue;
                if (tweenersToShow.Contains(tweener))
                {
                    tweener.Show();
                }
                else
                {
                    tweener.Hide();
                }
            }
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

        // ★ [추가] 아이템 사용 효과음 재생
        if (gameManager != null && gameManager.sfxAudioSource != null && itemUseSFX != null)
        {
            gameManager.sfxAudioSource.PlayOneShot(itemUseSFX);
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
                if (Random.value <= 0.2f)
                {
                    if (gameManager != null && gameManager.announcerAudioSource != null && commentItemstamina != null)
                    {
                        gameManager.announcerAudioSource.Stop();
                        gameManager.announcerAudioSource.PlayOneShot(commentItemstamina);
                        Debug.Log("아이템 사용 멘트 재생!");
                    }
                }

                playerMovement.speedMultiplier *= 1.5f;
                break;

            case ItemType.Range:
                if (Random.value <= 0.2f)
                {
                    if (gameManager != null && gameManager.announcerAudioSource != null && commentItem2x != null)
                    {
                        gameManager.announcerAudioSource.Stop();
                        gameManager.announcerAudioSource.PlayOneShot(commentItem2x);
                        Debug.Log("아이템 획득 멘트 재생!");
                    }
                }

                Player.ExplosionRange *= 1.5f;
                break;

            case ItemType.TurnOff:
                if (Random.value <= 0.2f)
                {
                    if (gameManager != null && gameManager.announcerAudioSource != null && commentItemTurnOff != null)
                    {
                        gameManager.announcerAudioSource.Stop();
                        gameManager.announcerAudioSource.PlayOneShot(commentItemTurnOff);
                        Debug.Log("아이템 획득 멘트 재생!");
                    }
                }
                nextPlayer.trajectory.isPainted = false;
                break;
            case ItemType.Chasing:
                if (Random.value <= 0.2f)
                {
                    if (gameManager != null && gameManager.announcerAudioSource != null && commentItemChasing != null)
                    {
                        gameManager.announcerAudioSource.Stop();
                        gameManager.announcerAudioSource.PlayOneShot(commentItemChasing);
                        Debug.Log("아이템 획득 멘트 재생!");
                    }
                }

                using_chasingItem = true;
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
            case ItemType.Chasing: return chasingIcon;
            default: return null;
        }
    }

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
                Instantiate(projectileDatabase[2].prefab, SpawnPosition, Quaternion.Euler(180f, 0f, 0f));
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
                Instantiate(projectileDatabase[1].prefab, SpawnPosition, Quaternion.Euler(180f, 0f, 0f));
                Debug.Log("지형 파괴 포탄 생성");
            }
        }
    }

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

    public void ToggleChaserMode()
    {
        if (statusText != null)
        {
            if (isNextShotChaser)
            {
                statusText.text = "특수탄: 추적자";
            }
            else
            {
                UpdateUIForState(currentState);
            }
        }
    }

    public void ResetChaserModeAfterFire()
    {
        if (isNextShotChaser)
        {
            isNextShotChaser = false;
            Debug.Log("추적자 모드 사용됨. 다음 턴을 위해 초기화.");
        }
    }

    public ProjectileType GetSelectedProjectileType()
    {
        if (selectedProjectile != null)
        {
            return selectedProjectile.type;
        }

        if (currentSelection.Count > 0)
        {
            return currentSelection[0].type;
        }

        Debug.LogError("선택된 포탄이 없으며, 현재 포탄 목록도 비어있습니다. NormalImpact로 대체합니다.");
        return ProjectileType.NormalImpact;
    }
}

[System.Serializable]
public class ProjectileData
{
    public ProjectileType type;
    public GameObject prefab;
    public Sprite icon;
}