using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<PlayerController> players;
    private int currentPlayerIndex = 0;

    public enum GameState
    {
        Playing,
        GameOver
    }
    public GameState currentState;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (players.Count == 0)
        {
            Debug.LogError("GameManager에 등록된 플레이어가 없습니다!");
            return;
        }

        currentState = GameState.Playing;
        players[currentPlayerIndex].StartTurn();
    }

    public void SwitchToNextTurn()
    {
        players[currentPlayerIndex].EndTurn();
        currentPlayerIndex++;
        if (currentPlayerIndex >= players.Count)
        {
            currentPlayerIndex = 0;
        }
        players[currentPlayerIndex].StartTurn();
    }
}