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
        rb.velocity = Vector3.zero;

        // 1. 발밑에 땅이 있는지 확인한다.
        if (IsGrounded())
        {
            // 2. 땅 위에 있을 때만 '홀로그램(isKinematic=true)'으로 만들어 미끄러짐을 방지한다.
            rb.isKinematic = true;
        }
        else
        {
            // 3. 공중에 있다면, 계속 '탱크(isKinematic=false)' 상태를 유지하여 아래로 떨어진다.
            rb.isKinematic = false;
        }
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
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