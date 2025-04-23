using UnityEngine;

public class ExitGame : MonoBehaviour
{
    [SerializeField] GameObject originalExitButton;
    
    float timer = 0;
    private void OnEnable()
    {
        timer = 0;
    }

    private void OnDisable()
    {
        SwitchBack();
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer > 5)
        {
            SwitchBack();
        }
    }

    void SwitchBack()
    {
        gameObject.SetActive(false);
        originalExitButton.SetActive(true);
    }
}
