using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // --- 외부에서 연결할 변수들 (Inspector 창에서) ---
    [Header("플레이어 설정")]
    public int playerID;
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 10.0f;

    [Header("조준 관련")]
    //public Transform turretPivot; 
    //public Transform cannonBarrel;
    public Transform firePoint; // ▼▼▼ firePoint는 남겨둡니다 ▼▼▼
    //public float aimSensitivity = 2.0f;
    //public float maxAimAngle = 60.0f;
    //public float minAimAngle = -10.0f;

    [Header("발사 관련")]
    public GameObject projectilePrefab;
    public float launchPower = 50f;

    // --- 내부 상태 변수 ---
    private bool isMyTurn = false;
    private bool justSwitchedTurn = false;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private float gravityValue = -9.81f;
    //private float currentAimAngle = 0.0f; // 조준 기능을 사용하지 않으므로 주석 처리

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void StartTurn()
    {
        isMyTurn = true;
        justSwitchedTurn = true;
        Debug.Log("Player " + playerID + "의 턴 시작!");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void EndTurn()
    {
        isMyTurn = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!isMyTurn)
        {
            return;
        }

        if (justSwitchedTurn)
        {
            justSwitchedTurn = false;
            return;
        }

        HandleMovement();
        //HandleAiming(); // ▼▼▼ 조준 기능 호출을 주석 처리 ▼▼▼

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    void HandleMovement()
    {
        Transform camTransform = Camera.main.transform;

        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * verticalInput) + (camRight * horizontalInput);

        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (moveDirection != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    void HandleAiming()
    {
        // ▼▼▼ 조준 기능 비활성화를 위해 함수 내용을 모두 주석 처리 ▼▼▼
        /*
        float mouseX = Input.GetAxis("Mouse X") * aimSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * aimSensitivity;

        turretPivot.Rotate(Vector3.up, mouseX);

        currentAimAngle -= mouseY;
        currentAimAngle = Mathf.Clamp(currentAimAngle, minAimAngle, maxAimAngle);
        
        cannonBarrel.localEulerAngles = new Vector3(currentAimAngle, 0, 0);
        */
    }

    void Fire()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab이 설정되지 않았습니다!");
            return;
        }

        // ▼▼▼ firePoint는 여전히 사용되므로 이 부분은 그대로 둡니다 ▼▼▼
        if (firePoint == null)
        {
            Debug.LogError("Fire Point가 설정되지 않았습니다!");
            return;
        }

        Debug.Log("Player " + playerID + " 발사!");

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firePoint.forward * launchPower, ForceMode.Impulse);
        }
        else
        {
            Debug.LogError("포탄 프리팹에 Rigidbody 컴포넌트가 없습니다!");
        }

        GameManager.instance.SwitchToNextTurn();
    }
}