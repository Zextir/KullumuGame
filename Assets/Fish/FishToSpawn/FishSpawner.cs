using UnityEngine;
using System.Collections;

public class DebrisDrop : MonoBehaviour
{
    public GameObject[] objectPrefabs; // Array of prefabs to randomly choose from
    public Transform spawnArea;
    public float spawnRadius = 10f;
    public float spawnInterval = 1f;

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
        if (objectPrefabs.Length == 0) return;

        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnArea.position.x - spawnRadius, spawnArea.position.x + spawnRadius),
            spawnArea.position.y,
            Random.Range(spawnArea.position.z - spawnRadius, spawnArea.position.z + spawnRadius)
        );

        Quaternion randomRotation = Random.rotation;

        // Randomly pick a prefab from the array
        GameObject selectedPrefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
        GameObject spawnedObject = Instantiate(selectedPrefab, spawnPosition, randomRotation);

        spawnedObject.AddComponent<WhaleExplosion>(); // Attach the same physics/collision script
    }
}
