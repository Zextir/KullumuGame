using UnityEngine;

public class LerpMover : MonoBehaviour
{
    [Tooltip("Target Transform the object will move toward.")]
    public Transform target;

    [Tooltip("Movement speed. Higher values = faster movement.")]
    public float moveSpeed = 5f;

    void Update()
    {
        if (target != null)
        {
            // Smoothly move toward the target position
            transform.position = Vector3.Lerp(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
    }
}
