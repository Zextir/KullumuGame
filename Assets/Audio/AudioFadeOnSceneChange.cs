using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioFadeOnSceneChange : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference musicEvent;

    [Header("Fade Settings")]
    [Tooltip("Time in seconds to fade out after scene change.")]
    public float fadeDuration = 2f;

    private EventInstance musicInstance;
    private bool fadeStarted = false;

    void Start()
    {
        if (musicEvent.IsNull)
        {
            Debug.LogWarning("FMODSceneMusicController: Music EventReference is null.");
            return;
        }

        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();
        musicInstance.release();

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!fadeStarted)
        {
            fadeStarted = true;
            StartCoroutine(FadeOutAndStop());
        }
    }

    private IEnumerator FadeOutAndStop()
    {
        float elapsedTime = 0f;
        float startVolume = 1f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentVolume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            musicInstance.setVolume(currentVolume);
            yield return null;
        }

        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Destroy(gameObject);
    }
}