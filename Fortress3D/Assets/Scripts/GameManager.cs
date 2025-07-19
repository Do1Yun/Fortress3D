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
        // ���� ���� �� Player 1�� �Ϻ��� ����
        currentState = GameState.Player1Turn;
        player1.StartTurn();
        player2.EndTurn();
    }

    public void SwitchToNextTurn()
    {
        if (currentState == GameState.Player1Turn)
        {
            // ���� ���� Player 1�̾��ٸ�, Player 2�� ������ ����
            currentState = GameState.Player2Turn;

            player1.EndTurn();   // Player 1�� �� ����
            player2.StartTurn(); // Player 2�� �� ����
        }
        else if (currentState == GameState.Player2Turn)
        {
            // ���� ���� Player 2�̾��ٸ�, Player 1�� ������ ����
            currentState = GameState.Player1Turn;

            player2.EndTurn();   // Player 2�� �� ����
            player1.StartTurn(); // Player 1�� �� ����
        }
    }
}