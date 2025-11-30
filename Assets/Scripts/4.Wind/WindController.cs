using UnityEngine;

public class WindController : MonoBehaviour
{
    public static WindController instance;

    [Header("바람 세기 설정")]
    [Tooltip("바람의 최소 세기입니다.")]
    public float minWindStrength = 1f;
    [Tooltip("바람의 최대 세기입니다.")]
    public float maxWindStrength = 8f;

    [Header("바람 방향 설정")]
    [Tooltip("수직 방향 바람의 강도를 조절합니다. (0 = 수평 바람만, 1 = 완전 3D 랜덤)")]
    [Range(0f, 1f)]
    public float verticalWindFactor = 0.2f;

    public Vector3 CurrentWindDirection { get; private set; }
    public float CurrentWindStrength { get; private set; }

    public GameManager gameManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
            Debug.LogError("GameManager를 찾을 수 없습니다!");

        ChangeWind();
    }

    public void ChangeWind()
    {
        if (gameManager.isMGTime()) return;


        Vector2 horizontalDir = Random.insideUnitCircle.normalized;


        float randomY = Random.Range(-1f, 1f) * verticalWindFactor;


        CurrentWindDirection = new Vector3(horizontalDir.x, randomY, horizontalDir.y).normalized;

        CurrentWindStrength = Random.Range(minWindStrength, maxWindStrength);


        float gravityForce = 9.81f;
        float verticalForce = CurrentWindDirection.y * CurrentWindStrength;

        // 상승 풍력이 중력의 80%를 넘으면 바람 세기를 줄임 
        if (verticalForce > gravityForce * 0.8f)
        {
            float reductionRatio = (gravityForce * 0.8f) / verticalForce;
            CurrentWindStrength *= reductionRatio;
        }

        Debug.Log($"턴 변경! 바람: {CurrentWindDirection}, 세기: {CurrentWindStrength:F1}");
    }

    // ★ [추가됨] 바람을 강제로 0으로 초기화하는 함수
    public void ResetWind()
    {
        CurrentWindDirection = Vector3.zero;
        CurrentWindStrength = 0f;
        Debug.Log("바람 초기화 (0)");
    }
}