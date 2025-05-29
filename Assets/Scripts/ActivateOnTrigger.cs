using UnityEngine;

public class ActivateOnTrigger : MonoBehaviour
{
    // Assign in Inspector
    public GameObject objectToActivate1;
    public GameObject objectToActivate2;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (objectToActivate1 != null)
                objectToActivate1.SetActive(true);
            else
                Debug.LogWarning("Object 1 is not assigned.");

            if (objectToActivate2 != null)
                objectToActivate2.SetActive(true);
            else
                Debug.LogWarning("Object 2 is not assigned.");
        }
    }
}
