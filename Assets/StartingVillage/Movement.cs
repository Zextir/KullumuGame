using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Camera Settings")]
    public Transform cameraTarget; // empty GameObject placed behind/above character
    public float mouseSensitivity = 3f;
    public float distanceFromPlayer = 5f;
    public float heightOffset = 2f;
    public float smoothTime = 0.1f;

    private CharacterController controller;
    private Vector3 velocity = Vector3.zero;
    private Vector3 currentVelocity;

    private float pitch = 0f;
    private float yaw = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleMovement();
        HandleCamera();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTarget.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentVelocity.y, 0.1f);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            Vector3 moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }
    }

    void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -30f, 60f);

        Vector3 targetPosition = transform.position - Quaternion.Euler(pitch, yaw, 0) * Vector3.forward * distanceFromPlayer;
        targetPosition.y += heightOffset;

        cameraTarget.position = Vector3.SmoothDamp(cameraTarget.position, targetPosition, ref velocity, smoothTime);
        cameraTarget.LookAt(transform.position + Vector3.up * 1.5f);
    }
}
