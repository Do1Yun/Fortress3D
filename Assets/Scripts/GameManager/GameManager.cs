using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // UnityEvent 사용을 위해 추가
using UnityEngine.SceneManagement; // 씬 전환을 위해 추가

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public List<PlayerController> players;
    private int currentPlayerIndex = 0;

    // TODO: 현재는 예시용으로 2명 이하가 되면 게임 오버. 실제로는 플레이어 사망 여부 등 더 복잡한 조건 추가
    [Header("게임 종료 조건")]
    public int minPlayersForGame = 2; 

    public enum GameState
    {
        PreGame,          // 게임 시작 전 대기 (로딩, 설정 등)
        PlayerTurn,       // 플레이어 턴 (이동, 조준, 발사 준비)
        ProjectileFlying, // 포탄이 발사되어 날아가는 중
        TurnEnd,          // 턴 종료 처리 (피해 계산, 다음 턴 준비)
        GameOver          // 게임 종료
    }
    public GameState currentState;

    // 턴 시작/종료 및 게임 상태 변경을 알리는 이벤트 정의
    // (다른 스크립트들이 이 이벤트를 구독하여 UI 업데이트, 카메라 전환 등을 처리할 수 있습니다.)
    public UnityEvent<int> OnTurnStart;         // 현재 플레이어 ID를 전달
    public UnityEvent<int> OnTurnEnd;           // 직전 플레이어 ID를 전달
    public UnityEvent<GameState> OnGameStateChanged; // 새로운 게임 상태를 전달
    public UnityEvent OnGameOver;               // 게임 오버 시 호출

    void Awake()
    {
        // 싱글턴 인스턴스 설정
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        // UnityEvent 초기화 (인스펙터에서 구독할 수 있도록)
        if (OnTurnStart == null) OnTurnStart = new UnityEvent<int>();
        if (OnTurnEnd == null) OnTurnEnd = new UnityEvent<int>();
        if (OnGameStateChanged == null) OnGameStateChanged = new UnityEvent<GameState>();
        if (OnGameOver == null) OnGameOver = new UnityEvent();
    }

    void Start()
    {
        // 플레이어 목록 유효성 검사
        if (players == null || players.Count < minPlayersForGame)
        {
            Debug.LogError($"GameManager에 등록된 플레이어가 {minPlayersForGame}명 미만입니다! 게임을 시작할 수 없습니다. 현재 플레이어 수: {players?.Count ?? 0}", this);
            // 에디터에서 플레이 중이라면 강제 종료하여 문제를 알림
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
            return;
        }

        InitializeGame();
    }

    // 게임 초기화 로직
    void InitializeGame()
    {
        SetGameState(GameState.PreGame); // 게임 시작 전 대기 상태
        Debug.Log("게임 초기화 중...");
        
        // TODO: 여기에서 게임 시작 전 필요한 로직 (예: 로딩 화면, 플레이어 초기 위치 설정 등)을 추가할 수 있습니다.

        // 짧은 지연 후 첫 턴 시작
        StartCoroutine(StartGameAfterDelay(1.5f)); 
    }

    // 게임 시작 전 지연 시간을 주기 위한 코루틴
    IEnumerator StartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetGameState(GameState.PlayerTurn); // 첫 플레이어 턴 시작
        players[currentPlayerIndex].StartTurn();
        OnTurnStart.Invoke(players[currentPlayerIndex].playerID);
        Debug.Log($"Player {players[currentPlayerIndex].playerID}의 턴 시작! [이동 모드]");
    }

    // 게임 상태를 변경하고 모든 구독자에게 알리는 함수
    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return; // 같은 상태로의 불필요한 변경 방지

        Debug.Log($"게임 상태 변경: {currentState} -> {newState}");
        currentState = newState;
        OnGameStateChanged.Invoke(newState); // 상태 변경 이벤트 발행
    }

    // 다음 턴으로 전환하는 함수 (Projectile 스크립트에서 호출될 예정)
    public void SwitchToNextTurn()
    {
        // 턴 종료 처리 (피해 계산, 점수 업데이트 등)
        SetGameState(GameState.TurnEnd); 
        
        // 현재 플레이어의 턴 종료 로직 호출 (PlayerController에서 직접 호출)
        players[currentPlayerIndex].EndTurn();
        OnTurnEnd.Invoke(players[currentPlayerIndex].playerID); // 턴 종료 이벤트 발행

        // 다음 플레이어 인덱스 계산
        currentPlayerIndex++;
        if (currentPlayerIndex >= players.Count)
        {
            currentPlayerIndex = 0; // 모든 플레이어가 턴을 마쳤으면 처음으로 돌아감
        }

        // 게임 종료 조건 확인
        if (CheckGameOverCondition())
        {
            HandleGameOver();
        }
        else
        {
            // 다음 턴 시작 (약간의 딜레이를 주면 게임 흐름이 자연스러워짐)
            StartCoroutine(StartNextTurnWithDelay(1.0f)); 
        }
    }

    // 다음 턴 시작 전 지연 시간을 주기 위한 코루틴
    IEnumerator StartNextTurnWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetGameState(GameState.PlayerTurn); // 다음 플레이어 턴 시작 상태
        players[currentPlayerIndex].StartTurn();
        OnTurnStart.Invoke(players[currentPlayerIndex].playerID);
        Debug.Log($"Player {players[currentPlayerIndex].playerID}의 턴 시작!");
    }

    // 게임 오버 조건 확인 (현재는 활성화된 플레이어 수로 판단)
    private bool CheckGameOverCondition()
    {
        int activePlayers = 0;
        foreach (var player in players)
        {
            // TODO: 실제로는 플레이어의 HP가 0이 되었는지, 또는 특정 게임 오버 조건을 여기서 확인
            if (player != null && player.gameObject.activeInHierarchy) 
            {
                activePlayers++;
            }
        }
        return activePlayers < minPlayersForGame; // 예를 들어, 2명 미만 남으면 게임 오버
    }

    // 게임 오버 처리 함수
    void HandleGameOver()
    {
        SetGameState(GameState.GameOver); // 게임 오버 상태로 변경
        OnGameOver.Invoke(); // 게임 오버 이벤트 발행
        Debug.Log("--- 게임 오버! ---");

        // TODO: 게임 오버 화면 표시, 결과 통계 등
        // 현재 씬 재시작 예시 (나중에는 메인 메뉴 등으로 이동)
        StartCoroutine(RestartGameAfterDelay(3.0f)); // 3초 후 게임 재시작
    }

    // 게임 재시작 전 지연 시간을 주기 위한 코루틴
    IEnumerator RestartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 현재 씬 다시 로드
    }

    // Projectile이 발사되었음을 GameManager에 알리는 함수 (PlayerController에서 호출)
    public void OnProjectileFired()
    {
        SetGameState(GameState.ProjectileFlying); // 포탄이 날아가는 중 상태
        Debug.Log("포탄 발사! 포탄의 움직임을 주시합니다.");
        // TODO: 이 상태에서 카메라가 포탄을 따라가도록 설정할 수 있습니다.
    }

    // Projectile이 충돌하거나 파괴되었음을 GameManager에 알리는 함수 (Projectile 스크립트에서 호출)
    public void OnProjectileDestroyed()
    {
        Debug.Log("포탄 파괴됨. 다음 턴으로 전환합니다.");
        SwitchToNextTurn(); // 다음 턴으로 전환
    }

    // (선택 사항) 플레이어 사망 시 호출될 함수 (PlayerController 등에서 호출)
    public void OnPlayerDied(PlayerController deadPlayer)
    {
        Debug.Log($"Player {deadPlayer.playerID} 사망!");
        // 플레이어 목록에서 사망한 플레이어 제거 또는 비활성화 처리
        if (players.Contains(deadPlayer))
        {
            players.Remove(deadPlayer);
            deadPlayer.gameObject.SetActive(false); // 플레이어 오브젝트 비활성화 (파괴는 아님)
        }

        // 즉시 게임 오버 조건 체크
        if (CheckGameOverCondition())
        {
            HandleGameOver();
        }
        // 만약 사망한 플레이어가 현재 턴 플레이어였다면 즉시 다음 턴으로 전환
        // 이 부분은 SwitchToNextTurn() 내부에서 이미 처리될 수 있으므로 중복 방지 필요
    }
}