using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;
using UnityEngine.Rendering;

public class GeyserShoot : MonoBehaviour
{
    // TODO: add stuff regarding the particleSystem so we don't have to change those in the PS itself
    [SerializeField] new ParticleSystem particleSystem;
    [SerializeField] BoxCollider boxCollider;
    ParticleSystem.MainModule psMain;

    [Header("Parameters")]
    [SerializeField] float interval = 10;
    [Tooltip("How much can the interval deviate from the main value?")]
    [SerializeField] float intervalRange = 3;
    [Tooltip("How much of the overall duration does the geyser stay active? (only change this value if you change the rate over time curve!)")]
    [SerializeField, Range(0,1)] float geyserActivityDurationPercentage = 0.35f;
    [Tooltip("The strength with which the geyser should shoot out objects.")]
    [SerializeField] float pushForce = 10f;
    
    Vector3 pushDirection = Vector3.up;
    
    float gravityModifier = 2.5f;

    float currentInterval;
    float burstCooldown;

    float maxGeyserDistance;

    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        boxCollider = GetComponent<BoxCollider>();
        psMain = particleSystem.main;
        currentInterval = interval;
        burstCooldown = interval;
    }

    private void Start()
    {
        OnChangeParameters();
        boxCollider.enabled = false;
        maxGeyserDistance = boxCollider.size.y * transform.localScale.x;
    }

    private void Update()
    {
        burstCooldown -= Time.deltaTime;
        if (burstCooldown < 0.05 * interval) Burst();
        if (burstCooldown < currentInterval - (geyserActivityDurationPercentage * currentInterval)) boxCollider.enabled = false;
            
    }

    void Burst()
    {
        particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        currentInterval = interval + ((2 * intervalRange * Random.value) - intervalRange);
        psMain.duration = currentInterval;
        boxCollider.enabled = true;
        burstCooldown = currentInterval;
        particleSystem.Play();
    }


    void OnChangeParameters()
    {
        if (particleSystem == null || boxCollider == null) return;
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        psMain = particleSystem.main;
        psMain.gravityModifier = gravityModifier * transform.lossyScale.x;
        psMain.duration = interval;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        float boundingSizeXZ = 2 * shape.angle;
        float boundingSizeY = psMain.startSpeed.constantMax;
        boxCollider.size = new Vector3(boundingSizeXZ, boundingSizeY, boundingSizeXZ);
        boxCollider.center = new Vector3(0, -boundingSizeY / 2, 0);

        pushDirection = -transform.up;
       
    }

    private void OnValidate()
    {
        OnChangeParameters();
        particleSystem.Play();
    }


    private void OnTriggerStay(Collider other)
    {
        UltimateCharacterLocomotion ucl = other.GetComponent<UltimateCharacterLocomotion>();
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        float distance = (transform.position - other.transform.position).magnitude;
        float forceMultiplier = 1 - Mathf.Clamp01(distance / maxGeyserDistance);
        Vector3 push = rb.mass * forceMultiplier * pushForce * pushDirection;

        if (ucl != null)
        {
           
            ucl.AddForce(push);
        }
        else
        {
            rb.AddForce(push);
        }

    }
}
