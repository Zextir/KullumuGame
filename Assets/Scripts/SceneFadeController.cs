using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeController : MonoBehaviour
{
    public static SceneFadeController Instance;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeToScene(string sceneName, string spawnPoint)
    {
        SceneTransitionManager.SpawnPointName = spawnPoint;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        yield return StartCoroutine(Fade(1f)); // fade to black
        SceneManager.LoadScene(sceneName);
        // fade back out after scene is loaded (OnSceneLoaded)
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeImage == null)
        {
            fadeImage = GameObject.FindWithTag("FadeImage")?.GetComponent<Image>();
        }

        StartCoroutine(Fade(0f)); // fade out
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, targetAlpha);
        if (targetAlpha == 0f)
            fadeImage.gameObject.SetActive(false);
    }
}
