using UnityEngine;

public class AlienPlantBehavior : MonoBehaviour
{
    [Header("References")]
    public Transform plantVisual;            // The visible part of the plant
    public GameObject alertEffect;           // Effect to activate after vibration

    [Header("Vibration Settings")]
    public float vibrationDuration = 1f;
    public float vibrationAmount = 0.02f;
    public float vibrationSpeed = 40f;

    [Header("Movement Settings")]
    public float sinkDepth = 1f;
    public float sinkSpeed = 8f;
    public float riseSpeed = 2f;

    [Header("Effect Settings")]
    public float alertDuration = 2f;

    private Vector3 initialPosition;
    private Vector3 hiddenPosition;
    private bool playerInRange = false;
    private bool isVibrating = false;
    private float vibrationTimer = 0f;

    private float alertTimer = 0f;

    void Start()
    {
        if (plantVisual == null)
            plantVisual = transform;

        initialPosition = plantVisual.localPosition;
        hiddenPosition = initialPosition - new Vector3(0, sinkDepth, 0);

        if (alertEffect != null)
            alertEffect.SetActive(false);
    }

    void Update()
    {
        if (isVibrating)
        {
            vibrationTimer -= Time.deltaTime;
            float vibration = Mathf.Sin(Time.time * vibrationSpeed) * vibrationAmount;
            plantVisual.localPosition = initialPosition + new Vector3(0, vibration, 0);

            if (vibrationTimer <= 0f)
            {
                isVibrating = false;

                // Activate effect
                if (alertEffect != null)
                {
                    alertEffect.SetActive(true);
                    alertTimer = alertDuration;
                }
            }
        }
        else
        {
            if (playerInRange)
            {
                // Sink quickly
                plantVisual.localPosition = Vector3.MoveTowards(plantVisual.localPosition, hiddenPosition, Time.deltaTime * sinkSpeed);
            }
            else
            {
                // Rise slowly
                plantVisual.localPosition = Vector3.MoveTowards(plantVisual.localPosition, initialPosition, Time.deltaTime * riseSpeed);
            }
        }

        // Timer to deactivate alert effect
        if (alertTimer > 0f)
        {
            alertTimer -= Time.deltaTime;
            if (alertTimer <= 0f && alertEffect != null)
            {
                alertEffect.SetActive(false);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerInRange)
        {
            playerInRange = true;
            isVibrating = true;
            vibrationTimer = vibrationDuration;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
