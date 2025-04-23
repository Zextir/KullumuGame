using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync("Level1");
    }
    public void ExitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public GameObject allOptions; // assign 'All Options' in the Inspector for the Main Camera
    public GameObject credits;    // assign 'Credits' in the Inspector for the Main Camera

    public void ShowCreditsAndHideOptions()
    {
        if (allOptions != null)
            allOptions.SetActive(false);

        if (credits != null)
            credits.SetActive(true);
    }

    public void ShowOptionsAndHideCredits()
    {
        if (allOptions != null)
            allOptions.SetActive(true);

        if (credits != null)
            credits.SetActive(false);
    }

}


