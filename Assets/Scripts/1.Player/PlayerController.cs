using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트 참조를 위해 추가
using TMPro;
public class PlayerController : MonoBehaviour

{
    // 플레이어의 상태를 나타내는 enum 정의
    public enum PlayerState { Moving, AimingVertical, AimingHorizontal, SettingPower, Waiting, Firing }
    private PlayerState currentState;

    [Header("플레이어 기본 설정")]
    public int playerID;

    [Header("UI 연결 (선택 사항: UI Manager로 분리 가능)")]
    // 모든 UI 참조를 이 스크립트에서 관리하거나 PlayerUIUpdater로 옮길 수 있습니다.
    public Image staminaImage;
    public Image powerImage;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Image timerImage;

    [Header("상태별 시간 제한")]
    public float stageTimeLimit = 5.0f; // 각 조준/파워 설정 단계의 시간 제한

    // 분할된 기능 스크립트들에 대한 참조
    private PlayerMovement playerMovement;
    private PlayerAiming playerAiming;
    private PlayerShooting playerShooting;
    // private PlayerUIUpdater playerUIUpdater; // UI 업데이트를 별도 스크립트로 분리할 경우

    private float currentStageTimer; // 현재 단계의 남은 시간

    // 카메라 컨트롤러 참조
    private CameraController mainCameraController;

    void Awake()
    {
        // 필요한 컴포넌트 참조 가져오기
        playerMovement = GetComponent<PlayerMovement>();
        playerAiming = GetComponent<PlayerAiming>();
        playerShooting = GetComponent<PlayerShooting>();
        // playerUIUpdater = GetComponent<PlayerUIUpdater>(); // UI Updater를 분리했다면

        // 각 기능 스크립트가 올바르게 할당되었는지 확인
        if (playerMovement == null || playerAiming == null || playerShooting == null)
        {
            Debug.LogError("PlayerController에 필요한 기능 스크립트(Movement, Aiming, Shooting)가 모두 할당되지 않았습니다.", this);
            enabled = false; // 스크립트 비활성화
        }
    }

    void Start()
    {
        // 카메라 컨트롤러는 씬에 하나만 존재한다고 가정
        mainCameraController = Camera.main.GetComponent<CameraController>();
        if (mainCameraController == null)
        {
            Debug.LogError("씬에 CameraController가 있는 메인 카메라를 찾을 수 없습니다. 카메라 기능이 제한될 수 있습니다.");
        }

        // 초기 UI 연결 (각 기능 스크립트로 UI 참조를 전달)
        // 이 부분은 PlayerUIUpdater를 만들고 GameManager 이벤트를 구독하여 처리하는 것이 더 좋습니다.
        // 현재는 편의상 여기서 전달합니다.
        playerMovement.SetUIReferences(staminaImage);
        playerShooting.SetUIReferences(powerImage, powerText);

        // GameManager의 턴 시작/종료 이벤트에 구독 (PlayerController가 직접 받아 처리)
        // GameManager는 이제 모든 플레이어에게 StartTurn/EndTurn을 직접 호출합니다.
    }

    void Update()
    {
        // 대기 중이거나 발사 중일 때는 입력 및 상태 전환을 처리하지 않습니다.
        if (currentState == PlayerState.Waiting || currentState == PlayerState.Firing) return;

        // UI 타이머 업데이트 (이동 모드 제외)
        if (currentState != PlayerState.Moving)
        {
            currentStageTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = $"{currentStageTimer:F1}";
            if (timerImage != null) timerImage.fillAmount = currentStageTimer / stageTimeLimit;

            // 타이머가 0이 되면 다음 단계로 자동 전환 (발사 단계는 자동 발사)
            if (currentStageTimer <= 0)
            {
                TransitionToNextStage(true); // 시간이 다 된 경우
                return; // 이미 전환했으므로 현재 프레임의 나머지 Update 로직은 스킵
            }
        }

        // 현재 상태에 따른 기능 스크립트의 메서드 호출
        switch (currentState)
        {
            case PlayerState.Moving:
                playerMovement.HandleMovement();
                if (Input.GetKeyDown(KeyCode.Space) || playerMovement.currentStamina <= 0) // 스태미나 고갈 시 자동 전환
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

    // 턴이 시작될 때 GameManager로부터 호출됩니다.
    public void StartTurn()
    {
        playerMovement.ResetStamina(); // 스태미나 초기화
        SetPlayerState(PlayerState.Moving); // 턴 시작 시 이동 모드로 설정
        Debug.Log($"Player {playerID}의 턴 시작! (현재 상태: {currentState})");
    }

    // 턴이 종료될 때 GameManager로부터 호출됩니다.
    public void EndTurn()
    {
        SetPlayerState(PlayerState.Waiting); // 턴 종료 시 대기 모드로 설정
        Debug.Log($"Player {playerID}의 턴 종료! (현재 상태: {currentState})");
    }

    // 플레이어의 현재 상태를 설정하고, 그에 따른 UI 및 카메라를 업데이트합니다.
    void SetPlayerState(PlayerState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"Player {playerID} 상태 변경: {newState}");

        UpdateUIForState(currentState); // UI 업데이트 (PlayerUIUpdater로 분리할 것)

        // 상태 전환 시 초기화 작업
        currentStageTimer = stageTimeLimit; // 타이머 초기화

        // 카메라 모드 전환 (CameraController가 PlayerController와 분리되어 있으므로)
        if (mainCameraController != null)
        {
            if (newState == PlayerState.Moving)
            {
                mainCameraController.SetTarget(this.transform); // 플레이어 본체를 따라다니도록 설정
                mainCameraController.ToggleAimMode(false); // 기본 카메라 모드
            }
            else if (newState == PlayerState.AimingVertical ||
                     newState == PlayerState.AimingHorizontal ||
                     newState == PlayerState.SettingPower)
            {
                mainCameraController.SetTarget(this.transform); // 여전히 플레이어를 따라가되
                mainCameraController.ToggleAimMode(true, playerAiming.turretPivot); // 조준 모드 시 포탑을 바라보도록 설정
            }
            else if (newState == PlayerState.Firing)
            {
                // 포탄 발사 시 카메라는 GameManager에서 Projectile을 따라가도록 설정될 예정
                // 여기서는 잠시 기본 모드로 전환 (또는 아예 제어하지 않음)
                mainCameraController.ToggleAimMode(false);
            }
        }
    }

    // 각 스테이지에서 스페이스바를 누르거나 시간이 다 되었을 때 다음 스테이지로 넘어가는 로직
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
                playerShooting.ResetPowerGauge(); // 파워 게이지 초기화
                break;
            case PlayerState.SettingPower:
                // 파워 설정이 끝났으므로 발사
                playerShooting.Fire(); // Fire 함수가 이제 PlayerShooting에 있습니다.
                SetPlayerState(PlayerState.Firing); // <-- 중요! 발사 후 즉시 Firing 상태로 변경
                break;
        }
    }

    // UI 요소들의 활성화/비활성화를 관리하는 함수 (임시, PlayerUIUpdater로 분리 권장)
    void UpdateUIForState(PlayerState state)
    {
        // 모든 UI 요소를 기본적으로 숨김
        if (staminaImage != null) staminaImage.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (timerImage != null) timerImage.gameObject.SetActive(false);
        if (powerImage != null) powerImage.gameObject.SetActive(false);
        if (powerText != null) powerText.gameObject.SetActive(false);

        // 각 상태에 따라 필요한 UI만 활성화
        switch (state)
        {
            case PlayerState.Moving:
                if (staminaImage != null)
                {
                    staminaImage.gameObject.SetActive(true);
                    playerMovement.UpdateStaminaUI(); // PlayerMovement의 UI 업데이트 함수 호출
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
                playerShooting.UpdatePowerUI(); // PlayerShooting의 UI 업데이트 함수 호출
                break;
            case PlayerState.Waiting:
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "waiting."; }
                break;
            case PlayerState.Firing:
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "shoot!"; }
                break;
        }
    }

    // Trajectory 스크립트에서 호출할 수 있도록 현재 조준 상태인지 확인하는 함수
    public bool IsAimingOrSettingPower()
    {
        return currentState == PlayerState.AimingVertical ||
               currentState == PlayerState.AimingHorizontal ||
               currentState == PlayerState.SettingPower;
    }

    // Trajectory 스크립트에서 현재 발사 파워를 가져갈 수 있도록 하는 함수
    public float GetCurrentLaunchPower()
    {
        // 이제 파워 관련 데이터는 PlayerShooting 스크립트에 있습니다.
        return playerShooting.GetCurrentLaunchPower();
    }
}