using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BigCreatureReset : MonoBehaviour
{
    public GameObject player;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private NavMeshAgent agent;
    private bool isRespawning = false;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRespawning) return;

        if (other.gameObject == player)
        {
            StartCoroutine(RespawnEnemy(3f));
        }
    }

    private IEnumerator RespawnEnemy(float delay)
    {
        isRespawning = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        yield return new WaitForSeconds(delay);

        if (agent.enabled)
            agent.enabled = false;

        transform.position = startPosition;
        transform.rotation = startRotation;

        agent.enabled = true;
        isRespawning = false;
    }

}
