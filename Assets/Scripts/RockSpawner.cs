using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject objectToSpawn; // Assign your prefab in the Inspector
    public float spawnInterval = 1f; // Time between spawns
    public Vector2 scaleRange = new Vector2(0.5f, 2f); // Random scale range

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnObject();
            timer = 0f;
        }
    }

    void SpawnObject()
    {
        if (objectToSpawn == null) return;

        // Spawn at this GameObject's position with a random rotation
        Quaternion randomRotation = Random.rotation;
        GameObject newObj = Instantiate(objectToSpawn, transform.position, randomRotation);

        // Apply a random uniform scale
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        newObj.transform.localScale = Vector3.one * randomScale;
    }
}
