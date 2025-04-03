using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;

public class ButtonHoverBehavior : MonoBehaviour
{
    [SerializeField] Image highlightImage;
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] Color normalTextColor;
    [SerializeField] Color highlightTextColor;



    public void ShowHighlightImage()
    {
        // Debug.Log("Pointer entered");
        highlightImage.enabled = true; // Show the highlight image

        if (buttonText != null)
        {
            buttonText.color = highlightTextColor;
            buttonText.fontStyle = FontStyles.Bold;
        }

    }

    public void HideHighlightImage()
    {
        // Debug.Log("Pointer exited");
        highlightImage.enabled = false;

        if (buttonText != null)
        {
            buttonText.color = normalTextColor;
            buttonText.fontStyle = FontStyles.Normal;
        }
    }

    void OnDisable()
    {
        HideHighlightImage();
    }
}