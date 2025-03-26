using UnityEngine;

public class AirDensityZone : MonoBehaviour
{
    [Tooltip("0 = No gravity, 100 = Extreme gravity")]
    [Range(0, 100)]
    public float airDensity = 0f;

    private void ApplyZoneEffects(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            PlayerMovement movement = other.GetComponent<PlayerMovement>();

            if (rb != null && movement != null)
            {
                // Exponential gravity scaling
                float gravityScale = -Mathf.Pow(airDensity / 10f, 2f);
                float dragValue = Mathf.Lerp(0f, 20f, airDensity / 100f);

                rb.linearDamping = dragValue;
                movement.gravityMultiplier = gravityScale;

                // Adjust jump force dynamically
                movement.jumpForce = Mathf.Lerp(20f, 1f, airDensity / 100f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyZoneEffects(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ApplyZoneEffects(other); // Continuously apply gravity and drag while inside the zone
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            PlayerMovement movement = other.GetComponent<PlayerMovement>();

            if (rb != null && movement != null)
            {
                rb.linearDamping = 1.0f;
                movement.gravityMultiplier = -9.81f; // Reset gravity
                movement.jumpForce = 7f; // Reset jump force
            }
        }
    }
}
