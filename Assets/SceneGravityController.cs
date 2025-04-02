using UnityEngine;
using Opsive.UltimateCharacterController.Character;

public class SceneGravityController : MonoBehaviour
{
    public Vector3 m_Gravity = new Vector3(0, -9.81f, 0); // Set custom gravity values
    private Vector3 originalGravity;
    private UltimateCharacterLocomotion characterLocomotion;

    void Awake()
    {
        // Store the original Unity gravity
        originalGravity = Physics.gravity;
        // Find the character locomotion component
        characterLocomotion = FindObjectOfType<UltimateCharacterLocomotion>();
        if (characterLocomotion != null)
        {
            // Set the new gravity direction for the character
            characterLocomotion.GravityDirection = m_Gravity.normalized * m_Gravity.magnitude;
        }
        else
        {
            // Apply global physics gravity if no UCC character is found
            Physics.gravity = m_Gravity;
        }
    }

    void OnDestroy()
    {
        if (characterLocomotion != null)
        {
            // Restore default gravity for the character
            characterLocomotion.GravityDirection = originalGravity.normalized * originalGravity.magnitude;
        }
        else
        {
            // Restore original Unity gravity
            Physics.gravity = originalGravity;
        }
    }
}