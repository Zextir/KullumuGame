using UnityEngine;

public class FloatingRock : MonoBehaviour
{
    public float floatHeight = 0.5f; // Distance the rock moves up and down
    public float floatSpeed = 1.0f; // Speed of the floating motion
    public float tiltAngle = 5f; // Maximum tilt angle
    public float tiltSpeed = 0.5f; // Speed of the tilting motion

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float randomOffset;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        randomOffset = Random.Range(0f, Mathf.PI * 2); // Randomize phase
    }

    void Update()
    {
        // Floating movement
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Tilting movement
        float tiltX = Mathf.Sin(Time.time * tiltSpeed + randomOffset) * tiltAngle;
        float tiltZ = Mathf.Cos(Time.time * tiltSpeed + randomOffset) * tiltAngle;
        Quaternion tiltRotation = Quaternion.Euler(tiltX, 0, tiltZ);
        transform.rotation = startRotation * tiltRotation;
    }
}