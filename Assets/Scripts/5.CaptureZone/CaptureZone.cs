using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureZone : MonoBehaviour
{
    private GameManager gameManager;
    private Light spotLight;

    public float captureRadius = 5f;
    //public float captureTime = 5f;

    //private float currentCaptureProgress = 0f;
    //    private bool isCaptured = false;

    // 수정
    public GameObject neutralObject;
    public GameObject greenObject;
    public GameObject redObject;
    // 수정
    private List<PlayerController> players;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        spotLight = GetComponentInChildren<Light>();
        spotLight.color = Color.white;

        if (gameManager == null)
        {
            Debug.LogError("GameManager를 찾을 수 없습니다!");
        }
        if (spotLight == null)
        {
            Debug.LogWarning("자식 오브젝트에서 Light 컴포넌트를 찾을 수 없습니다.");
        }

        players = gameManager.players;
    }

    void Update()
    {
        switch (gameManager.playerInCaptureZone)
        {
            case 0:
                neutralObject.SetActive(true);
                greenObject.SetActive(false);
                redObject.SetActive(false);
                ChangeLightColor(Color.white);
                break;
            case 1:
                neutralObject.SetActive(false);
                greenObject.SetActive(true);
                redObject.SetActive(false);
                ChangeLightColor(Color.green);
                break;
            case 2:
                neutralObject.SetActive(false);
                greenObject.SetActive(false);
                redObject.SetActive(true);
                ChangeLightColor(Color.red);
                break;
            default:
                break;
        }

//        bool anyPlayerInRange = false;

        foreach (PlayerController player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= captureRadius && !player.isInCaptureZone)
            {
                player.isInCaptureZone = true;
                gameManager.playerInCaptureZone += 1;
            }
            if (distance > captureRadius && player.isInCaptureZone)
            {
                player.isInCaptureZone = false;
                gameManager.playerInCaptureZone -= 1;
            }
        }

        //if (anyPlayerInRange)
        //{
        //    if (!isCaptured)
        //    {
        //        currentCaptureProgress += Time.deltaTime;
        //        Debug.Log($"점령 중... {currentCaptureProgress:F2}s / {captureTime}s");

        //        if (currentCaptureProgress >= captureTime)
        //        {
        //            isCaptured = true;
        //            OnCaptureComplete();
        //        }
        //    }
        //}
        //else
        //{
        //    // 아무도 없으면 점령 진행 초기화
        //    if (!isCaptured && currentCaptureProgress > 0f)
        //    {
        //        currentCaptureProgress = 0f;
        //        Debug.Log("점령 중단 (플레이어 없음)");
        //    }
        //}
    }

    public void ChangeLightColor(Color newColor)
    {
        if (spotLight != null)
        {
            spotLight.color = newColor;
        }
    }
}
