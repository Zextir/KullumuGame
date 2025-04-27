using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
//using static UnityEngine.Rendering.VolumeComponent;

public class PauseHandler : MonoBehaviour
{
    [SerializeField] GameObject overallPause;
    [SerializeField] GameObject pause;
    [SerializeField] GameObject settings;

    public static bool paused = false;

    //[Inject] PostProcessingHandler pp;

    private void OnEnable()
    {
        settings.GetComponent<Settings>().first = settings.activeInHierarchy;
        ResumeGame();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

    }

    public void ResumeGame()
    {
        overallPause.SetActive(false);
        settings.SetActive(false);
        pause.SetActive(true);
        paused = false;
        Time.timeScale = 1f;
    }

    private void PauseGame()
    {
        overallPause.SetActive(true);
        paused = true;
        Time.timeScale = 0f;
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
