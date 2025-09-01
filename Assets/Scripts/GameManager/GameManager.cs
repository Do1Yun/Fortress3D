// Scripts.zip/GameManager/GameManager.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<PlayerController> players;
    public List<PlayerMovement> players_movement;
    public int currentPlayerIndex = 0;

    [Header("카메?�� 컨트롤러")]
    public CameraController mainCameraController; // [추�??] ?��?��?��?��?��?�� 메인 카메?���? 직접 ?��?��?�� 주세?��.

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

        // ?��?��?��?��?��?�� ?��?��?���? ?��?��?�� 경우�? ???비해 ?���? ?�� 찾아봅니?��.
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
            Debug.LogError($"GameManager?�� ?��록된 ?��?��?��?���? �?족합?��?��! 게임?�� ?��?��?�� ?�� ?��?��?��?��.", this);
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

        StartCoroutine(StartGameAfterDelay(1.5f));
    }

    IEnumerator StartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        PlayerController firstPlayer = players[currentPlayerIndex];
        firstPlayer.StartTurn();

        // �? 번째 ?��?��?��?���? 카메?�� ???�? ?��?��
        if (mainCameraController != null)
        {
            mainCameraController.SetTarget(firstPlayer.transform);
        }

        SetGameState(GameState.PlayerTurn);
        OnTurnStart.Invoke(firstPlayer.playerID);
        Debug.Log($"Player {firstPlayer.playerID}?�� ?�� ?��?��!");
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

        Item_Reset(); // ������ ���� �ʱ�ȭ

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
        nextPlayer.StartTurn();

        // ?��?�� ?��?��?��?���? 카메?�� ???�? ?��?��
        if (mainCameraController != null)
        {
            mainCameraController.SetTarget(nextPlayer.transform);
        }

        SetGameState(GameState.PlayerTurn);
        OnTurnStart.Invoke(nextPlayer.playerID);
        Debug.Log($"Player {nextPlayer.playerID}?�� ?�� ?��?��!");
    }

    /* [추�??] PlayerController�? 카메?�� 모드 �?경을 ?���??�� ?�� ?��?�� ?��?��
    public void RequestCameraModeChange(CameraController.CameraMode newMode)
    {
        if (mainCameraController != null)
        {
            mainCameraController.SwitchMode(newMode);
        }
    }
    */

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
        Debug.Log("--- 게임 ?���?! ---");
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

    void Item_Reset() // ���� �Ѿ�� ������ ���� �ʱ�ȭ
    {
        players[currentPlayerIndex].trajectory.isPainted = true;
        players_movement[currentPlayerIndex].maxStamina = players_movement[currentPlayerIndex].basicStamina;
        players[currentPlayerIndex].ExplosionRange = players[currentPlayerIndex].BasicExplosionRange;
    }
}