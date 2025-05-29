using UnityEngine;

public class LerpMover : MonoBehaviour
{
    [Tooltip("Target Transform the object will move toward.")]
    public Transform target;

    [Tooltip("Movement speed. Higher values = faster movement.")]
    public float moveSpeed = 5f;

    private Vector3 initialPosition;

    void Start()
    {
        // Store the initial position of the object
        initialPosition = transform.position;
    }

    void Update()
    {
        if (target != null)
        {
            // Smoothly move toward the target position
            transform.position = Vector3.Lerp(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
    }

    // Call this method when collision with the player is detected
    public void ResetPosition()
    {
        transform.position = initialPosition;
    }
}
