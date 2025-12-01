using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public List<GameObject> players;
    public List<Transform> position;
    public GameObject projectileprefab;
    public GameObject mainCamera;

    private Vector3 winPosition;
    private Vector3 losePosition;
    private int winPlayer_index;
    private Projectile projectile;

    [Header("카메라 이동 설정")]
    [Tooltip("카메라가 이동하는 데 걸리는 시간(초)")]
    public float cameraMoveDuration = 2.0f;

    [Tooltip("카메라가 이동할 거리 및 방향 (현재 위치 기준)")]
    public Vector3 cameraMoveOffset = new Vector3(5f, 0f, 5f);

    [Tooltip("이동 움직임 그래프 (예: Ease In Out을 추천합니다)")]
    // 기본값을 EaseInOut(부드러운 출발/정지)으로 설정
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake()
    {
        // GameManager가 없거나 플레이어 리스트가 비어있는 경우에 대한 안전장치 (선택사항)
        if (GameManager.instance != null)
        {
            winPlayer_index = GameManager.instance.currentPlayerIndex;
        }

        // 리스트 범위 체크 (안전장치)
        if (players.Count >= 2 && position.Count >= 2)
        {
            winPosition = position[0].position;
            losePosition = position[1].position;

            players[(winPlayer_index + 1) % 2].transform.position = winPosition;
            players[winPlayer_index].transform.position = losePosition;
        }

        if (projectileprefab != null)
        {
            projectile = projectileprefab.GetComponent<Projectile>();
            if (projectile != null) projectile.explosionRadius = 2.0f;
        }
    }

    void Start()
    {
        StartCoroutine(SpawnBullet());
    }

    IEnumerator SpawnBullet()
    {
        // 총알 발사 부분 (기존 코드 유지)
        if (projectileprefab != null)
        {
            Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));
            yield return new WaitForSeconds(0.3f);
            Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));
            yield return new WaitForSeconds(0.3f);
            Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));
            yield return new WaitForSeconds(0.3f);
            Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));
        }

        yield return new WaitForSeconds(0.6f);

        // 카메라 부드럽게 이동 시작
        if (mainCamera != null)
        {
            StartCoroutine(MoveCameraSmoothly(cameraMoveOffset, cameraMoveDuration));
        }
    }

    // AnimationCurve를 사용하여 카메라를 이동시키는 코루틴
    IEnumerator MoveCameraSmoothly(Vector3 offset, float duration)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = mainCamera.transform.position;
        Vector3 endPosition = startPosition + offset;

        while (elapsedTime < duration)
        {
            // 0 ~ 1 사이의 진행률(t) 계산
            float t = elapsedTime / duration;

            // ★ 핵심: 인스펙터에서 설정한 커브 그래프에 따라 진행률을 변환합니다.
            // 커브를 어떻게 그리느냐에 따라 천천히 출발하거나, 튕기거나 하는 효과가 적용됩니다.
            float curveValue = movementCurve.Evaluate(t);

            mainCamera.transform.position = Vector3.LerpUnclamped(
                startPosition,
                endPosition,
                curveValue
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 이동 완료 후 정확한 위치로 보정
        mainCamera.transform.position = endPosition;
    }
}