using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public List<GameObject> players;
    public List<Transform> position;
    public GameObject projectileprefab;

    private Vector3 winPosition;
    private Vector3 losePosition;
    private int winPlayer_index;
    private Projectile projectile;

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
        Instantiate(projectileprefab, losePosition + new Vector3(0, 5f, 0), Quaternion.Euler(180f, 0f, 0f));
        yield return new WaitForSeconds(0.5f);
        Instantiate(projectileprefab, losePosition + new Vector3(0, 5f, 0), Quaternion.Euler(180f, 0f, 0f));
    }
}
