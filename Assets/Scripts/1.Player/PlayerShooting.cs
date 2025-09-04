using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerShooting : MonoBehaviour
{
    [Header("발사 지점")]
    public Transform firePoint;
    private GameObject currentProjectilePrefab;

    [Header("발사 파워 설정")]
    public float minLaunchPower = 10f;
    public float maxLaunchPower = 50f;
    public float powerGaugeSpeed = 30f;

    private Image powerImage;
    private TextMeshProUGUI powerText;

    [HideInInspector] public float currentLaunchPower;
    private bool isPowerIncreasing = true;

    public void SetProjectile(GameObject prefab)
    {
        currentProjectilePrefab = prefab;
    }

    public void SetUIReferences(Image powerImg, TextMeshProUGUI powerTxt)
    {
        powerImage = powerImg;
        powerText = powerTxt;
    }

    public void ResetPowerGauge()
    {
        currentLaunchPower = minLaunchPower;
        isPowerIncreasing = true;
        UpdatePowerUI();
    }

    public void HandlePowerSetting()
    {
        if (isPowerIncreasing)
        {
            currentLaunchPower += powerGaugeSpeed * Time.deltaTime;
            if (currentLaunchPower >= maxLaunchPower) isPowerIncreasing = false;
        }
        else
        {
            currentLaunchPower -= powerGaugeSpeed * Time.deltaTime;
            if (currentLaunchPower <= minLaunchPower) isPowerIncreasing = true;
        }
        UpdatePowerUI();
    }

    public void UpdatePowerUI()
    {
        if (powerImage != null) powerImage.fillAmount = (currentLaunchPower - minLaunchPower) / (maxLaunchPower - minLaunchPower);
        if (powerText != null) powerText.text = $"Power: {currentLaunchPower:F0}";
    }

    public void Fire()
    {
        if (currentProjectilePrefab == null || firePoint == null)
        {
            Debug.LogError("발사할 포탄이 선택되지 않았거나 발사 지점이 할당되지 않았습니다.", this);
            if (GameManager.instance != null) GameManager.instance.SwitchToNextTurn();
            return;
        }

        Debug.LogFormat("포탄 발사! (파워: {0:F1})", currentLaunchPower);
        GameObject projectileGO = Instantiate(currentProjectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firePoint.forward * currentLaunchPower, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("발사체 프리팹에 Rigidbody 컴포넌트가 없습니다.", projectileGO);
            Destroy(projectileGO);
            if (GameManager.instance != null) GameManager.instance.SwitchToNextTurn();
            return;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.OnProjectileFired();
        }
    }

    public float GetCurrentLaunchPower()
    {
        return currentLaunchPower;
    }

    public GameObject GetCurrentProjectilePrefab()
    {
        return currentProjectilePrefab;
    }
}