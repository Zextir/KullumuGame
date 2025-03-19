using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using System.Collections;

public class LaunchPlayer : MonoBehaviour
{
    [Tooltip("Direction to launch the player (normalized).")]
    public Vector3 launchDirection = new Vector3(1, 1, 0); // Default diagonal launch

    [Tooltip("Force applied to the player on launch.")]
    public float launchForce = 10f;

    [Tooltip("Delay before launching the player.")]
    public float launchDelay = 1f;

    private UltimateCharacterLocomotion characterLocomotion;
    private Rigidbody characterRigidbody;

    void Start()
    {
        characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
        characterRigidbody = GetComponent<Rigidbody>();

        if (characterLocomotion == null || characterRigidbody == null)
        {
            Debug.LogError("LaunchPlayer: Missing UltimateCharacterLocomotion or Rigidbody component!");
            return;
        }

        // Ensure Rigidbody is not kinematic (UCC sometimes sets it to kinematic)
        characterRigidbody.isKinematic = false;

        StartCoroutine(DelayedLaunch());
    }

    private IEnumerator DelayedLaunch()
    {
        yield return new WaitForSeconds(launchDelay);
        Launch();
    }

    private void Launch()
    {
        Vector3 force = launchDirection.normalized * launchForce;

        // Reset current velocity before launching
        characterRigidbody.velocity = Vector3.zero;

        // Apply force
        characterRigidbody.AddForce(force, ForceMode.Impulse);

        Debug.Log("Player Launched with force: " + force);
    }
}
