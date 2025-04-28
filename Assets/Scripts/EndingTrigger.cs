using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class BloomTrigger : MonoBehaviour
{
    [Header("Bloom Settings")]
    public Volume volume;
    private Bloom bloom;

    [Header("Threshold Settings")]
    public float targetThreshold = 1f;
    public float thresholdTransitionDuration = 1f;

    [Header("Intensity Settings")]
    public float targetIntensity = 1f;
    public float intensityTransitionDuration = 1f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public bool fadeAudio = true;
    public float targetVolume = 1f;
    public float audioFadeDuration = 1f;

    [Header("Camera Settings")]
    public GameObject cameraController;
    public float targetFOV = 70f;
    public float fovTransitionDuration = 2f;

    [Header("Canvas Image Fade Settings")]
    public Image canvasImage;
    public float canvasFadeDelay = 2f;
    public float canvasFadeDuration = 2f;

    [Header("GameObject Activation Settings")]
    public GameObject objectToActivate;
    public float objectActivationDelay = 2f;

    private Camera cameraComponent;

    private bool isTriggered = false;
    private bool canvasFadeStarted = false;
    private bool canvasFadeCompleted = false;
    private bool objectActivated = false;

    private float initialThreshold;
    private float initialIntensity;
    private float initialAudioVolume;
    private float initialFOV;

    private float elapsedThresholdTime = 0f;
    private float elapsedIntensityTime = 0f;
    private float elapsedAudioTime = 0f;
    private float elapsedFOVTime = 0f;
    private float elapsedCanvasFadeDelay = 0f;
    private float elapsedCanvasFadeTime = 0f;
    private float elapsedObjectActivationTime = 0f;

    private void Start()
    {
        if (volume != null && volume.sharedProfile.TryGet(out bloom))
        {
            initialThreshold = bloom.threshold.value;
            initialIntensity = bloom.intensity.value;
        }
        else
        {
            Debug.LogError("Bloom not found in the assigned Volume Profile!");
        }

        if (audioSource != null)
        {
            initialAudioVolume = 0f;
            audioSource.volume = 0f;
        }

        if (cameraController != null)
        {
            cameraComponent = cameraController.GetComponentInChildren<Camera>();
            if (cameraComponent != null)
            {
                initialFOV = cameraComponent.fieldOfView;
            }
            else
            {
                Debug.LogError("Camera not found under the CameraController!");
            }
        }

        if (canvasImage != null)
        {
            Color color = canvasImage.color;
            color.a = 0f;
            canvasImage.color = color;
        }

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false);
        }
    }

    private void Update()
    {
        if (isTriggered && bloom != null)
        {
            elapsedThresholdTime += Time.deltaTime;
            elapsedIntensityTime += Time.deltaTime;
            elapsedAudioTime += Time.deltaTime;
            elapsedFOVTime += Time.deltaTime;

            float tThreshold = Mathf.Clamp01(elapsedThresholdTime / thresholdTransitionDuration);
            float tIntensity = Mathf.Clamp01(elapsedIntensityTime / intensityTransitionDuration);
            float tAudio = Mathf.Clamp01(elapsedAudioTime / audioFadeDuration);
            float tFOV = Mathf.Clamp01(elapsedFOVTime / fovTransitionDuration);


            // Smooth Interpolation
            bloom.threshold.value = Mathf.Lerp(initialThreshold, targetThreshold, tThreshold);
            bloom.intensity.value = Mathf.Lerp(initialIntensity, targetIntensity, tIntensity);


            if (audioSource != null && fadeAudio)
            {
                audioSource.volume = Mathf.Lerp(initialAudioVolume, targetVolume, tAudio);
            }

            if (cameraComponent != null)
            {
                cameraComponent.fieldOfView = Mathf.Lerp(initialFOV, targetFOV, tFOV);
            }

            // Start Canvas Fade after a delay
            if (canvasImage != null && !canvasFadeStarted)
            {
                elapsedCanvasFadeDelay += Time.deltaTime;
                if (elapsedCanvasFadeDelay >= canvasFadeDelay)
                {
                    canvasFadeStarted = true;
                    elapsedCanvasFadeTime = 0f;
                }
            }

            // Handle Canvas Image Fading
            if (canvasFadeStarted && !canvasFadeCompleted)
            {
                elapsedCanvasFadeTime += Time.deltaTime;
                float tCanvas = Mathf.Clamp01(elapsedCanvasFadeTime / canvasFadeDuration);
                Color color = canvasImage.color;
                color.a = tCanvas;
                canvasImage.color = color;

                if (tCanvas >= 1f)
                {
                    canvasFadeCompleted = true;
                    elapsedObjectActivationTime = 0f;
                }
            }

            // Handle GameObject Activation after canvas fully faded
            if (canvasFadeCompleted && objectToActivate != null && !objectActivated)
            {
                elapsedObjectActivationTime += Time.deltaTime;
                if (elapsedObjectActivationTime >= objectActivationDelay)
                {
                    objectToActivate.SetActive(true);
                    objectActivated = true;
                }
            }

            // Stop updating if all completed
            if (tThreshold >= 1f && tIntensity >= 1f && tFOV >= 1f && (!fadeAudio || elapsedAudioTime >= audioFadeDuration))
            {
                // Do nothing special here, since the canvas and object continue independently
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isTriggered && other.CompareTag("Player"))
        {
            isTriggered = true;
            elapsedThresholdTime = 0f;
            elapsedIntensityTime = 0f;
            elapsedAudioTime = 0f;
            elapsedFOVTime = 0f;
            elapsedCanvasFadeDelay = 0f;
            elapsedCanvasFadeTime = 0f;
            elapsedObjectActivationTime = 0f;

            canvasFadeStarted = false;
            canvasFadeCompleted = false;
            objectActivated = false;

            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}
