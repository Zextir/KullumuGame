using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenTrigger : MonoBehaviour
{
    public string sceneToLoad; // Set this in the Inspector
    public string loadingSceneName = "LoadingScene"; // Name of the loading screen scene
    public Image fadeImage; // Assign a UI Image that covers the screen (black with alpha 0 initially)
    public float fadeDuration = 1f;

    private static LoadingScreenTrigger instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Check if fadeImage is assigned before using it
        if (fadeImage == null)
        {
            Debug.LogError("Fade Image is not assigned in the Inspector!");
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure the player has the "Player" tag
        {
            StartCoroutine(LoadWithLoadingScreen());
        }
    }

    IEnumerator LoadWithLoadingScreen()
    {
        // Fade to black before loading
        yield return StartCoroutine(FadeToBlack());

        // Load the loading screen scene asynchronously
        AsyncOperation loadingSceneOp = SceneManager.LoadSceneAsync(loadingSceneName);
        while (!loadingSceneOp.isDone)
        {
            yield return null; // Wait until loading completes
        }

        // Ensure the fade-out is complete
        yield return StartCoroutine(FadeFromBlack());

        // Simulate loading time (Replace this with actual loading logic if needed)
        yield return new WaitForSeconds(3f);

        // Fade to black again before switching to the target scene
        yield return StartCoroutine(FadeToBlack());

        // Load the target scene asynchronously
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(sceneToLoad);
        while (!sceneLoadOp.isDone)
        {
            yield return null; // Wait until loading completes
        }

        // Fade out once the scene is loaded
        yield return StartCoroutine(FadeFromBlack());
    }

    IEnumerator FadeToBlack()
    {
        float elapsedTime = 0;
        Color color = fadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
    }

    IEnumerator FadeFromBlack()
    {
        float elapsedTime = 0;
        Color color = fadeImage.color;
        color.a = 1; // Ensure it's fully black before fading in
        fadeImage.color = color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = 1 - Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
    }
}
