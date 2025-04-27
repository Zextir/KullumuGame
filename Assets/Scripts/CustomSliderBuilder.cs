using UnityEngine;
using UnityEngine.UI;

public class CustomSliderBuilder : MonoBehaviour
{

    [SerializeField] CustomSliderFill filledLayout;
    [SerializeField] CustomSliderFill unfilledLayout;
    [SerializeField] RectMask2D fillMask;
    [SerializeField] RectTransform handleArea;

    [SerializeField] RectTransform customSlider;

    [SerializeField] string settingName;
    Slider slider;

    int segments;
    int height = 50;

    private void Start()
    {
        slider = GetComponent<Slider>();
        Build();
        slider.value = Settings.GetSetting(settingName);
    }

    void Build()
    {
        segments = (int)(slider.maxValue - slider.minValue) + 1;
        int width = (int)(height * 1.5 * segments - height / 2);



        customSlider.sizeDelta = new Vector2(width, height);
        handleArea.sizeDelta = new Vector2(width - height, height);
        handleArea.anchoredPosition = new Vector2(height / 2, 0);

        filledLayout.CreateSlider(segments);
        unfilledLayout.CreateSlider(segments);
        UpdateFill();
    }

    void UpdateFill()
    {
        int padding = (int)(1.5 * height * (segments - (int)slider.value) - height / 2);
        fillMask.padding = new Vector4(0, 0, padding, 0);
    }

    public void OnChange()
    {
        Settings.Set(settingName, slider.value);
        UpdateFill();
    }

}
