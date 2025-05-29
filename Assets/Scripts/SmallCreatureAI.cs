using UnityEngine;
using UnityEngine.AI;

public class SmallCreatureAI : MonoBehaviour
{
    public Transform[] threats; // Player + enemy
    public float fleeDistance = 50f;
    public float wanderRadius = 20f;
    public float decisionInterval = 0.5f;
    public float normalSpeed = 10f;
    public float fleeSpeed = 100f;

    private NavMeshAgent agent;
    private Animator anim;
    private float timer;
    private int safeAreaMask;
    private bool isDead = false;
    private bool isActive = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        agent.enabled = false;
        anim.enabled = false;

        // Avoid toxic when wandering
        safeAreaMask = ~(1 << NavMesh.GetAreaFromName("Toxic"));
        timer = decisionInterval;
    }

    void Update()
    {
        if (!isActive || isDead) return;

        anim.SetFloat("Speed", agent.velocity.magnitude);

        if (IsThreatNearby(out Vector3 fleeDir))
        {
            anim.SetBool("IsFleeing", true);
            agent.speed = fleeSpeed;
            Flee(fleeDir);
        }
        else
        {
            anim.SetBool("IsFleeing", false);
            agent.speed = normalSpeed;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = decisionInterval;
                Wander();
            }
        }

        if (anim.GetBool("IsFleeing"))
        {
            agent.acceleration = 999f;
            agent.angularSpeed = 999f;
        }
        else
        {
            agent.acceleration = 50f;
            agent.angularSpeed = 120f;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.GetComponent<DeadlyToTouch>() != null)
        {
            Die();
        }
    }

    public void ActivateAI()
    {
        isActive = true;
        agent.enabled = true;
        anim.enabled = true;
    }

    bool IsThreatNearby(out Vector3 fleeDir)
    {
        fleeDir = Vector3.zero;
        int threatCount = 0;

        foreach (var t in threats)
        {
            if (t == null) continue;

            float dist = Vector3.Distance(transform.position, t.position);
            if (dist < fleeDistance)
            {
                fleeDir += (transform.position - t.position).normalized;
                threatCount++;
            }
        }

        if (threatCount > 0)
        {
            fleeDir.Normalize();
            return true;
        }

        return false;
    }

    void Flee(Vector3 dir)
    {

        Vector3 target = transform.position + dir * Random.Range(15f, 25f);

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            Vector3 fallback = transform.position - dir * 5f;
            if (NavMesh.SamplePosition(fallback, out NavMeshHit backupHit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(backupHit.position);
            }
        }
    }


    void Wander()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 point = transform.position + new Vector3(offset.x, 0, offset.y);

        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, safeAreaMask))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    
        anim.SetTrigger("Die");
    }
}