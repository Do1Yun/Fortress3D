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

    [Header("ì¹´ë©”?¼ ì»¨íŠ¸ë¡¤ëŸ¬")]
    public CameraController mainCameraController; // [ì¶”ê??] ?¸?Š¤?™?„°?—?„œ ë©”ì¸ ì¹´ë©”?¼ë¥? ì§ì ‘ ?• ?‹¹?•´ ì£¼ì„¸?š”.

    [Header("ê²Œì„ ì¢…ë£Œ ì¡°ê±´")]
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

        // ?¸?Š¤?™?„°?—?„œ ?• ?‹¹?˜ì§? ?•Š?•˜?„ ê²½ìš°ë¥? ???ë¹„í•´ ?•œë²? ?” ì°¾ì•„ë´…ë‹ˆ?‹¤.
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
            Debug.LogError($"GameManager?— ?“±ë¡ëœ ?”Œ? ˆ?´?–´ê°? ë¶?ì¡±í•©?‹ˆ?‹¤! ê²Œì„?„ ?‹œ?‘?•  ?ˆ˜ ?—†?Šµ?‹ˆ?‹¤.", this);
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

        // ì²? ë²ˆì§¸ ?”Œ? ˆ?´?–´ë¡? ì¹´ë©”?¼ ???ê²? ?„¤? •
        if (mainCameraController != null)
        {
            mainCameraController.SetTarget(firstPlayer.transform);
        }

        SetGameState(GameState.PlayerTurn);
        OnTurnStart.Invoke(firstPlayer.playerID);
        Debug.Log($"Player {firstPlayer.playerID}?˜ ?„´ ?‹œ?‘!");
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

        Item_Reset(); // ¾ÆÀÌÅÛ ¿µÇâ ÃÊ±âÈ­

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

        // ?‹¤?Œ ?”Œ? ˆ?´?–´ë¡? ì¹´ë©”?¼ ???ê²? ?„¤? •
        if (mainCameraController != null)
        {
            mainCameraController.SetTarget(nextPlayer.transform);
        }

        SetGameState(GameState.PlayerTurn);
        OnTurnStart.Invoke(nextPlayer.playerID);
        Debug.Log($"Player {nextPlayer.playerID}?˜ ?„´ ?‹œ?‘!");
    }

    /* [ì¶”ê??] PlayerControllerê°? ì¹´ë©”?¼ ëª¨ë“œ ë³?ê²½ì„ ?š”ì²??•  ?ˆ˜ ?ˆ?Š” ?•¨?ˆ˜
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
        Debug.Log("--- ê²Œì„ ?˜¤ë²?! ---");
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

    void Item_Reset() // ÅÏÀÌ ³Ñ¾î°¡¸é ¾ÆÀÌÅÛ ¿µÇâ ÃÊ±âÈ­
    {
        players[currentPlayerIndex].trajectory.isPainted = true;
        players_movement[currentPlayerIndex].maxStamina = players_movement[currentPlayerIndex].basicStamina;
        players[currentPlayerIndex].ExplosionRange = players[currentPlayerIndex].BasicExplosionRange;
    }
}