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

    [Header("특수탄 설정 (Chaser)")]
    [Tooltip("M키를 눌렀을 때 발사할 '추적탄 발사기' 프리팹")]
    public GameObject chaserDeployerPrefab;

    private PlayerController playerController;

    private Image powerImage;
    private TextMeshProUGUI powerText;

    [HideInInspector] public float currentLaunchPower;
    private bool isPowerIncreasing = true;

    private Collider playerCollider;

    void Awake()
    {
        playerCollider = GetComponent<Collider>();
        if (playerCollider == null)
        {
            Debug.LogWarning("PlayerShooting 스크립트가 있는 오브젝트에서 Collider를 찾지 못했습니다.", this);
        }

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerShooting이 PlayerController를 찾을 수 없습니다!", this);
        }
    }

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

        if (powerText != null)
        {
            powerText.text = $"Power: {currentLaunchPower:F0} / {maxLaunchPower:F0}";
        }
    }

    public void Fire()
    {
        bool isChaser = false;
        GameObject prefabToFire = currentProjectilePrefab; // 기본 포탄

        if (playerController != null && playerController.isNextShotChaser)
        {
            if (chaserDeployerPrefab != null)
            {
                prefabToFire = chaserDeployerPrefab; // "알" 포탄으로 교체
                isChaser = true;
            }
            else
            {
                Debug.LogError("추적자 모드이지만 chaserDeployerPrefab이 할당되지 않아 일반탄을 발사합니다.");
            }
            playerController.ResetChaserModeAfterFire();
        }

        if (prefabToFire == null || firePoint == null)
        {
            Debug.LogError("발사할 포탄이 선택되지 않았거나(null) 발사 지점이 할당되지 않았습니다.", this);
            if (GameManager.instance != null) GameManager.instance.SwitchToNextTurn();
            return;
        }

        Debug.LogFormat("포탄 발사! (프리팹: {0}, 파워: {1:F1})", prefabToFire.name, currentLaunchPower);

        GameObject projectileGO = Instantiate(prefabToFire, firePoint.position, firePoint.rotation);

        // ▼▼▼ [추가됨] "알" 포탄에 선택한 탄 타입 주입 ▼▼▼
        if (isChaser)
        {
            // 1. 방금 선택한 탄의 타입을 PlayerController로부터 가져옴
            ProjectileType selectedType = playerController.GetSelectedProjectileType();

            // 2. "알" 포탄의 스크립트를 가져옴
            ChaserDeployerProjectile deployer = projectileGO.GetComponent<ChaserDeployerProjectile>();
            if (deployer != null)
            {
                // 3. "알" 포탄에게 임무(탄 타입) 전달
                deployer.Initialize(selectedType);
            }
            else
            {
                Debug.LogError("chaserDeployerPrefab에 ChaserDeployerProjectile 스크립트가 없습니다!");
            }
        }
        // ▲▲▲ [여기까지 추가] ▲▲▲

        Collider projectileCollider = projectileGO.GetComponent<Collider>();
        if (playerCollider != null && projectileCollider != null)
        {
            Physics.IgnoreCollision(projectileCollider, playerCollider);
        }

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
            // (이전의 버그 수정 코드는 이미 여기에 반영되어 있습니다)
            GameManager.instance.OnProjectileFired(projectileGO.transform);
        }
    }
    // ▲▲▲ [여기까지 수정] ▲▲▲

    public float GetCurrentLaunchPower()
    {
        return currentLaunchPower;
    }

    public GameObject GetCurrentProjectilePrefab()
    {
        return currentProjectilePrefab;
    }
}