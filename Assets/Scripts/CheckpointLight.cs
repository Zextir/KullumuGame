using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [Header("Light Settings")]
    public Light[] lightsToAdjust;
    public float targetIntensity = 3f;
    public float targetRange = 15f;

    [Header("Emission Settings")]
    public Renderer emissionObjectRenderer;
    public Color emissionColor = Color.white;
    public float targetEmissionIntensity = 2f;

    [Header("Activation Settings")]
    public GameObject objectToActivate;

    [Header("Transition Settings")]
    public float transitionDuration = 1f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Fade lights
            foreach (Light light in lightsToAdjust)
            {
                StartCoroutine(FadeLight(light, light.intensity, targetIntensity, light.range, targetRange));
            }

            // Fade emission
            if (emissionObjectRenderer != null)
            {
                Material mat = emissionObjectRenderer.material;
                Color currentEmission = mat.GetColor("_EmissionColor");
                float currentIntensity = currentEmission.maxColorComponent;
                StartCoroutine(FadeEmission(mat, currentIntensity, targetEmissionIntensity));
            }

            // Activate another GameObject
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
            }
        }
    }

    private System.Collections.IEnumerator FadeLight(Light light, float startIntensity, float endIntensity, float startRange, float endRange)
    {
        float time = 0f;
        while (time < transitionDuration)
        {
            float t = time / transitionDuration;
            light.intensity = Mathf.Lerp(startIntensity, endIntensity, t);
            light.range = Mathf.Lerp(startRange, endRange, t);
            time += Time.deltaTime;
            yield return null;
        }
        light.intensity = endIntensity;
        light.range = endRange;
    }

    private System.Collections.IEnumerator FadeEmission(Material mat, float startIntensity, float endIntensity)
    {
        float time = 0f;
        while (time < transitionDuration)
        {
            float t = time / transitionDuration;
            float currentIntensity = Mathf.Lerp(startIntensity, endIntensity, t);
            Color emission = emissionColor * Mathf.LinearToGammaSpace(currentIntensity);
            mat.SetColor("_EmissionColor", emission);
            DynamicGI.SetEmissive(emissionObjectRenderer, emission);
            time += Time.deltaTime;
            yield return null;
        }

        Color finalEmission = emissionColor * Mathf.LinearToGammaSpace(endIntensity);
        mat.SetColor("_EmissionColor", finalEmission);
        DynamicGI.SetEmissive(emissionObjectRenderer, finalEmission);
    }
}
