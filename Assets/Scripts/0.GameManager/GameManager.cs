// Scripts.zip/GameManager/GameManager.cs

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

    [Header("메인카메라 지정")]
    public CameraController mainCameraController;
    public GameObject MGCamera;
    public ProjectileFollowCamera projectileCam;

    [Header("UI 연결")]
    public TextMeshProUGUI turnDisplayText;

    [Header("플레이어 수,스코어")]
    public int minPlayersForGame = 2;
    public int score_player1 = 0, score_player2 = 0;
    public int WinningScore = 3;
    public int playerInCaptureZone = 0;

    private bool TurnFlag = false;

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
        InitializeGame();
    }

    void InitializeGame()
    {
        SetGameState(GameState.PreGame);

        foreach (var player in players)
        {
            if (player != null) player.EndTurn();
        }

        StartCoroutine(StartGameAfterDelay(1.0f));
    }


    IEnumerator StartGameAfterDelay(float delay)
    {
        // ------------------------------------ 우당탕탕 스킵 ㅇㅇ------------------------------------ 
        //SetGameState(GameState.MakeGround);
        //if (mainCameraController != null)
        //{
        //    mainCameraController.SetCamera(MGCamera.transform);
        //}

        //// ▼▼▼ [추가됨] '우당탕탕' 중 턴 텍스트 변경 ▼▼▼
        //if (turnDisplayText != null)
        //{
        //    turnDisplayText.text = "전투 준비!";
        //}
        //// ▲▲▲ [여기까지 추가] ▲▲▲

        //// 모든 플레이어가 순서대로 '우당탕탕'을 진행합니다.
        //foreach (var player in players)
        //{
        //    player.MakeGround();
        //    // ★★★ [수정됨] 불안정한 WaitWhile 대신, 각 플레이어의 MakingGroundTime 만큼 명시적으로 기다립니다. ★★★
        //    yield return new WaitForSeconds(player.MakingGroundTime);
        //    yield return new WaitForSeconds(delay / 2); // 각 턴 사이에 짧은 딜레이
        //}
        // ------------------------------------ 우당탕탕 스킵 ㅇㅇ------------------------------------ 

        // '우당탕탕' 종료 후 첫 번째 플레이어의 턴을 시작합니다.
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

        // 점령시 점수 획득
        if (TurnFlag)
        {
            if (players[0].isInCaptureZone && !players[1].isInCaptureZone)
            {
                score_player1 += 1;
            }
        }
        else
        {
             if (players[1].isInCaptureZone && !players[0].isInCaptureZone)
            {
                score_player2 += 1;
            }
        }

        if ((score_player1 >= WinningScore) || (score_player2 >= WinningScore))
        {
            Debug.Log("Hello");
            SceneManager.LoadScene("GameoverScene");
        }

        Item_Reset(); // 아이템 영향 초기화

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
        Debug.Log($"Player {nextPlayer.playerID}의 턴 시작!");
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
        StartCoroutine(RestartGameAfterDelay(3.0f));
    }

    IEnumerator RestartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
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

    public void OnPlayerDied(PlayerController deadPlayer)
    {
        if (players.Contains(deadPlayer))
        {
            players.Remove(deadPlayer);
        }
    }

    void Item_Reset()
    {
        players[currentPlayerIndex].trajectory.isPainted = true;
        players[currentPlayerIndex].ExplosionRange = players[currentPlayerIndex].BasicExplosionRange;
    }

    public bool isMGTime()
    {
        if (currentState == GameState.MakeGround) return true;
        else return false;
    }
}

