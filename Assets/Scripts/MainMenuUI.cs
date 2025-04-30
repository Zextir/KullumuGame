using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] GameObject loading;
    public void PlayGame()
    {
        StartCoroutine(LoadSceneWithMinimumTime());
    }

    IEnumerator LoadSceneWithMinimumTime()
    {
        float startTime = Time.time;
        float minimumLoadDelay = 4f;

        loading.SetActive(true);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainLevel");
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        float elapsed = Time.time - startTime;
        if (elapsed < minimumLoadDelay)
        {
            yield return new WaitForSeconds(minimumLoadDelay - elapsed);
        }

        asyncLoad.allowSceneActivation = true;
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}





