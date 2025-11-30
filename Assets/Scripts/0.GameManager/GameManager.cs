using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<PlayerController> players;
    public List<PlayerMovement> players_movement;
    public int currentPlayerIndex = 0;

    [Header("중계 오디오 설정")]
    public AudioSource announcerAudioSource; // 중계 멘트를 재생할 오디오 소스
    public AudioClip openingCommentary1;      // 첫 번째 멘트 파일
    public AudioClip openingCommentary2;      // 두 번째 멘트 파일
    public AudioClip closingCommentary;
    public AudioClip turnCommentary;
    public AudioClip pointCommentary;
    public AudioClip p2Commentary;
    public AudioClip NpCommentary;

    [Header("룰렛 연출 오디오 (추가됨)")]
    public AudioClip rouletteSpinSFX;   // 룰렛 돌아가는 소리 (틱, 틱, 틱)
    public AudioClip rouletteDecideSFX; // 룰렛 결정 소리 (탁!)

    [Header("배경음악 오디오 설정")]
    public AudioSource BGMAudioSource;
    public AudioClip BGM1;
    public AudioClip BGM2;

    [Header("메인카메라 지정")]
    public CameraController mainCameraController;
    public GameObject MGCamera;
    public ProjectileFollowCamera projectileCam;

    [Header("UI 연결")]
    public TextMeshProUGUI turnDisplayText; // 기존 턴 표시 텍스트
    public TextMeshProUGUI rouletteResultText; // 룰렛 전용 텍스트
    public GameObject pauseMenuUI;
    public GameObject darkBackground;

    [Header("점수 UI 연결")]
    public TextMeshProUGUI scoreTextP1;
    public TextMeshProUGUI scoreTextP2;

    [Header("진행 제어 UI")]
    public GameObject nextPhaseButton; // '다음' 또는 '게임 시작' 버튼
    public GameObject gameOverPanel;   // 게임 오버 패널
    public TextMeshProUGUI winnerText; // 승리자 텍스트

    [Header("플레이어 수,스코어")]
    public int minPlayersForGame = 2;
    public int score_player1 = 0, score_player2 = 0;
    public int WinningScore = 3;
    public int playerInCaptureZone = 0;

    private bool TurnFlag = false;
    public bool dangtang = false;
    public bool coment = false;

    // 버튼 입력을 기다리기 위한 플래그
    private bool isPhaseReady = false;

    bool isPaused = false;

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
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (mainCameraController == null)
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
        if (players == null || players.Count < minPlayersForGame)
        {
            Debug.LogError($"플레이어 수 부족.", this);
            return;
        }

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        if (nextPhaseButton != null) nextPhaseButton.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 룰렛 텍스트 초기화 (숨김)
        if (rouletteResultText != null) rouletteResultText.gameObject.SetActive(false);

        InitializeGame();
        UpdateScoreUI();
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

        // 각 플레이어 우당탕탕 진행
        foreach (var player in players)
        {
            if (nextPhaseButton != null)
            {
                nextPhaseButton.SetActive(true);
                TextMeshProUGUI btnText = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"P{player.playerID + 1} 준비";
            }

            isPhaseReady = false;
            yield return new WaitUntil(() => isPhaseReady);

            if (nextPhaseButton != null) nextPhaseButton.SetActive(false);

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

        // 룰렛 코루틴 호출
        yield return StartCoroutine(PositionSwapRoulette());


        // ------------------------------------ 게임 시작 ------------------------------------ 

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
        Debug.Log($"Player {firstPlayer.playerID}의 턴 시작!");
    }

    // 룰렛 연출 및 위치 스왑 로직
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

            Debug.Log("⚡ 룰렛 결과: 진영 교체!");
        }
        else
        {
            if (rouletteResultText != null)
                rouletteResultText.text = "<color=blue>진영 유지\n<size=80%>Position KEEP</size></color>";

            Debug.Log("⚡ 룰렛 결과: 위치 유지");
        }

        yield return new WaitForSeconds(0.5f);

        // 위치 변경 로직 (CharacterController 처리 포함)
        if (doSwap)
        {
            // 1. CharacterController 비활성화
            CharacterController cc1 = players[0].GetComponent<CharacterController>();
            CharacterController cc2 = players[1].GetComponent<CharacterController>();

            if (cc1 != null) cc1.enabled = false;
            if (cc2 != null) cc2.enabled = false;

            // 2. 위치 스왑
            Vector3 tempPos = players[0].transform.position;
            players[0].transform.position = players[1].transform.position;
            players[1].transform.position = tempPos;

            // 3. 180도 회전
            players[0].transform.Rotate(0, 180f, 0);
            players[1].transform.Rotate(0, 180f, 0);

            // 위치 변경 반영 대기
            yield return null;

            // 4. CharacterController 다시 활성화
            if (cc1 != null) cc1.enabled = true;
            if (cc2 != null) cc2.enabled = true;

            // 5. 리스폰 포인트 스왑
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

        if (nextPhaseButton != null)
        {
            nextPhaseButton.SetActive(true);
            TextMeshProUGUI btnText = nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "Game Start!";
        }

        isPhaseReady = false;
        yield return new WaitUntil(() => isPhaseReady);

        if (nextPhaseButton != null) nextPhaseButton.SetActive(false);
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
        }
        else
        {
            if (players[1].isInCaptureZone && !players[0].isInCaptureZone)
            {
                score_player2 += 1;
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
        }

        UpdateScoreUI();

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

    // ★ [수정됨] 이 함수에서 firstPlayer를 쓰면 안 됩니다! nextPlayer로 수정했습니다.
    IEnumerator StartNextTurnWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayerController nextPlayer = players[currentPlayerIndex]; // 여기서는 nextPlayer를 씁니다.
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
        Debug.Log($"Player {nextPlayer.playerID}의 턴 시작!"); // firstPlayer -> nextPlayer 수정됨
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
        Debug.Log("--- game over ---");

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