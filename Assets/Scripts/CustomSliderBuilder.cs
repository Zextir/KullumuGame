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

    private void Start()
    {
        slider = GetComponent<Slider>();
        Build();
        slider.value = Settings.GetSetting(settingName);
    }

    void Build()
    {
        segments = (int)(slider.maxValue - slider.minValue) + 1;
        int height = 20;
        int width = 30 * segments - 10;

        customSlider.sizeDelta = new Vector2(width, height);
        handleArea.sizeDelta = new Vector2(width - 20, height);

        filledLayout.CreateSlider(segments);
        unfilledLayout.CreateSlider(segments);
        UpdateFill();
    }

    public void UpdateFill()
    {
        int padding = 30 * (segments - (int)slider.value) - 5;
        fillMask.padding = new Vector4(0, 0, padding, 0);
    }

}
