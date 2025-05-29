using UnityEngine;

public class ResetWater : MonoBehaviour
{
    [Tooltip("Reference to the LerpMover script on the water object.")]
    public LerpMover waterMover;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure your player GameObject is tagged as "Player"
        {
            waterMover.ResetPosition();
        }
    }
}
