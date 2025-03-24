using UnityEngine;
using UnityEngine.UI;

public class RotateAndFadeImage : MonoBehaviour
{
    public float rotationSpeed = 100f; // Rotation speed in degrees per second
    public float fadeSpeed = 1f;       // Speed at which the opacity fades (0 to 1)
    private Image imageComponent;
    private float targetAlpha = 1f;    // Target opacity (1 = fully visible, 0 = fully transparent)
    private float fadeTimer = 0f;      // Timer to control the fade

    void Start()
    {
        // Get the Image component
        imageComponent = GetComponent<Image>();
    }

    void Update()
    {
        // Rotate the UI element
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

        // Handle fading
        fadeTimer += Time.deltaTime * fadeSpeed;
        if (fadeTimer >= 1f)
        {
            fadeTimer = 0f; // Reset timer after one fade cycle
            targetAlpha = targetAlpha == 1f ? 0f : 1f; // Toggle between 0 and 1
        }

        // Smoothly change the alpha value
        float alpha = Mathf.Lerp(imageComponent.color.a, targetAlpha, fadeTimer);

        // Update the Image component's color with the new alpha value
        Color newColor = imageComponent.color;
        newColor.a = alpha;
        imageComponent.color = newColor;
    }
}
