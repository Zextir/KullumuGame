using UnityEngine;
using System.Collections;

public class WhaleExplosion : MonoBehaviour
{
    public GameObject impactObjectPrefab; // Assign a GameObject to appear on impact (e.g., crater, explosion)

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>(); // Ensure object has a Rigidbody
        }

        rb.angularVelocity = Random.insideUnitSphere * 5f; // Apply random spin
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("WhaleCollider")) // Ensure terrain has the "Terrain" tag
        {
            StartCoroutine(SpawnImpactObject());
        }
    }

    IEnumerator SpawnImpactObject()
    {
        if (impactObjectPrefab != null)
        {
            GameObject impactInstance = Instantiate(impactObjectPrefab, transform.position, Quaternion.identity);
            Destroy(impactInstance, 1.5f); // Destroy impact object after 3 seconds
        }

        gameObject.SetActive(false); // Hide the falling object immediately
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject); // Destroy after a short delay
    }
}
