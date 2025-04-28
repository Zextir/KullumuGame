using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessAdjustment : MonoBehaviour
{
    [SerializeField] Vector2 brightnessLimits = new Vector2(-2, 5);
    [SerializeField] Vector2 gammaLimits = new Vector2(0.3f, 1.6f);

    VolumeProfile volumeProfile;

    float brightness;
    float gamma;

    private void OnEnable()
    {
        volumeProfile = GetComponent<Volume>().sharedProfile;
    }


    void Update()
    {
        float currentBrightness = Settings.GetSetting("brightness");
        float currentGamma = Settings.GetSetting("gamma");

        if (currentBrightness != brightness)
        {
            brightness = currentBrightness;
            currentBrightness = RemapBrightness(currentBrightness);
            Debug.Log("updating brightness!");
            if (volumeProfile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                colorAdjustments.postExposure.value = currentBrightness;
                Debug.Log(volumeProfile.name);
            }
           
        }
        if (currentGamma != gamma)
        {
            gamma = currentGamma;
            currentGamma = RemapGamma(currentGamma);
            Debug.Log("updating gamma!");
            if (volumeProfile.TryGet<LiftGammaGain>(out var liftGammaGain))
            {
                Vector4 gammaSettings = liftGammaGain.gamma.value;
                gammaSettings.w = currentGamma;
                liftGammaGain.gamma.value = gammaSettings;
                Debug.Log(volumeProfile.name);
            }
        }

    }

    // TODO (nice-to-have): get the actual slider range (10, now) from the sliders, in case of change.
    float RemapBrightness(float originalBrightness)
    {
        float range = brightnessLimits.y - brightnessLimits.x;
        float unit = range / 10;
        return brightnessLimits.x + unit * originalBrightness;
    }

    float RemapGamma(float originalGamma)
    {
        float range = gammaLimits.y - gammaLimits.x;
        float unit = range / 10;
        return gammaLimits.x + unit * originalGamma - 1;
    }
}
