using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BigCreatureAI : MonoBehaviour
{
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    public float attackRange = 1.5f;
    public float biteCooldown = 3f;

    private float biteTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        int toxicArea = NavMesh.GetAreaFromName("Toxic");
        agent.areaMask = NavMesh.AllAreas & ~(1 << toxicArea);

        animator.SetBool("Spawned", true); // Will later be set from a trigger zone
    }

    void Update()
    {
        if (!agent.isOnNavMesh || player == null) return;

        agent.SetDestination(player.position);
        
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        biteTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, player.position);
        bool canReach = agent.pathStatus == NavMeshPathStatus.PathComplete;

        if (biteTimer <= 0f)
        {
            if (canReach && distance < attackRange)
            {
                animator.SetBool("CanReachPlayer", true);
                animator.SetTrigger("ShouldBite");
                biteTimer = biteCooldown;
            }
            else if (!canReach && distance < 100f)
            {
                animator.SetBool("CanReachPlayer", false);
                animator.SetTrigger("ShouldBite");
                biteTimer = biteCooldown;
            }
        }

    }

    public void Die()
    {
        animator.SetTrigger("Die");
        agent.isStopped = true;
        this.enabled = false;
    }
}