using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<PlayerController> players;
    private int currentPlayerIndex = 0;

    [Header("게임 종료 조건")]
    public int minPlayersForGame = 2;

    public enum GameState
    {
        PreGame,
        PlayerTurn,
        ProjectileFlying,
        TurnEnd,
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

        if (OnTurnStart == null) OnTurnStart = new UnityEvent<int>();
        if (OnTurnEnd == null) OnTurnEnd = new UnityEvent<int>();
        if (OnGameStateChanged == null) OnGameStateChanged = new UnityEvent<GameState>();
        if (OnGameOver == null) OnGameOver = new UnityEvent();
    }

    void Start()
    {
        if (players == null || players.Count < minPlayersForGame)
        {
            Debug.LogError($"GameManager에 등록된 플레이어가 부족합니다! 게임을 시작할 수 없습니다.", this);
            return;
        }
        InitializeGame();
    }

    void InitializeGame()
    {
        SetGameState(GameState.PreGame);

        foreach (var player in players)
        {
            if (player != null) player.enabled = false;
        }

        StartCoroutine(StartGameAfterDelay(1.5f));
    }

    IEnumerator StartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayerController firstPlayer = players[currentPlayerIndex];
        firstPlayer.enabled = true;
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

        PlayerController previousPlayer = players[currentPlayerIndex];
        if (previousPlayer != null)
        {
            previousPlayer.EndTurn();
            previousPlayer.enabled = false;
            OnTurnEnd.Invoke(previousPlayer.playerID);
        }

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
    }

    IEnumerator StartNextTurnWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayerController nextPlayer = players[currentPlayerIndex];
        nextPlayer.enabled = true;
        nextPlayer.StartTurn();

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
        Debug.Log("--- 게임 오버! ---");
        StartCoroutine(RestartGameAfterDelay(3.0f));
    }

    IEnumerator RestartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnProjectileFired()
    {
        SetGameState(GameState.ProjectileFlying);
    }

    public void OnProjectileDestroyed()
    {
        // 포탄이 날아가는 중이 아닐 때 호출되면(예: 턴 종료 처리 중 다른 포탄이 터짐) 턴이 중복으로 넘어가는 것을 방지
        if (currentState != GameState.ProjectileFlying) return;

        SwitchToNextTurn();
    }

    public void OnPlayerDied(PlayerController deadPlayer)
    {
        if (players.Contains(deadPlayer))
        {
            players.Remove(deadPlayer);
        }
    }
}