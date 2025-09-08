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

    private List<PlayerController> players;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        spotLight = GetComponentInChildren<Light>();
        spotLight.color = Color.green;

        if (gameManager == null)
        {
            Debug.LogError("GameManager�� ã�� �� �����ϴ�!");
        }
        if (spotLight == null)
        {
            Debug.LogWarning("�ڽ� ������Ʈ���� Light ������Ʈ�� ã�� �� �����ϴ�.");
        }

        players = gameManager.players;
    }

    void Update()
    {
        switch (gameManager.playerInCaptureZone)
        {
            case 0:
                ChangeLightColor(Color.green);
                break;
            case 1:
                ChangeLightColor(Color.blue);
                break;
            case 2:
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
        //        Debug.Log($"���� ��... {currentCaptureProgress:F2}s / {captureTime}s");

        //        if (currentCaptureProgress >= captureTime)
        //        {
        //            isCaptured = true;
        //            OnCaptureComplete();
        //        }
        //    }
        //}
        //else
        //{
        //    // �ƹ��� ������ ���� ���� �ʱ�ȭ
        //    if (!isCaptured && currentCaptureProgress > 0f)
        //    {
        //        currentCaptureProgress = 0f;
        //        Debug.Log("���� �ߴ� (�÷��̾� ����)");
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
