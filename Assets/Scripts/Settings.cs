using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    //[SerializeField] Slider brightnessSlider;
    //[SerializeField] Slider gammaSlider;
    //[SerializeField] Slider volumeSlider;

    static Dictionary<string, int> defaultSettings = new Dictionary<string, int>()
    {
        { "brightness", 5 },
        { "gamma", 6 },
        { "volume", 5 }
    };

    public bool first = false;

    public static int GetSetting(string key)
    {
        return PlayerPrefs.GetInt(key, defaultSettings[key]);
    }

    public static void Set(string key, float value)
    {
        PlayerPrefs.SetInt(key, (int)value);
        PlayerPrefs.Save();
    }

    //private void OnEnable()
    //{
    //    if (!defaultSettings.ContainsKey("brightness"))
    //        defaultSettings.Add("brightness", 5);
    //    if (!defaultSettings.ContainsKey("gamma"))
    //        defaultSettings.Add("gamma", 5);
    //    if (!defaultSettings.ContainsKey("volume"))
    //        defaultSettings.Add("volume", 5);
    //}
    //private void OnEnable()
    //{
    //    //InitSettings();
    //    //LoadSettings();
    //}

    //private void OnDisable()
    //{
    //    if (first)
    //    {
    //        first = false;
    //        return;
    //    }
    //    SaveSettings();
    //}

    ////void LoadSettings()
    ////{
    ////    defaultSettings["brightness"] = 
    ////    defaultSettings["gamma"] = PlayerPrefs.GetInt("gamma", defaultSettings["gamma"]);
    ////    defaultSettings["volume"] = PlayerPrefs.GetInt("volume", defaultSettings["volume"]);
    ////}

    //void SaveSettings()
    //{
    //    PlayerPrefs.SetInt("brightness", (int)brightnessSlider.value);
    //    PlayerPrefs.SetInt("gamma", (int)gammaSlider.value);
    //    PlayerPrefs.SetInt("volume", (int)volumeSlider.value);
    //    PlayerPrefs.Save();

    //}
}
