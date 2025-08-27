using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float maxStamina = 100f;
    public float staminaConsumptionRate = 20f;
    public float currentStamina { get; private set; }

    [Header("지면 체크")]
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private Image staminaImage;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentStamina = maxStamina;
    }

    public void SetUIReferences(Image stamina)
    {
        staminaImage = stamina;
    }

    public void HandleMovement()
    {
        if (rb.isKinematic)
        {
            rb.isKinematic = false;
        }

        if (currentStamina > 0)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

            if (movement.magnitude > 0)
            {
                rb.velocity = new Vector3(movement.x * moveSpeed, rb.velocity.y, movement.z * moveSpeed);
                currentStamina -= staminaConsumptionRate * Time.deltaTime;
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
        else
        {
            currentStamina = 0;
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }

        UpdateStaminaUI();
    }

    public void StopMovement()
    {
        if (rb == null) return;

        rb.velocity = Vector3.zero;

        // IsGrounded() 함수의 결과를 변수에 저장합니다.
        bool isPlayerGrounded = IsGrounded();

        // ★★★ 디버그 로그 1: StopMovement가 호출된 시점과 땅 감지 결과를 출력합니다.
        Debug.Log($"StopMovement Called! Is Player Grounded? -> {isPlayerGrounded}");

        if (isPlayerGrounded)
        {
            rb.isKinematic = true;
            // ★★★ 디버그 로그 2: Kinematic으로 전환되었음을 알립니다.
            Debug.Log("Result -> Rigidbody is now KINEMATIC (고정 상태)");
        }
        else
        {
            rb.isKinematic = false;
            // ★★★ 디버그 로그 3: Non-Kinematic 상태임을 알립니다.
            Debug.Log("Result -> Rigidbody is now NON-KINEMATIC (물리 적용 상태, 떨어져야 함)");
        }
    }

    public bool IsGrounded()
    {
        // 아래로 레이저(Raycast)를 쏴서 땅과 충돌하는지 확인합니다.
        bool didHit = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        // ★★★ 시각적 디버깅: 씬(Scene) 화면에 레이저를 직접 그립니다.
        // 땅을 감지했다면: 초록색 선
        // 땅을 감지하지 못했다면: 빨간색 선
        Color rayColor = didHit ? Color.green : Color.red;
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, rayColor, 2.0f);

        return didHit;
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    void UpdateStaminaUI()
    {
        if (staminaImage != null)
        {
            staminaImage.fillAmount = currentStamina / maxStamina;
        }
    }
}