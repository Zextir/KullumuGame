using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string targetScene; // Target scene to load
    public string spawnPointInTargetScene; // Spawn point in the target scene

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Entering scene transition. Target scene: " + targetScene + " Spawn point: " + spawnPointInTargetScene);

            // Set the spawn point name to pass to the new scene
            SceneTransitionManager.SpawnPointName = spawnPointInTargetScene;

            // Load the new scene
            SceneManager.LoadScene(targetScene);
        }
    }
}
