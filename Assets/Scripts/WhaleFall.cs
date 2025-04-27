using UnityEngine;
using System.Collections;

public class WhaleFall : MonoBehaviour
{
    public GameObject objectPrefab; // Prefab to spawn
    public Transform spawnArea; // Define an area to spawn objects
    public float spawnRadius = 10f; // Spawn range
    public float spawnInterval = 1f; // Time between spawns

    private void Start()
    {
        StartCoroutine(SpawnObjects());
    }

    IEnumerator SpawnObjects()
    {
        while (true)
        {
            SpawnObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnObject()
    {
        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnArea.position.x - spawnRadius, spawnArea.position.x + spawnRadius),
            spawnArea.position.y,
            Random.Range(spawnArea.position.z - spawnRadius, spawnArea.position.z + spawnRadius)
        );

        Quaternion randomRotation = Random.rotation; // Randomize rotation

        GameObject spawnedObject = Instantiate(objectPrefab, spawnPosition, randomRotation);
        spawnedObject.AddComponent<WhaleExplosion>(); // Attach script to handle physics and collision
    }
}
