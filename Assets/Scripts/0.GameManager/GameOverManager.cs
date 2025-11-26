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

    // 카메라 이동 설정 변수
    // 이 값을 더 작게 수정하면 카메라가 더 빠르게 이동합니다.
    public float cameraMoveDuration = 1.0f; // 2.0f에서 1.0f로 변경 (더 빠름)
    public Vector3 cameraMoveOffset = new Vector3(5f, 0f, 5f);

    void Awake()
    {
        winPlayer_index = GameManager.instance.currentPlayerIndex;

        winPosition = position[0].position;
        losePosition = position[1].position;

        players[winPlayer_index].transform.position = winPosition;
        players[(winPlayer_index + 1) % 2].transform.position = losePosition;
        projectile = projectileprefab.GetComponent<Projectile>();
        projectile.explosionRadius = 2.0f;
    }

    void Start()
    {
        StartCoroutine(SpawnBullet());
    }

    IEnumerator SpawnBullet()
    {
        // 총알 발사 부분 (기존 코드 유지)
        Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));
        yield return new WaitForSeconds(0.3f);
        Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));
        yield return new WaitForSeconds(0.3f);
        Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));
        yield return new WaitForSeconds(0.3f);
        Instantiate(projectileprefab, losePosition + new Vector3(0f, 10f, 0f), Quaternion.Euler(180f, 0f, 0f));

        yield return new WaitForSeconds(0.6f);

        // 카메라 부드럽게 이동 시작
        StartCoroutine(MoveCameraSmoothly(cameraMoveOffset, cameraMoveDuration));
    }

    // 선형 보간을 사용하여 카메라를 부드럽게 이동시키는 코루틴
    IEnumerator MoveCameraSmoothly(Vector3 offset, float duration)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = mainCamera.transform.position;
        Vector3 endPosition = startPosition + offset;

        while (elapsedTime < duration)
        {
            mainCamera.transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                elapsedTime / duration
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = endPosition;
    }
}