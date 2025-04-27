using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Opsive.UltimateCharacterController.Character;

public class PlayerSpawner : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null; // Wait one frame to ensure everything's initialized

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject spawn = GameObject.Find(SceneTransitionManager.SpawnPointName);

        if (player == null || spawn == null)
        {
            Debug.LogWarning("Missing player or spawn point.");
            yield break;
        }

        var locomotion = player.GetComponent<UltimateCharacterLocomotion>();
        if (locomotion != null)
        {
            // Move the player to the spawn point's position
            locomotion.SetPositionAndRotation(spawn.transform.position, player.transform.rotation);

            // Get the current scene name
            string currentScene = SceneManager.GetActiveScene().name;

            // Rotate player only if in LavaTunnel
            if (currentScene == "LavaTunnel")
            {
                // Rotate the player to give the tilted effect (rotate along the Z-axis)
                Vector3 tiltedRotation = new Vector3(0, spawn.transform.eulerAngles.y, 180f);
                player.transform.eulerAngles = tiltedRotation;

                Debug.Log("🔥 LavaTunnel: Tilted player rotation applied!");
            }
            else
            {
                // Normal player rotation for other scenes
                Vector3 normalRotation = new Vector3(0, spawn.transform.eulerAngles.y, 0);
                player.transform.eulerAngles = normalRotation;

                Debug.Log("✅ Player spawned normally at: " + spawn.name);
            }
        }
        else
        {
            Debug.LogError("🚫 UCC Locomotion component not found on player.");
        }
    }
}
