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
    public float verticalWindFactor = 0.2f; // <-- 이 변수가 추가되었습니다!

    public Vector3 CurrentWindDirection { get; private set; }
    public float CurrentWindStrength { get; private set; }

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
        ChangeWind();
    }

    public void ChangeWind()
    {
        // 1. 일단 완전 3D 랜덤 방향을 생성합니다.
        Vector3 randomDirection = Random.onUnitSphere;

        // 2. Y축(수직) 방향의 힘을 verticalWindFactor 값만큼 줄여줍니다.
        randomDirection.y *= verticalWindFactor;

        // 3. 방향 벡터의 전체 길이를 다시 1로 정규화하여 순수한 방향으로 만듭니다.
        CurrentWindDirection = randomDirection.normalized;

        CurrentWindStrength = Random.Range(minWindStrength, maxWindStrength);

        Debug.Log($" 턴 변경! 새로운 바람 발생! 방향: {CurrentWindDirection}, 세기: {CurrentWindStrength:F1}");
    }
}