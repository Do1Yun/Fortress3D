using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PlayerController player1;
    public PlayerController player2;

    public enum GameState
    {
        GameStart,
        Player1Turn,
        Player2Turn,
        Firing,
        GameOver
    }

    public GameState currentState;

    void Awake()
    {
        Debug.Log("GameManager ON");
        instance = this;
    }

    void Start()
    {
        Debug.Log("P1 Start");
        // 게임 시작 시 Player 1의 턴부터 시작
        currentState = GameState.Player1Turn;
        player1.StartTurn();
        player2.EndTurn();
    }

    public void SwitchToNextTurn()
    {
        if (currentState == GameState.Player1Turn)
        {
            // 현재 턴이 Player 1이었다면, Player 2의 턴으로 변경
            currentState = GameState.Player2Turn;

            player1.EndTurn();   // Player 1의 턴 종료
            player2.StartTurn(); // Player 2의 턴 시작
        }
        else if (currentState == GameState.Player2Turn)
        {
            // 현재 턴이 Player 2이었다면, Player 1의 턴으로 변경
            currentState = GameState.Player1Turn;

            player2.EndTurn();   // Player 2의 턴 종료
            player1.StartTurn(); // Player 1의 턴 시작
        }
    }
}