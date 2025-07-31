using UnityEngine;
using UnityEngine.UI; // Image, TextMeshProUGUI 사용을 위해 추가
using TMPro; // TextMeshPro 사용을 위해 추가

public class PlayerShooting : MonoBehaviour
{
    [Header("발사 지점 및 발사체")]
    public Transform firePoint;     // 포탄 발사 위치
    public GameObject projectilePrefab; // 발사할 포탄 프리팹

    [Header("발사 파워 설정")]
    public float minLaunchPower = 10f;
    public float maxLaunchPower = 50f;
    public float powerGaugeSpeed = 30f; // 파워 게이지 변화 속도

    // UI 참조 (PlayerController에서 SetUIReferences를 통해 전달받음)
    private Image powerImage;
    private TextMeshProUGUI powerText;

    [HideInInspector] public float currentLaunchPower; // 현재 발사 파워
    private bool isPowerIncreasing = true; // 파워 게이지가 증가 중인지 여부

    // PlayerController에서 UI 참조를 받아오기 위한 함수
    public void SetUIReferences(Image powerImg, TextMeshProUGUI powerTxt)
    {
        powerImage = powerImg;
        powerText = powerTxt;
    }

    // 파워 게이지 초기화 (PlayerController의 TransitionToNextStage에서 호출)
    public void ResetPowerGauge()
    {
        currentLaunchPower = minLaunchPower;
        isPowerIncreasing = true;
        UpdatePowerUI();
    }

    // 발사 파워 설정 로직
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

        UpdatePowerUI(); // 파워 UI 업데이트
    }

    // 파워 UI 업데이트 함수
    public void UpdatePowerUI()
    {
        if (powerImage != null) powerImage.fillAmount = (currentLaunchPower - minLaunchPower) / (maxLaunchPower - minLaunchPower);
        if (powerText != null) powerText.text = $"Power: {currentLaunchPower:F0}";
    }

    // 포탄 발사 로직
    public void Fire()
{
    if (projectilePrefab == null || firePoint == null)
    {
        Debug.LogError("발사체 프리팹 또는 발사 지점이 할당되지 않았습니다. 발사할 수 없습니다.", this);
        // 발사 실패 시 즉시 다음 턴으로 전환
        if (GameManager.instance != null) GameManager.instance.SwitchToNextTurn(); // <-- 이 부분 수정
        return;
    }

    Debug.LogFormat("포탄 발사! (파워: {0:F1})", currentLaunchPower);
    GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    
    Rigidbody rb = projectileGO.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.AddForce(firePoint.forward * currentLaunchPower, ForceMode.Impulse);
    }
    else
    {
        Debug.LogWarning("발사체 프리팹에 Rigidbody 컴포넌트가 없습니다. 물리 시뮬레이션이 작동하지 않습니다.", projectileGO);
        // Rigidbody가 없으면 포탄이 날아가지 않으므로 즉시 다음 턴으로 전환
        if (GameManager.instance != null) GameManager.instance.SwitchToNextTurn(); // <-- 이 부분 추가 (경고 후에도)
        Destroy(projectileGO); // 물리 효과가 없으니 바로 파괴
        return; // 추가 로직 실행 방지
    }

    // GameManager에게 포탄이 발사되었음을 알림 (발사 성공 시)
    if (GameManager.instance != null)
    {
        GameManager.instance.OnProjectileFired();
    }
}

    // Trajectory 스크립트에서 현재 발사 파워를 가져갈 수 있도록 하는 함수
    public float GetCurrentLaunchPower()
    {
        return currentLaunchPower;
    }
}