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

    // ======== ▼▼▼ 1. 플레이어 자신의 콜라이더를 저장할 변수 추가 ▼▼▼ ========
    private Collider playerCollider;

    void Awake()
    {
        // 게임 시작 시 자신의 콜라이더를 찾아 변수에 저장해 둡니다.
        // GetComponent<Collider>()는 이 스크립트가 붙어있는 오브젝트의 콜라이더를 찾습니다.
        playerCollider = GetComponent<Collider>();
        if (playerCollider == null)
        {
            // 만약 콜라이더가 자식 오브젝트에 있다면 GetComponentInChildren<Collider>()를 사용해야 합니다.
            Debug.LogWarning("PlayerShooting 스크립트가 있는 오브젝트에서 Collider를 찾지 못했습니다.", this);
        }
    }
    // =================================================================

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

        // ======== ▼▼▼ 2. 충돌 무시 로직 추가 ▼▼▼ ========
        Collider projectileCollider = projectileGO.GetComponent<Collider>();

        // 플레이어 자신의 콜라이더와 방금 생성한 포탄의 콜라이더가 모두 존재한다면,
        if (playerCollider != null && projectileCollider != null)
        {
            // 물리 엔진에게 이 둘 사이의 충돌을 무시하라고 명령합니다.
            Physics.IgnoreCollision(projectileCollider, playerCollider);
        }
        // ==============================================

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