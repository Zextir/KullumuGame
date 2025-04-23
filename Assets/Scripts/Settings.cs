using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [SerializeField] Slider brightnessSlider;
    [SerializeField] Slider gammaSlider;
    [SerializeField] Slider volumeSlider;

    static Dictionary<string, int> settings = new Dictionary<string, int>();

    public bool first = false;

    public static int GetSetting(string key)
    {
        return settings[key];
    }

    private void Start()
    {
        if (!settings.ContainsKey("brightness"))
            settings.Add("brightness", 5);
        if (!settings.ContainsKey("gamma"))
            settings.Add("gamma", 5);
        if (!settings.ContainsKey("volume"))
            settings.Add("volume", 5);
    }
    private void OnEnable()
    {
        //InitSettings();
        LoadSettings();
    }

    private void OnDisable()
    {
        if (first)
        {
            first = false;
            return;
        }
        SaveSettings();
    }

    void LoadSettings()
    {
        settings["brightness"] = PlayerPrefs.GetInt("brightness", settings["brightness"]);
        settings["gamma"] = PlayerPrefs.GetInt("gamma", settings["gamma"]);
        settings["volume"] = PlayerPrefs.GetInt("volume", settings["volume"]);
    }

    void SaveSettings()
    {
        settings["brightness"] = (int)brightnessSlider.value;
        settings["gamma"] = (int)gammaSlider.value;
        settings["volume"] = (int)volumeSlider.value;
        PlayerPrefs.SetInt("brightness", settings["brightness"]);
        PlayerPrefs.SetInt("gamma", settings["gamma"]);
        PlayerPrefs.SetInt("volume", settings["volume"]);
        PlayerPrefs.Save();

    }
}
