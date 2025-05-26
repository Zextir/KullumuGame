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

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.acceleration = 999f;     // Instant acceleration
        agent.angularSpeed = 999f;     // Instant turning
        agent.speed = normalSpeed;     // Ensure it's set at start
        anim = GetComponent<Animator>();

        // Avoid toxic when wandering
        safeAreaMask = ~(1 << NavMesh.GetAreaFromName("Toxic"));

        timer = decisionInterval;
    }

    void Update()
    {
        if (isDead) return;

        anim.SetFloat("Speed", agent.velocity.magnitude);
        timer -= Time.deltaTime;

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
            agent.acceleration = 50f;   // Smooth, calm wandering
            agent.angularSpeed = 120f;  // More natural turning
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.GetComponent<DeadlyToTouch>() != null)
        {
            Debug.Log("Entered deadly zone — dying.");
            Die();
        }
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
        //Debug.Log("Fleeing from threat!");

        Vector3 target = transform.position + dir * Random.Range(15f, 25f);

        // First, try full NavMesh
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // fallback: flee backward a bit
            Vector3 fallback = transform.position - dir * 5f;
            if (NavMesh.SamplePosition(fallback, out NavMeshHit backupHit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(backupHit.position);
                //Debug.Log("Used fallback flee target.");
            }
            else
            {
                //Debug.LogWarning("Critter couldn't find flee destination.");
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
        agent.isStopped = true;
        anim.SetTrigger("Die");

        // Optional: disable further AI movement
        this.enabled = false;
    }
}