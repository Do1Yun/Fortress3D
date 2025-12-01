using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ★ 버튼 제어를 위해 추가 필수

using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<PlayerController> players;
    public List<PlayerMovement> players_movement;
    public int currentPlayerIndex = 0;

    [Header("중계 오디오 설정")]
    public AudioSource announcerAudioSource;
    public AudioClip openingCommentary1;
    public AudioClip openingCommentary2;
    public AudioClip closingCommentary;
    public AudioClip turnCommentary;
    public AudioClip pointCommentary;
    public AudioClip p2Commentary;
    public AudioClip NpCommentary;

    [Header("룰렛 연출 오디오")]
    public AudioClip rouletteSpinSFX;
    public AudioClip rouletteDecideSFX;

    [Header("배경음악 오디오 설정")]
    public AudioSource BGMAudioSource;
    public AudioClip BGM1;
    public AudioClip BGM2;

    [Header("메인카메라 지정")]
    public CameraController mainCameraController;
    public GameObject MGCamera;
    public ProjectileFollowCamera projectileCam;

    [Header("UI 연결")]
    public TextMeshProUGUI turnDisplayText;
    public TextMeshProUGUI rouletteResultText;
    public GameObject pauseMenuUI;
    public GameObject darkBackground;

    [Header("점수 UI 연결")]
    public TextMeshProUGUI scoreTextP1;
    public TextMeshProUGUI scoreTextP2;

    [Header("진행 제어 UI")]
    public GameObject nextPhaseButton; // '다음' 또는 '게임 시작' 버튼
    public GameObject gameOverPanel;   // 게임 오버 패널
    public TextMeshProUGUI winnerText;

    [Header("플레이어 수,스코어")]
    public int minPlayersForGame = 2;
    public int score_player1 = 0, score_player2 = 0;
    public int WinningScore = 3;
    public int playerInCaptureZone = 0;

    private bool TurnFlag = false;
    public bool dangtang = false;
    public bool coment = false;

    private bool isPhaseReady = false;

    public bool isPaused = false;

    public enum GameState
    {
        PreGame,
        PlayerTurn,
        ProjectileFlying,
        TurnEnd,
        MakeGround,
        GameOver
    }
    public GameState currentState;

    public UnityEvent<int> OnTurnStart;
    public UnityEvent<int> OnTurnEnd;
    public UnityEvent<GameState> OnGameStateChanged;
    public UnityEvent OnGameOver;

    void Awake()
    {
        // 싱글톤 갱신 로직
        if (instance != null && instance != this)
        {
            instance.InitializeGameData(this);
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (mainCameraController == null && Camera.main != null)
        {
            mainCameraController = Camera.main.GetComponent<CameraController>();
        }

        if (OnTurnStart == null) OnTurnStart = new UnityEvent<int>();
        if (OnTurnEnd == null) OnTurnEnd = new UnityEvent<int>();
        if (OnGameStateChanged == null) OnGameStateChanged = new UnityEvent<GameState>();
        if (OnGameOver == null) OnGameOver = new UnityEvent();
    }

    void Start()
    {
        if (instance == this)
        {
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (nextPhaseButton != null) nextPhaseButton.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (rouletteResultText != null) rouletteResultText.gameObject.SetActive(false);

            StartGameLogic();
            UpdateScoreUI();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState != GameState.GameOver)
            {
                Pause();
            }
        }
    }

    // ★ 핵심 함수: 게임 재시작 시 데이터 리셋 및 참조 갱신
    public void InitializeGameData(GameManager newManager)
    {
        // 1. 참조 갱신
        this.players = newManager.players;
        this.players_movement = newManager.players_movement;

        this.announcerAudioSource = newManager.announcerAudioSource;
        this.BGMAudioSource = newManager.BGMAudioSource;

        this.mainCameraController = newManager.mainCameraController;
        this.MGCamera = newManager.MGCamera;
        this.projectileCam = newManager.projectileCam;

        this.turnDisplayText = newManager.turnDisplayText;
        this.rouletteResultText = newManager.rouletteResultText;
        this.pauseMenuUI = newManager.pauseMenuUI;
        this.darkBackground = newManager.darkBackground;

        this.scoreTextP1 = newManager.scoreTextP1;
        this.scoreTextP2 = newManager.scoreTextP2;

        this.nextPhaseButton = newManager.nextPhaseButton;
        this.gameOverPanel = newManager.gameOverPanel;
        this.winnerText = newManager.winnerText;

        // ★ [수정됨] 버튼 클릭 이벤트 자동 재연결
        // 이전 매니저는 파괴되므로, 버튼이 살아남은 매니저(this)의 함수를 호출하도록 다시 연결합니다.
        if (this.nextPhaseButton != null)
        {
            Button btn = this.nextPhaseButton.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextPhaseButtonClicked);
            }
        }

        // 2. 변수 리셋
        score_player1 = 0;
        score_player2 = 0;
        playerInCaptureZone = 0;
        currentPlayerIndex = 0;
        TurnFlag = false;
        dangtang = false;
        coment = false;
        isPaused = false;
        isPhaseReady = false;

        // 3. 시간 배율 초기화
        Time.timeScale = 1f;

        // 4. 정리 및 재시작
        StopAllCoroutines();

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (nextPhaseButton != null) nextPhaseButton.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (rouletteResultText != null) rouletteResultText.gameObject.SetActive(false);
        if (darkBackground != null) darkBackground.SetActive(false);

        UpdateScoreUI();
        StartGameLogic();
    }

    void StartGameLogic()
    {
        if (players == null || players.Count < minPlayersForGame)
        {
            Debug.LogError($"플레이어 수 부족.", this);
            return;
        }

        InitializeGame();
    }

    public void Pause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
            if (darkBackground != null) darkBackground.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (darkBackground != null) darkBackground.SetActive(false);
        }
    }

    void InitializeGame()
    {
        SetGameState(GameState.PreGame);

        if (BGMAudioSource != null && BGM1 != null)
        {
            BGMAudioSource.clip = BGM1;
            BGMAudioSource.loop = true;
            BGMAudioSource.Play();
        }

        foreach (var player in players)
        {
            if (player != null) player.EndTurn();
        }

        StartCoroutine(StartGameAfterDelay(1.0f));
    }

    IEnumerator PlayOpeningCommentarySequence()
    {
        if (announcerAudioSource != null && openingCommentary1 != null)
        {
            announcerAudioSource.PlayOneShot(openingCommentary1);
            yield return new WaitForSecondsRealtime(openingCommentary1.length);
        }

        if (announcerAudioSource != null && openingCommentary2 != null)
        {
            announcerAudioSource.PlayOneShot(openingCommentary2);
            yield return new WaitForSecondsRealtime(openingCommentary2.length);
        }
    }

    IEnumerator StartGameAfterDelay(float delay)
    {
        // ------------------------------------ 우당탕탕 (Make Ground) ------------------------------------ 
        dangtang = true;
        SetGameState(GameState.MakeGround);

        // ★ [수정됨] ChangeWind() 대신 ResetWind()를 호출하여 바람을 0으로 만듭니다.
        if (WindController.instance != null)
        {
            WindController.instance.ResetWind();
        }

        if (mainCameraController != null)
        {
            mainCameraController.SetCamera(MGCamera.transform);
        }
        StartCoroutine(PlayOpeningCommentarySequence());

        if (turnDisplayText != null)
        {
            turnDisplayText.text = "전투 준비!";
        }

        foreach (var player in players)
        {
            if (nextPhaseButton != null)
            {
                nextPhaseButton.SetActive(true);
                TextMeshProUGUI btnText = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"P{player.playerID + 1} 준비";

                // 버튼 누를 때까지 대기
                isPhaseReady = false;
                yield return new WaitUntil(() => isPhaseReady);
                nextPhaseButton.SetActive(false);
            }

            player.MakeGround();
            yield return new WaitForSeconds(player.MakingGroundTime);
            yield return new WaitForSeconds(delay / 2);
        }

        if (announcerAudioSource != null && closingCommentary != null)
        {
            announcerAudioSource.Stop();
            announcerAudioSource.PlayOneShot(closingCommentary);
        }

        // ------------------------------------ 룰렛 및 진영 결정 ------------------------------------ 

        yield return StartCoroutine(PositionSwapRoulette());

        // ------------------------------------ 게임 시작 ------------------------------------ 

        dangtang = false;
        SetGameState(GameState.TurnEnd);

        // ★ [확인] 실제 턴이 시작되기 직전, 첫 바람을 생성합니다.
        // 이때는 이미 dangtang = false이므로 ChangeWind()가 정상 작동합니다.
        if (WindController.instance != null)
        {
            WindController.instance.ChangeWind();
        }

        PlayerController firstPlayer = players[currentPlayerIndex];
        if (mainCameraController != null)
        {
            mainCameraController.SetTarget(firstPlayer.transform);
        }

        yield return new WaitForSeconds(delay);

        if (turnDisplayText != null)
        {
            turnDisplayText.text = $"P{firstPlayer.playerID + 1}";
        }

        firstPlayer.StartTurn();

        SetGameState(GameState.PlayerTurn);
        OnTurnStart.Invoke(firstPlayer.playerID);

        if (Random.value <= 0.2f)
        {
            if (announcerAudioSource != null && turnCommentary != null && coment == false)
            {
                announcerAudioSource.Stop();
                announcerAudioSource.PlayOneShot(turnCommentary);
            }
            else
            {
                coment = false;
            }
        }
    }

    IEnumerator PositionSwapRoulette()
    {
        bool doSwap = (players.Count >= 2 && Random.value <= 0.5f);

        float duration = 2.5f;
        float timer = 0f;
        float interval = 0.1f;
        bool toggle = false;

        if (rouletteResultText != null)
        {
            rouletteResultText.gameObject.SetActive(true);
        }

        while (timer < duration)
        {
            toggle = !toggle;

            if (rouletteResultText != null)
            {
                if (toggle)
                    rouletteResultText.text = "진영 유지\n<size=80%>Position KEEP</size>";
                else
                    rouletteResultText.text = "진영 교체\n<size=80%>Position SWAP</size>";
            }

            if (announcerAudioSource != null && rouletteSpinSFX != null)
            {
                announcerAudioSource.PlayOneShot(rouletteSpinSFX);
            }

            yield return new WaitForSeconds(interval);

            timer += interval;
            interval += 0.02f;
        }

        if (announcerAudioSource != null && rouletteDecideSFX != null)
        {
            announcerAudioSource.PlayOneShot(rouletteDecideSFX);
        }

        if (doSwap)
        {
            if (rouletteResultText != null)
                rouletteResultText.text = "<color=red>진영 교체\n<size=80%>Position SWAP</size></color>";
        }
        else
        {
            if (rouletteResultText != null)
                rouletteResultText.text = "<color=blue>진영 유지\n<size=80%>Position KEEP</size></color>";
        }

        yield return new WaitForSeconds(0.5f);

        if (doSwap)
        {
            CharacterController cc1 = players[0].GetComponent<CharacterController>();
            CharacterController cc2 = players[1].GetComponent<CharacterController>();

            if (cc1 != null) cc1.enabled = false;
            if (cc2 != null) cc2.enabled = false;

            Vector3 tempPos = players[0].transform.position;
            players[0].transform.position = players[1].transform.position;
            players[1].transform.position = tempPos;

            players[0].transform.Rotate(0, 180f, 0);
            players[1].transform.Rotate(0, 180f, 0);

            yield return null;

            if (cc1 != null) cc1.enabled = true;
            if (cc2 != null) cc2.enabled = true;

            if (players_movement.Count >= 2)
            {
                Transform tempRespawn = players_movement[0].respawnPoint;
                players_movement[0].respawnPoint = players_movement[1].respawnPoint;
                players_movement[1].respawnPoint = tempRespawn;
            }
        }

        yield return new WaitForSeconds(1.0f);

        if (rouletteResultText != null)
        {
            rouletteResultText.gameObject.SetActive(false);
        }

        // ★ [수정됨] 버튼이 있을 때만 대기
        if (nextPhaseButton != null)
        {
            nextPhaseButton.SetActive(true);
            TextMeshProUGUI btnText = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "Game Start!";

            isPhaseReady = false;
            yield return new WaitUntil(() => isPhaseReady);
            nextPhaseButton.SetActive(false);
        }
    }

    public void OnNextPhaseButtonClicked()
    {
        isPhaseReady = true;
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        OnGameStateChanged.Invoke(newState);
    }

    public void SwitchToNextTurn()
    {
        SetGameState(GameState.TurnEnd);
        if (WindController.instance != null)
        {
            WindController.instance.ChangeWind();
        }

        PlayerController previousPlayer = players[currentPlayerIndex];
        if (previousPlayer != null)
        {
            previousPlayer.EndTurn();
            OnTurnEnd.Invoke(previousPlayer.playerID);
        }

        if (TurnFlag)
        {
            if (players[0].isInCaptureZone && !players[1].isInCaptureZone)
            {
                score_player1 += 1;
                PlayPointCommentary();
            }
        }
        else
        {
            if (players[1].isInCaptureZone && !players[0].isInCaptureZone)
            {
                score_player2 += 1;
                PlayPointCommentary();
            }
        }

        UpdateScoreUI();
        HandleCaptureZoneCommentary();

        if ((score_player1 >= 2 || score_player2 >= 2) && BGMAudioSource.clip != BGM2)
        {
            if (BGMAudioSource != null && BGM2 != null)
            {
                BGMAudioSource.Stop();
                BGMAudioSource.clip = BGM2;
                BGMAudioSource.Play();
            }
        }

        if (score_player1 >= WinningScore || score_player2 >= WinningScore)
        {
            HandleGameOver();
            return;
        }

        Item_Reset();

        currentPlayerIndex++;
        if (currentPlayerIndex >= players.Count)
        {
            currentPlayerIndex = 0;
        }

        if (CheckGameOverCondition())
        {
            HandleGameOver();
        }
        else
        {
            StartCoroutine(StartNextTurnWithDelay(1.0f));
        }

        TurnFlag = !TurnFlag;
    }

    void PlayPointCommentary()
    {
        if (Random.value <= 1.0f)
        {
            coment = true;
            if (announcerAudioSource != null && pointCommentary != null)
            {
                announcerAudioSource.Stop();
                announcerAudioSource.PlayOneShot(pointCommentary);
            }
        }
    }

    void HandleCaptureZoneCommentary()
    {
        if (!players[1].isInCaptureZone && !players[0].isInCaptureZone)
        {
            if (Random.value <= 0.2f)
            {
                coment = true;
                if (announcerAudioSource != null && NpCommentary != null)
                {
                    announcerAudioSource.Stop();
                    announcerAudioSource.PlayOneShot(NpCommentary);
                }
            }
        }
        if (players[1].isInCaptureZone && players[0].isInCaptureZone)
        {
            if (Random.value <= 1.0f)
            {
                coment = true;
                if (announcerAudioSource != null && p2Commentary != null)
                {
                    announcerAudioSource.Stop();
                    announcerAudioSource.PlayOneShot(p2Commentary);
                }
            }
        }
    }

    IEnumerator StartNextTurnWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayerController nextPlayer = players[currentPlayerIndex];
        nextPlayer.StartTurn();

        if (mainCameraController != null)
        {
            mainCameraController.SetTarget(nextPlayer.transform);
        }

        if (turnDisplayText != null)
        {
            turnDisplayText.text = $"P{nextPlayer.playerID + 1}";
        }

        SetGameState(GameState.PlayerTurn);
        OnTurnStart.Invoke(nextPlayer.playerID);

        if (Random.value <= 0.2f)
        {
            if (announcerAudioSource != null && turnCommentary != null && coment == false)
            {
                announcerAudioSource.Stop();
                announcerAudioSource.PlayOneShot(turnCommentary);
            }
            else
            {
                coment = false;
            }
        }
    }

    private bool CheckGameOverCondition()
    {
        int activePlayers = 0;
        foreach (var player in players)
        {
            if (player != null && player.gameObject.activeInHierarchy)
            {
                activePlayers++;
            }
        }
        return activePlayers < minPlayersForGame;
    }

    void HandleGameOver()
    {
        SetGameState(GameState.GameOver);
        OnGameOver.Invoke();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (winnerText != null)
            {
                string winnerMsg = "무승부";
                if (score_player1 > score_player2) winnerMsg = "Player 1 승리!";
                else if (score_player2 > score_player1) winnerMsg = "Player 2 승리!";

                winnerText.text = winnerMsg;
            }
        }
    }

    public void OnRestartButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnProjectileFired(Transform projectileTransform)
    {
        SetGameState(GameState.ProjectileFlying);

        if (projectileCam != null)
        {
            projectileCam.SetTarget(projectileTransform);
        }
    }

    public void OnProjectileDestroyed()
    {
        if (currentState != GameState.ProjectileFlying) return;

        if (projectileCam != null)
        {
            projectileCam.StartDeactivationDelay(1.0f);
        }

        SwitchToNextTurn();
    }

    void Item_Reset()
    {
        players[currentPlayerIndex].trajectory.isPainted = true;
        players[currentPlayerIndex].ExplosionRange = players[currentPlayerIndex].BasicExplosionRange;
        players_movement[currentPlayerIndex].ResetSpeed();
    }

    public bool isMGTime()
    {
        if (currentState == GameState.MakeGround) return true;
        else return false;
    }

    void UpdateScoreUI()
    {
        if (scoreTextP1 != null) scoreTextP1.text = $"{score_player1}";
        if (scoreTextP2 != null) scoreTextP2.text = $"{score_player2}";
    }
}