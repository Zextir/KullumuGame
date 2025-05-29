using System.Collections;
using System.Collections.Generic;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;
using UnityEngine.AI;

public class BigCreatureAI : MonoBehaviour
{
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    public Animator playerAnimator;

    public float attackRange = 1.5f;
    public float biteCooldown = 3f;
    public float detectionRange = 1000f;
    public bool isCharacterCrouching = false;

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

        float distance = Vector3.Distance(transform.position, player.position);
        bool canReach = agent.pathStatus == NavMeshPathStatus.PathComplete;

        isCharacterCrouching = playerAnimator.GetInteger("AbilityIndex") == 3;

        if (isCharacterCrouching)
        {
            detectionRange = 500f; // Reduce detection range when player is crouching
        }
        else
        {
            detectionRange = 1000f; // Reset detection range when player is not crouching
        }

        if (distance <= detectionRange)
        {
            agent.SetDestination(player.position);
            animator.SetBool("CanDetectPlayer", true);
        }
        else
        {
            agent.ResetPath();
            animator.SetBool("CanDetectPlayer", false);
        }

        biteTimer -= Time.deltaTime;

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