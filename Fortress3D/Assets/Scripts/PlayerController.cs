using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // --- �ܺο��� ������ ������ (Inspector â����) ---
    [Header("�÷��̾� ����")]
    public int playerID;
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 10.0f;

    [Header("���� ����")]
    //public Transform turretPivot; 
    //public Transform cannonBarrel;
    public Transform firePoint; // ���� firePoint�� ���ܵӴϴ� ����
    //public float aimSensitivity = 2.0f;
    //public float maxAimAngle = 60.0f;
    //public float minAimAngle = -10.0f;

    [Header("�߻� ����")]
    public GameObject projectilePrefab;
    public float launchPower = 50f;

    // --- ���� ���� ���� ---
    private bool isMyTurn = false;
    private bool justSwitchedTurn = false;
    private CharacterController characterController;
    private Vector3 playerVelocity;
    private float gravityValue = -9.81f;
    //private float currentAimAngle = 0.0f; // ���� ����� ������� �����Ƿ� �ּ� ó��

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void StartTurn()
    {
        isMyTurn = true;
        justSwitchedTurn = true;
        Debug.Log("Player " + playerID + "�� �� ����!");

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
        //HandleAiming(); // ���� ���� ��� ȣ���� �ּ� ó�� ����

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
        // ���� ���� ��� ��Ȱ��ȭ�� ���� �Լ� ������ ��� �ּ� ó�� ����
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
            Debug.LogError("Projectile Prefab�� �������� �ʾҽ��ϴ�!");
            return;
        }

        // ���� firePoint�� ������ ���ǹǷ� �� �κ��� �״�� �Ӵϴ� ����
        if (firePoint == null)
        {
            Debug.LogError("Fire Point�� �������� �ʾҽ��ϴ�!");
            return;
        }

        Debug.Log("Player " + playerID + " �߻�!");

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firePoint.forward * launchPower, ForceMode.Impulse);
        }
        else
        {
            Debug.LogError("��ź �����տ� Rigidbody ������Ʈ�� �����ϴ�!");
        }

        GameManager.instance.SwitchToNextTurn();
    }
}