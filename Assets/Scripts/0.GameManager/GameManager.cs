using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<PlayerController> players;
    public List<PlayerMovement> players_movement;
    public int currentPlayerIndex = 0;

    [Header("중계 오디오 설정 (Voice)")]
    public AudioSource announcerAudioSource;
    public AudioClip openingCommentary1;
    public AudioClip openingCommentary2;
    public AudioClip closingCommentary;
    public AudioClip turnCommentary;
    public AudioClip pointCommentary;
    public AudioClip p2Commentary;
    public AudioClip NpCommentary;

    [Header("효과음 오디오 설정 (SFX)")] // ★ [추가] SFX 전용 오디오 소스
    public AudioSource sfxAudioSource;

    [Header("룰렛 연출 오디오")]
    public AudioClip rouletteTickSFX;
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

    public GameObject roulettePanel;
    public TextMeshProUGUI rouletteResultText;

    public GameObject announcementPanel;
    public TextMeshProUGUI announcementText;

    public TextMeshProUGUI NextPhaseText;
    public GameObject pauseMenuUI;
    public GameObject darkBackground;
    public TextMeshProUGUI scoreTextP1;
    public TextMeshProUGUI scoreTextP2;
    public GameObject nextPhaseButton;
    public GameObject gameOverPanel;
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

            if (roulettePanel != null) roulettePanel.SetActive(false);
            if (rouletteResultText != null) rouletteResultText.gameObject.SetActive(false);
            if (announcementPanel != null) announcementPanel.SetActive(false);

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

    public void InitializeGameData(GameManager newManager)
    {
        this.players = newManager.players;
        this.players_movement = newManager.players_movement;

        this.announcerAudioSource = newManager.announcerAudioSource;
        // ★ [추가] SFX 오디오 소스 연결
        this.sfxAudioSource = newManager.sfxAudioSource;
        this.BGMAudioSource = newManager.BGMAudioSource;

        this.mainCameraController = newManager.mainCameraController;
        this.MGCamera = newManager.MGCamera;
        this.projectileCam = newManager.projectileCam;

        this.turnDisplayText = newManager.turnDisplayText;

        this.roulettePanel = newManager.roulettePanel;
        this.rouletteResultText = newManager.rouletteResultText;
        this.announcementPanel = newManager.announcementPanel;
        this.announcementText = newManager.announcementText;

        this.pauseMenuUI = newManager.pauseMenuUI;
        this.darkBackground = newManager.darkBackground;

        this.scoreTextP1 = newManager.scoreTextP1;
        this.scoreTextP2 = newManager.scoreTextP2;

        this.nextPhaseButton = newManager.nextPhaseButton;
        this.gameOverPanel = newManager.gameOverPanel;
        this.winnerText = newManager.winnerText;

        if (this.nextPhaseButton != null)
        {
            Button btn = this.nextPhaseButton.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextPhaseButtonClicked);
            }
        }

        score_player1 = 0;
        score_player2 = 0;
        playerInCaptureZone = 0;
        currentPlayerIndex = 0;
        TurnFlag = false;
        dangtang = false;
        coment = false;
        isPaused = false;
        isPhaseReady = false;

        Time.timeScale = 1f;

        StopAllCoroutines();

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (nextPhaseButton != null) nextPhaseButton.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (darkBackground != null) darkBackground.SetActive(false);

        if (roulettePanel != null) roulettePanel.SetActive(false);
        if (rouletteResultText != null) rouletteResultText.gameObject.SetActive(false);
        if (announcementPanel != null) announcementPanel.SetActive(false);

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

    IEnumerator ShowAnnouncement(string message, float duration)
    {
        if (announcementPanel == null) yield break;

        if (announcementText != null)
            announcementText.text = message;

        announcementPanel.SetActive(true);

        yield return new WaitForSeconds(duration);

        announcementPanel.SetActive(false);
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
            string msg = $"Player{player.playerID + 1} 우당탕탕 시작하기!";
            if (announcementText != null) announcementText.text = msg;

            if (announcementPanel != null) announcementPanel.SetActive(true);

            if (nextPhaseButton != null)
            {
                nextPhaseButton.SetActive(true);
                TextMeshProUGUI btnText = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = "시작";

                isPhaseReady = false;
                yield return new WaitUntil(() => isPhaseReady);

                nextPhaseButton.SetActive(false);
            }

            if (announcementPanel != null) announcementPanel.SetActive(false);

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

        StartCoroutine(ShowAnnouncement("진영 결정 룰렛!", 1.5f));
        yield return new WaitForSeconds(1.5f);

        yield return StartCoroutine(PositionSwapRoulette());

        // ------------------------------------ 게임 시작 ------------------------------------ 

        dangtang = false;
        SetGameState(GameState.TurnEnd);

        if (announcementText != null) announcementText.text = "게임 시작!";
        if (announcementPanel != null) announcementPanel.SetActive(true);

        if (nextPhaseButton != null)
        {
            nextPhaseButton.SetActive(true);
            TextMeshProUGUI btnText = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "Game Start!";

            isPhaseReady = false;
            yield return new WaitUntil(() => isPhaseReady);
            nextPhaseButton.SetActive(false);
        }

        if (announcementPanel != null) announcementPanel.SetActive(false);

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

        if (roulettePanel != null) roulettePanel.SetActive(true);
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

            // ★ [수정] sfxAudioSource를 사용하여 틱 소리 재생
            if (sfxAudioSource != null && rouletteTickSFX != null)
            {
                sfxAudioSource.pitch = Random.Range(0.9f, 1.1f);
                sfxAudioSource.PlayOneShot(rouletteTickSFX);
            }

            yield return new WaitForSeconds(interval);

            timer += interval;
            interval += 0.02f;
        }

        // ★ [수정] sfxAudioSource의 Pitch 복구
        if (sfxAudioSource != null)
        {
            sfxAudioSource.pitch = 1.0f;
        }

        // ★ [수정] 결정음도 SFX로 재생
        if (sfxAudioSource != null && rouletteDecideSFX != null)
        {
            sfxAudioSource.PlayOneShot(rouletteDecideSFX);
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
        if (roulettePanel != null) roulettePanel.SetActive(false);
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